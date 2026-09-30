using Microsoft.AspNetCore.Http;

namespace D3Fragments.AspNetCore;

/// <summary>
///     <para xml:lang="en">Provides options for converting domain errors (<see cref="DomainException"/> and failed <see cref="Result"/>) into Problem Details responses.</para>
///     <para xml:lang="ja">ドメインエラー（<see cref="DomainException"/>および失敗した<see cref="Result"/>）をProblem Details応答へ変換する際のオプションを提供する。</para>
/// </summary>
public sealed class DomainProblemDetailsOptions
{
    /// <summary>
    ///     <para xml:lang="en">The default extension member name that holds the application-specific error code.</para>
    ///     <para xml:lang="ja">アプリケーション固有のエラーコードを格納する拡張メンバー名の既定値。</para>
    /// </summary>
    public const string DefaultErrorCodeKey = "errorCode";

    /// <summary>
    ///     <para xml:lang="en">Gets the mapping from <see cref="ErrorType"/> to an HTTP status code. Entries can be added or overwritten.</para>
    ///     <para xml:lang="ja"><see cref="ErrorType"/>からHTTPステータスコードへの対応表を取得する。要素は追加・上書きできる。</para>
    /// </summary>
    /// <value>
    ///     <para xml:lang="en">The mapping. An <see cref="ErrorType"/> that is not in the mapping is converted to 400 (Bad Request).</para>
    ///     <para xml:lang="ja">対応表。対応表に存在しない<see cref="ErrorType"/>は400（Bad Request）に変換する。</para>
    /// </value>
    public IDictionary<ErrorType, int> StatusCodeMap { get; } = new Dictionary<ErrorType, int>
    {
        [ErrorType.Unauthenticated] = StatusCodes.Status401Unauthorized,
        [ErrorType.Validation] = StatusCodes.Status400BadRequest,
        [ErrorType.NotFound] = StatusCodes.Status404NotFound,
        [ErrorType.Conflict] = StatusCodes.Status409Conflict,
        [ErrorType.Forbidden] = StatusCodes.Status403Forbidden,
        [ErrorType.InvalidState] = StatusCodes.Status409Conflict,
        [ErrorType.PreconditionFailed] = StatusCodes.Status422UnprocessableEntity,
    };

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the extension member name that holds the application-specific error code (<see cref="Error.Code"/> or <see cref="DomainException.ErrorCode"/>).</para>
    ///     <para xml:lang="ja">アプリケーション固有のエラーコード（<see cref="Error.Code"/>または<see cref="DomainException.ErrorCode"/>）を格納する拡張メンバー名を取得または設定する。</para>
    /// </summary>
    /// <value>
    ///     <para xml:lang="en">The member name. The default is <see cref="DefaultErrorCodeKey"/>. The member is omitted when the error code is <see langword="null"/>.</para>
    ///     <para xml:lang="ja">メンバー名。既定値は<see cref="DefaultErrorCodeKey"/>。エラーコードが<see langword="null"/>の場合、メンバーは出力しない。</para>
    /// </value>
    public string ErrorCodeKey { get; set; } = DefaultErrorCodeKey;
}
