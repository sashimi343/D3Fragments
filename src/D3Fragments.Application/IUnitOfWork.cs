namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Manages the atomicity of a command's changes. Implemented by the infrastructure layer (e.g. an EF Core <c>DbContext</c> wrapper).</para>
///     <para xml:lang="ja">Commandの変更の原子性を管理する。Infrastructure層（EF Coreの<c>DbContext</c>ラッパー等）が実装する。</para>
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    ///     <para xml:lang="en">Saves the tracked changes.</para>
    ///     <para xml:lang="ja">追跡中の変更を保存する。</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The number of affected rows.</para>
    ///     <para xml:lang="ja">影響を受けた行数。</para>
    /// </returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Discards the tracked changes without saving them.</para>
    ///     <para xml:lang="ja">追跡中の変更を保存せずに破棄する。</para>
    /// </summary>
    void DiscardChanges();

    /// <summary>
    ///     <para xml:lang="en">Runs <paramref name="action"/> inside an explicit database transaction, committing when <paramref name="shouldCommit"/> returns <c>true</c> for the produced response and rolling back (discarding tracked changes) otherwise, including when an exception is thrown or the operation is canceled.</para>
    ///     <para xml:lang="ja"><paramref name="action"/>を明示的なDBトランザクション内で実行する。生成された応答に対し<paramref name="shouldCommit"/>が<c>true</c>を返す場合はCommitし、それ以外（例外送出時・キャンセル時を含む）はRollbackして追跡中の変更も破棄する。</para>
    /// </summary>
    /// <typeparam name="TResponse">
    ///     <para xml:lang="en">The type of the response produced by <paramref name="action"/>.</para>
    ///     <para xml:lang="ja"><paramref name="action"/>が生成する応答の型。</para>
    /// </typeparam>
    /// <param name="action">
    ///     <para xml:lang="en">The operation to run inside the transaction.</para>
    ///     <para xml:lang="ja">トランザクション内で実行する処理。</para>
    /// </param>
    /// <param name="shouldCommit">
    ///     <para xml:lang="en">Determines, from the produced response, whether the transaction should be committed.</para>
    ///     <para xml:lang="ja">生成された応答から、トランザクションをCommitすべきかどうかを判定する。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> action,
        Func<TResponse, bool> shouldCommit,
        CancellationToken cancellationToken);
}
