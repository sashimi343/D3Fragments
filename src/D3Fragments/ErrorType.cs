namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents the category of a domain-specific error.</para>
///     <para xml:lang="ja">ドメイン固有エラーの分類を表す列挙型。</para>
/// </summary>
public enum ErrorType
{
    /// <summary>
    ///     <para xml:lang="en">The caller could not be authenticated.</para>
    ///     <para xml:lang="ja">未認証（誰であるかを識別できない）であることを表す。</para>
    /// </summary>
    Unauthenticated = 0,

    /// <summary>
    ///     <para xml:lang="en">An input value is invalid.</para>
    ///     <para xml:lang="ja">入力値の妥当性エラーであることを表す。</para>
    /// </summary>
    Validation = 1,

    /// <summary>
    ///     <para xml:lang="en">The requested resource does not exist.</para>
    ///     <para xml:lang="ja">対象が存在しないことを表す。</para>
    /// </summary>
    NotFound = 2,

    /// <summary>
    ///     <para xml:lang="en">The operation conflicts with the current state, such as a duplicate key.</para>
    ///     <para xml:lang="ja">状態の競合（キー重複等）であることを表す。</para>
    /// </summary>
    Conflict = 3,

    /// <summary>
    ///     <para xml:lang="en">The caller is identified but does not have permission to perform the operation.</para>
    ///     <para xml:lang="ja">認可エラー（誰であるかは分かるが権限がない）であることを表す。</para>
    /// </summary>
    Forbidden = 4,

    /// <summary>
    ///     <para xml:lang="en">The entity or aggregate is not in a state that allows the operation.</para>
    ///     <para xml:lang="ja">エンティティ・集約が処理を許容しない状態にあることを表す。</para>
    /// </summary>
    InvalidState = 5,

    /// <summary>
    ///     <para xml:lang="en">A precondition required for the operation is not satisfied.</para>
    ///     <para xml:lang="ja">事前条件を満たしていないことを表す。</para>
    /// </summary>
    PreconditionFailed = 6,
}
