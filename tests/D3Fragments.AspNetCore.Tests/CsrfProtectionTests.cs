using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.AspNetCore.Tests;

public class CsrfProtectionTests
{
    private static async Task<int> SendAsync(string method, string path, string? header = null, Action<CsrfProtectionOptions>? configure = null)
    {
        var services = new ServiceCollection().AddLogging().Configure(configure ?? (_ => { })).BuildServiceProvider();
        var app = new ApplicationBuilder(services);
        app.UseCsrfProtection("/webhook");
        app.Run(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        var httpContext = new DefaultHttpContext { RequestServices = services, Response = { Body = new MemoryStream() } };
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        if (header is not null)
        {
            httpContext.Request.Headers[header] = "any";
        }

        await app.Build()(httpContext);
        return httpContext.Response.StatusCode;
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task 安全なメソッドは検証しない(string method)
    {
        Assert.Equal(StatusCodes.Status200OK, await SendAsync(method, "/api"));
    }

    [Fact]
    public async Task ヘッダがないPOSTは400で拒否する()
    {
        Assert.Equal(StatusCodes.Status400BadRequest, await SendAsync("POST", "/api"));
    }

    [Fact]
    public async Task ヘッダがあるPOSTは通す()
    {
        Assert.Equal(StatusCodes.Status200OK, await SendAsync("POST", "/api", CsrfProtectionOptions.DefaultRequiredHeaderName));
    }

    [Fact]
    public async Task ヘッダ名を変更できる()
    {
        Assert.Equal(StatusCodes.Status400BadRequest,
            await SendAsync("POST", "/api", CsrfProtectionOptions.DefaultRequiredHeaderName, o => o.RequiredHeaderName = "X-Custom"));
        Assert.Equal(StatusCodes.Status200OK, await SendAsync("POST", "/api", "X-Custom", o => o.RequiredHeaderName = "X-Custom"));
    }

    [Fact]
    public async Task 除外パス配下は検証しない()
    {
        Assert.Equal(StatusCodes.Status200OK, await SendAsync("POST", "/webhook/stripe"));
        Assert.Equal(StatusCodes.Status400BadRequest, await SendAsync("POST", "/webhooks"));
    }
}
