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
    ///    <para xml:lang="en">Gets or sets the project-specific error code associated with this exception.</para>
    ///    <para xml:lang="ja">この例外に関連付けられたプロジェクト固有のエラーコードを取得または設定する。</para>
    /// </summary>
    public string? ErrorCode { get; set; }

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
    ///     <para xml:lang="en">Converts this exception back into a domain <see cref="Error"/>.</para>
    ///     <para xml:lang="ja">この例外をドメインエラーに変換する。</para>
    /// </summary>
    public Error GetDomainError() => new(ErrorType, Message);
}

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
    public DomainValidationException(string message) : base(ErrorType.Validation, message) { }
}

/// <summary>
///    <para xml:lang="en">Represents a <see cref="DomainException"/> that indicates a domain entity was not found.</para>
///    <para xml:lang="ja">ドメイン固有のエンティティが見つからなかったことを表す <see cref="DomainException"/>。</para>
/// </summary>
public class EntityNotFoundException : DomainException
{
    /// <summary>
    ///    <para xml:lang="en">Initializes a new <see cref="EntityNotFoundException"/>.</para>
    ///    <para xml:lang="ja">新しい <see cref="EntityNotFoundException"/> を初期化する。</para>
    /// </summary>
    public EntityNotFoundException() : base(ErrorType.NotFound, ExceptionMessages.EntityNotFoundException_DefaultMessage) { }

    /// <summary>
    ///   <para xml:lang="en">Initializes a new <see cref="EntityNotFoundException"/> with the name of the entity that was not found.</para>
    ///   <para xml:lang="ja">見つからなかったエンティティの名前を指定して新しい <see cref="EntityNotFoundException"/> を初期化する。</para>
    /// </summary>
    /// <param name="entityName">
    ///     <para xml:lang="en">The name of the entity that was not found.</para>
    ///     <para xml:lang="ja">見つからなかったエンティティの名前。</para>
    /// </param>
    public EntityNotFoundException(string entityName) : base(ErrorType.NotFound, string.Format(ExceptionMessages.EntityNotFoundException_EntityName, entityName)) { }

    /// <summary>
    ///   <para xml:lang="en">Initializes a new <see cref="EntityNotFoundException"/> with the name of the entity that was not found and an optional message.</para>
    ///   <para xml:lang="ja">見つからなかったエンティティの名前と任意のメッセージを指定して新しい <see cref="EntityNotFoundException"/> を初期化する。</para>
    /// </summary>
    /// <param name="entityName">
    ///     <para xml:lang="en">The name of the entity that was not found.</para>
    ///     <para xml:lang="ja">見つからなかったエンティティの名前。</para>
    /// </param>
    /// <param name="optionalMessage">
    ///     <para xml:lang="en">An optional message providing additional context.</para>
    ///     <para xml:lang="ja">追加のコンテキストを提供する任意のメッセージ。</para>
    /// </param>
    public EntityNotFoundException(string entityName, string optionalMessage) : base(ErrorType.NotFound, string.Format(ExceptionMessages.EntityNotFoundException_EntityNameAndOptionalMessage, entityName, optionalMessage)) { }
}

/// <summary>
///   <para xml:lang="en">Represents a <see cref="DomainException"/> that indicates a conflict occurred in the domain.</para>
///   <para xml:lang="ja">ドメイン内で競合が発生したことを表す <see cref="DomainException"/>。</para>
/// </summary>
public class ConflictOccurredException : DomainException
{
    /// <summary>
    ///   <para xml:lang="en">Initializes a new <see cref="ConflictOccurredException"/>.</para>
    ///   <para xml:lang="ja">新しい <see cref="ConflictOccurredException"/> を初期化する。</para>
    /// </summary>
    /// <param name="message"></param>
    public ConflictOccurredException(string message) : base(ErrorType.Conflict, message) { }

    /// <summary>
    ///  <para xml:lang="en">Initializes a new <see cref="ConflictOccurredException"/> with an inner exception.</para>
    ///  <para xml:lang="ja">内部例外を指定して新しい <see cref="ConflictOccurredException"/> を初期化する。</para>
    /// </summary>
    /// <param name="format">
    ///     <para xml:lang="en">A composite format string.</para>
    ///     <para xml:lang="ja">複合書式指定文字列。</para>
    /// </param>
    /// <param name="args">
    ///     <para xml:lang="en">An array of objects to format.</para>
    ///     <para xml:lang="ja">書式設定するオブジェクトの配列。</para>
    /// </param>
    public ConflictOccurredException(string format, params object[] args) : base(ErrorType.Conflict, string.Format(format, args)) { }

    /// <summary>
    ///   <para xml:lang="en">Initializes a new <see cref="ConflictOccurredException"/> with an inner exception.</para>
    ///   <para xml:lang="ja">内部例外を指定して新しい <see cref="ConflictOccurredException"/> を初期化する。</para>
    /// </summary>
    /// <param name="innerException">
    ///     <para xml:lang="en">The inner exception.</para>
    ///     <para xml:lang="ja">内部例外。</para>
    /// </param>
    /// <param name="format">
    ///     <para xml:lang="en">A composite format string.</para>
    ///     <para xml:lang="ja">複合書式指定文字列。</para>
    /// </param>
    /// <param name="args">
    ///     <para xml:lang="en">An array of objects to format.</para>
    ///     <para xml:lang="ja">書式設定するオブジェクトの配列。</para>
    /// </param>
    public ConflictOccurredException(Exception innerException, string format, params object[] args) : base(ErrorType.Conflict, string.Format(format, args), innerException) { }
}
