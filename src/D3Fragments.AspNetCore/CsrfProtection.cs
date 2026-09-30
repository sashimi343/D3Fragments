using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace D3Fragments.AspNetCore;

/// <summary>
///     <para xml:lang="en">Provides options for the CSRF protection middleware added by <see cref="CsrfProtectionApplicationBuilderExtensions.UseCsrfProtection"/>.</para>
///     <para xml:lang="ja"><see cref="CsrfProtectionApplicationBuilderExtensions.UseCsrfProtection"/>で追加するCSRF対策ミドルウェアのオプションを提供する。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Configure this class with <c>services.Configure&lt;CsrfProtectionOptions&gt;(...)</c>. The defaults are used when it is not configured.</para>
///     <para xml:lang="ja">このクラスは<c>services.Configure&lt;CsrfProtectionOptions&gt;(...)</c>で構成する。構成しない場合は既定値を使用する。</para>
/// </remarks>
public sealed class CsrfProtectionOptions
{
    /// <summary>
    ///     <para xml:lang="en">The default name of the required custom request header.</para>
    ///     <para xml:lang="ja">必須とするカスタムリクエストヘッダ名の既定値。</para>
    /// </summary>
    public const string DefaultRequiredHeaderName = "X-Requested-By";

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the name of the custom request header that non-safe requests must include.</para>
    ///     <para xml:lang="ja">安全でないメソッドのリクエストに必須とするカスタムリクエストヘッダ名を取得または設定する。</para>
    /// </summary>
    /// <value>
    ///     <para xml:lang="en">The header name. The default is <see cref="DefaultRequiredHeaderName"/>. The header value is not checked.</para>
    ///     <para xml:lang="ja">ヘッダ名。既定値は<see cref="DefaultRequiredHeaderName"/>。ヘッダの値は検証しない。</para>
    /// </value>
    public string RequiredHeaderName { get; set; } = DefaultRequiredHeaderName;
}

/// <summary>
///     <para xml:lang="en">Provides extension methods that add the CSRF protection middleware.</para>
///     <para xml:lang="ja">CSRF対策ミドルウェアを追加する拡張メソッドを提供する。</para>
/// </summary>
public static class CsrfProtectionApplicationBuilderExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Adds middleware that rejects requests without the custom request header specified by <see cref="CsrfProtectionOptions.RequiredHeaderName"/>.</para>
    ///     <para xml:lang="ja"><see cref="CsrfProtectionOptions.RequiredHeaderName"/>で指定したカスタムリクエストヘッダを持たないリクエストを拒否するミドルウェアを追加する。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Requests with the safe methods GET, HEAD, OPTIONS and TRACE are not checked. A rejected request receives 400 (Bad Request) as a Problem Details response. Because browsers cannot add a custom header to a cross-origin request without a CORS preflight, this protects cookie-authenticated APIs whose CORS policy does not allow untrusted origins.</para>
    ///     <para xml:lang="ja">安全なメソッド（GET、HEAD、OPTIONS、TRACE）のリクエストは検証しない。拒否したリクエストには400（Bad Request）をProblem Details応答として返す。ブラウザはCORSプリフライトなしにクロスオリジンのリクエストへカスタムヘッダを付与できないため、信頼できないオリジンを許可しないCORSポリシーを持つCookie認証のAPIを保護できる。</para>
    /// </remarks>
    /// <param name="app">
    ///     <para xml:lang="en">The application builder.</para>
    ///     <para xml:lang="ja">アプリケーションビルダー。</para>
    /// </param>
    /// <param name="bypassPaths">
    ///     <para xml:lang="en">The path prefixes that are excluded from the check (e.g. a webhook endpoint). Matching is performed by path segment.</para>
    ///     <para xml:lang="ja">検証の対象外とするパスのプレフィックス（Webhookのエンドポイント等）。パスセグメント単位で照合する。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same application builder for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じアプリケーションビルダー。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="app"/> or <paramref name="bypassPaths"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="app"/>または<paramref name="bypassPaths"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder app, params string[] bypassPaths)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(bypassPaths);

        if (bypassPaths.Length == 0)
        {
            return app.UseMiddleware<CsrfProtectionMiddleware>();
        }

        return app.UseWhen(
            context => !bypassPaths.Any(path => context.Request.Path.StartsWithSegments(path)),
            branch => branch.UseMiddleware<CsrfProtectionMiddleware>());
    }
}

internal sealed class CsrfProtectionMiddleware(RequestDelegate next, IOptions<CsrfProtectionOptions> options)
{
    public Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var headerName = options.Value.RequiredHeaderName;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method)
            || context.Request.Headers.ContainsKey(headerName))
        {
            return next(context);
        }

        return TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            detail: string.Format(CultureInfo.CurrentCulture, ResponseMessages.CsrfProtection_RequiredHeaderMissing, headerName))
            .ExecuteAsync(context);
    }
}
