namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents a <see cref="DomainException"/> that indicates a domain validation failure.</para>
///     <para xml:lang="ja">ドメイン固有のバリデーションに失敗したことを表す <see cref="DomainException"/>。</para>
/// </summary>
public class DomainValidationException : DomainException
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="DomainValidationException"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="DomainValidationException"/> を初期化する。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">The validation error message.</para>
    ///     <para xml:lang="ja">バリデーションエラーメッセージ。</para>
    /// </param>
    public DomainValidationException(string message) : base(ErrorType.Validation, message)
    {
    }
}
