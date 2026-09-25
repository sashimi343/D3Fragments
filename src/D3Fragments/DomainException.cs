namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents a domain-specific exception.</para>
///     <para xml:lang="ja">ドメイン固有の例外を表す例外クラス。</para>
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    ///     <para xml:lang="en">Gets the category of the error that caused this exception.</para>
    ///     <para xml:lang="ja">この例外の原因となったエラーの分類を取得する。</para>
    /// </summary>
    public ErrorType ErrorType { get; }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="DomainException"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="DomainException"/> を初期化する。</para>
    /// </summary>
    /// <param name="errorType">
    ///     <para xml:lang="en">The category of the error.</para>
    ///     <para xml:lang="ja">エラーの分類。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">The exception message.</para>
    ///     <para xml:lang="ja">例外メッセージ。</para>
    /// </param>
    public DomainException(ErrorType errorType, string message) : base(message)
    {
        ErrorType = errorType;
    }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="DomainException"/> with an inner exception.</para>
    ///     <para xml:lang="ja">内部例外を指定して新しい <see cref="DomainException"/> を初期化する。</para>
    /// </summary>
    /// <param name="errorType">
    ///     <para xml:lang="en">The category of the error.</para>
    ///     <para xml:lang="ja">エラーの分類。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">The exception message.</para>
    ///     <para xml:lang="ja">例外メッセージ。</para>
    /// </param>
    /// <param name="innerException">
    ///     <para xml:lang="en">The exception that caused this exception.</para>
    ///     <para xml:lang="ja">この例外の原因となった例外。</para>
    /// </param>
    public DomainException(ErrorType errorType, string message, Exception innerException) : base(message, innerException)
    {
        ErrorType = errorType;
    }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="DomainException"/> from a domain <see cref="Error"/>.</para>
    ///     <para xml:lang="ja">ドメインエラーから新しい <see cref="DomainException"/> を初期化する。</para>
    /// </summary>
    /// <param name="error">
    ///     <para xml:lang="en">The domain error.</para>
    ///     <para xml:lang="ja">ドメインエラー。</para>
    /// </param>
    public DomainException(Error error) : base(error.Message)
    {
        ErrorType = error.Type;
    }

    /// <summary>
    ///     <para xml:lang="en">Throws a <see cref="DomainException"/> built from the first error when <paramref name="result"/> is a failure.</para>
    ///     <para xml:lang="ja"><paramref name="result"/> が失敗している場合、最初のエラーから <see cref="DomainException"/> を生成してスローする。</para>
    /// </summary>
    /// <param name="result">
    ///     <para xml:lang="en">The result to check.</para>
    ///     <para xml:lang="ja">チェック対象の結果。</para>
    /// </param>
    /// <exception cref="DomainException">
    ///     <para xml:lang="en">Thrown when <paramref name="result"/> is a failure.</para>
    ///     <para xml:lang="ja"><paramref name="result"/> が失敗している場合にスローされる。</para>
    /// </exception>
    public static void ThrowIfFailure(Result result)
    {
        if (result.IsFailure)
        {
            throw new DomainException(result.Errors[0]);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Converts this exception back into a domain <see cref="Error"/>.</para>
    ///     <para xml:lang="ja">この例外をドメインエラーに変換する。</para>
    /// </summary>
    public Error GetDomainError() => new(ErrorType, Message);
}
