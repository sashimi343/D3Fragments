using System.Text.Json;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D3Fragments.AspNetCore.Tests;

public class DomainProblemDetailsTests
{
    private static DefaultHttpContext CreateContext(Action<DomainProblemDetailsOptions>? configure = null)
    {
        var services = new ServiceCollection().AddLogging().AddD3FragmentsAspNetCore(configure).BuildServiceProvider();
        return new DefaultHttpContext { RequestServices = services, Response = { Body = new MemoryStream() } };
    }

    private static JsonElement ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body).RootElement;
    }

    [Fact]
    public async Task DomainException_既定の対応表とerrorCodeキーでProblemDetailsに変換される()
    {
        var context = CreateContext();
        var handler = context.RequestServices.GetServices<IExceptionHandler>().Single();
        var exception = new DomainException([Error.InvalidState("first", code: "E001"), Error.Validation("second")]);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        var body = ReadBody(context);
        Assert.Equal("InvalidState", body.GetProperty("title").GetString());
        Assert.Equal("first\nsecond", body.GetProperty("detail").GetString());
        Assert.Equal("E001", body.GetProperty("errorCode").GetString());
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task DomainException以外は処理しない()
    {
        var context = CreateContext();
        var handler = context.RequestServices.GetServices<IExceptionHandler>().Single();

        Assert.False(await handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None));
    }

    [Fact]
    public async Task Result失敗_オプションで対応表とキー名を上書きできる()
    {
        var context = CreateContext(o =>
        {
            o.StatusCodeMap[ErrorType.InvalidState] = StatusCodes.Status422UnprocessableEntity;
            o.ErrorCodeKey = "error_code";
        });

        await Result<int>.Failure(Error.InvalidState("failed", code: "E002")).ToHttpResult().ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status422UnprocessableEntity, context.Response.StatusCode);
        var body = ReadBody(context);
        Assert.Equal("E002", body.GetProperty("error_code").GetString());
        Assert.False(body.TryGetProperty("errorCode", out _));
    }

    [Fact]
    public async Task Result失敗_エラーコードがなければメンバーを出力しない()
    {
        var context = CreateContext();

        await Result.Failure(Error.NotFound("missing")).ToHttpResult().ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.False(ReadBody(context).TryGetProperty("errorCode", out _));
    }

    [Fact]
    public async Task Result成功_値を200で返す()
    {
        var context = CreateContext();

        await Result<int>.Success(42).ToHttpResult().ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(42, ReadBody(context).GetInt32());
    }

    [Fact]
    public async Task Result成功_値なしは204を返す()
    {
        var context = CreateContext();

        await Result.Success().ToHttpResult().ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
    }

    [Fact]
    public async Task Result成功_変換関数を指定した場合はその結果を返す()
    {
        var context = CreateContext();

        await Result<int>.Success(42).ToHttpResult(x => TypedResults.Created($"/items/{x}", x)).ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.Equal("/items/42", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Result失敗_変換関数を指定しても呼ばずにProblemDetailsを返す()
    {
        var context = CreateContext();

        await Result<int>.Failure(Error.NotFound("missing")).ToHttpResult(_ => throw new InvalidOperationException()).ExecuteAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
    }

    [Fact]
    public void ToHttpResult_nullは例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => ((Result)null!).ToHttpResult());
        Assert.Throws<ArgumentNullException>(() => ((Result<int>)null!).ToHttpResult());
        Assert.Throws<ArgumentNullException>(() => ((Result<int>)null!).ToHttpResult(_ => TypedResults.Ok()));
        Assert.Throws<ArgumentNullException>(() => Result<int>.Success(1).ToHttpResult(null!));
    }

    [Fact]
    public void AddD3FragmentsAspNetCore_MinimalAPIとMVCのJSON設定に値オブジェクトのコンバーターを登録する()
    {
        using var services = new ServiceCollection().AddLogging().AddD3FragmentsAspNetCore().BuildServiceProvider();

        Assert.Contains(
            services.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions.Converters,
            x => x is ValueObjectJsonConverterFactory);
        Assert.Contains(
            services.GetRequiredService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>().Value.JsonSerializerOptions.Converters,
            x => x is ValueObjectJsonConverterFactory);
    }

    [Fact]
    public void AddD3FragmentsAspNetCore_nullは例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddD3FragmentsAspNetCore());
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public List<(string Category, LogLevel Level, string Message)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(this, categoryName);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(RecordingLoggerProvider provider, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                provider.Entries.Add((category, logLevel, formatter(state, exception)));
        }
    }

    [Fact]
    public async Task DomainException_種類を含めてInformationでログに出力する()
    {
        var logs = new RecordingLoggerProvider();
        var services = new ServiceCollection().AddLogging(x => x.AddProvider(logs)).AddD3FragmentsAspNetCore().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services, Response = { Body = new MemoryStream() } };
        var handler = services.GetServices<IExceptionHandler>().Single();

        await handler.TryHandleAsync(context, new DomainException(ErrorType.Conflict, "conflict"), CancellationToken.None);

        var entry = Assert.Single(logs.Entries, x => x.Category.EndsWith("DomainExceptionHandler", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("Conflict", entry.Message, StringComparison.Ordinal);
    }
}
