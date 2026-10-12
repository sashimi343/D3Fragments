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

    /// <summary>
    ///     <para xml:lang="en">Gets the path prefixes that are excluded from the check (e.g. a webhook endpoint). Matching is performed by path segment.</para>
    ///     <para xml:lang="ja">検証の対象外とするパスのプレフィックス（Webhookのエンドポイント等）を取得する。パスセグメント単位で照合する。</para>
    /// </summary>
    /// <value>
    ///     <para xml:lang="en">The list of path prefixes, each starting with <c>/</c>. The default is empty.</para>
    ///     <para xml:lang="ja"><c>/</c>で始まるパスのプレフィックスのリスト。既定値は空。</para>
    /// </value>
    public IList<string> BypassPaths { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Determines whether a request with the specified method and path must include the header specified by <see cref="RequiredHeaderName"/>.</para>
    ///     <para xml:lang="ja">指定したメソッドとパスのリクエストが<see cref="RequiredHeaderName"/>で指定したヘッダを必須とするかどうかを判定する。</para>
    /// </summary>
    /// <param name="method">
    ///     <para xml:lang="en">The HTTP method.</para>
    ///     <para xml:lang="ja">HTTPメソッド。</para>
    /// </param>
    /// <param name="path">
    ///     <para xml:lang="en">The request path.</para>
    ///     <para xml:lang="ja">リクエストパス。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><see langword="true"/> if the method is not one of the safe methods GET, HEAD, OPTIONS and TRACE and the path is not under <see cref="BypassPaths"/>; otherwise, <see langword="false"/>.</para>
    ///     <para xml:lang="ja">メソッドが安全なメソッド（GET、HEAD、OPTIONS、TRACE）以外で、かつパスが<see cref="BypassPaths"/>の配下でない場合は<see langword="true"/>、それ以外は<see langword="false"/>。</para>
    /// </returns>
    public bool RequiresHeader(string method, PathString path) =>
        !(HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method))
        && !BypassPaths.Any(bypassPath => path.StartsWithSegments(bypassPath));
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
    ///     <para xml:lang="en">Requests with the safe methods GET, HEAD, OPTIONS and TRACE, and requests under <see cref="CsrfProtectionOptions.BypassPaths"/> are not checked. A rejected request receives 400 (Bad Request) as a Problem Details response. Because browsers cannot add a custom header to a cross-origin request without a CORS preflight, this protects cookie-authenticated APIs whose CORS policy does not allow untrusted origins.</para>
    ///     <para xml:lang="ja">安全なメソッド（GET、HEAD、OPTIONS、TRACE）のリクエストと、<see cref="CsrfProtectionOptions.BypassPaths"/>配下のリクエストは検証しない。拒否したリクエストには400（Bad Request）をProblem Details応答として返す。ブラウザはCORSプリフライトなしにクロスオリジンのリクエストへカスタムヘッダを付与できないため、信頼できないオリジンを許可しないCORSポリシーを持つCookie認証のAPIを保護できる。</para>
    /// </remarks>
    /// <param name="app">
    ///     <para xml:lang="en">The application builder.</para>
    ///     <para xml:lang="ja">アプリケーションビルダー。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The same application builder for chaining.</para>
    ///     <para xml:lang="ja">メソッドチェーン用の同じアプリケーションビルダー。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="app"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="app"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<CsrfProtectionMiddleware>();
    }
}

internal sealed class CsrfProtectionMiddleware(RequestDelegate next, IOptions<CsrfProtectionOptions> options)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headerName = options.Value.RequiredHeaderName;
        if (!options.Value.RequiresHeader(context.Request.Method, context.Request.Path)
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
