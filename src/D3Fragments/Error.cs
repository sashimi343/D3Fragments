namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents a domain-specific error.</para>
///     <para xml:lang="ja">ドメイン固有のエラー情報を表すクラス。</para>
/// </summary>
public sealed class Error
{
    /// <summary>
    ///     <para xml:lang="en">Gets the error message.</para>
    ///     <para xml:lang="ja">エラーメッセージを取得する。</para>
    /// </summary>
    public string Message { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the category of this error.</para>
    ///     <para xml:lang="ja">エラーの分類を取得する。</para>
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the application-specific error code, if any. Used to attach a custom error code to an API response.</para>
    ///     <para xml:lang="ja">アプリケーション固有のエラーコード（省略可）を取得する。API応答への独自エラーコード付与等に使用する。</para>
    /// </summary>
    public string? Code { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the key that identifies the target of this error (e.g. a field name in an API response or a UI control), if any.</para>
    ///     <para xml:lang="ja">エラーの対象を識別するキー（省略可）を取得する。APIレスポンスの項目や画面コントロール等とエラーを紐づけるために使用する。</para>
    /// </summary>
    public string? Target { get; }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="Error"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="Error"/> を初期化する。</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">The category of the error.</para>
    ///     <para xml:lang="ja">エラーの分類。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">The error message.</para>
    ///     <para xml:lang="ja">エラーメッセージ。</para>
    /// </param>
    /// <param name="code">
    ///     <para xml:lang="en">An optional application-specific error code.</para>
    ///     <para xml:lang="ja">アプリケーション固有のエラーコード（省略可）。</para>
    /// </param>
    /// <param name="target">
    ///     <para xml:lang="en">An optional key that identifies the target of the error.</para>
    ///     <para xml:lang="ja">エラーの対象を識別するキー（省略可）。</para>
    /// </param>
    public Error(ErrorType type, string message, string? code = null, string? target = null)
    {
        Type = type;
        Message = message;
        Code = code;
        Target = target;
    }

    /// <summary>
    ///     <para xml:lang="en">Creates an <see cref="Unauthenticated"/> error.</para>
    ///     <para xml:lang="ja">未認証エラーを生成する。</para>
    /// </summary>
    public static Error Unauthenticated(string message, string? code = null, string? target = null) => new(ErrorType.Unauthenticated, message, code, target);

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="Validation"/> error.</para>
    ///     <para xml:lang="ja">バリデーションエラーを生成する。</para>
    /// </summary>
    public static Error Validation(string message, string? code = null, string? target = null) => new(ErrorType.Validation, message, code, target);

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="NotFound"/> error.</para>
    ///     <para xml:lang="ja">NotFoundエラーを生成する。</para>
    /// </summary>
    public static Error NotFound(string message, string? code = null, string? target = null) => new(ErrorType.NotFound, message, code, target);

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="Conflict"/> error.</para>
    ///     <para xml:lang="ja">Conflictエラーを生成する。</para>
    /// </summary>
    public static Error Conflict(string message, string? code = null, string? target = null) => new(ErrorType.Conflict, message, code, target);

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="Forbidden"/> error.</para>
    ///     <para xml:lang="ja">Forbiddenエラーを生成する。</para>
    /// </summary>
    public static Error Forbidden(string message, string? code = null, string? target = null) => new(ErrorType.Forbidden, message, code, target);

    /// <summary>
    ///     <para xml:lang="en">Creates an <see cref="InvalidState"/> error.</para>
    ///     <para xml:lang="ja">InvalidStateエラーを生成する。</para>
    /// </summary>
    public static Error InvalidState(string message, string? code = null, string? target = null) => new(ErrorType.InvalidState, message, code, target);

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="PreconditionFailed"/> error.</para>
    ///     <para xml:lang="ja">PreconditionFailedエラーを生成する。</para>
    /// </summary>
    public static Error PreconditionFailed(string message, string? code = null, string? target = null) => new(ErrorType.PreconditionFailed, message, code, target);

    /// <inheritdoc />
    public override string ToString() => Message;
}
