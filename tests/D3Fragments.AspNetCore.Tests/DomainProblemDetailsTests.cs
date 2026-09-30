using System.Text.Json;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
}
