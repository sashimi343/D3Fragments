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
    ///     <para xml:lang="en">Runs <paramref name="action"/> inside an explicit database transaction, committing when it completes without throwing and rolling back (discarding tracked changes) when it throws or the operation is canceled. Failure is expressed solely through an exception propagating out of <paramref name="action"/>; there is no separate success/failure inspection of the produced response.</para>
    ///     <para xml:lang="ja"><paramref name="action"/>を明示的なDBトランザクション内で実行する。例外を投げずに完了した場合はCommitし、例外送出時・キャンセル時はRollbackして追跡中の変更も破棄する。失敗は<paramref name="action"/>から伝播する例外のみで表現され、生成された応答自体の成功・失敗を別途判定することはない。</para>
    /// </summary>
    /// <typeparam name="TResponse">
    ///     <para xml:lang="en">The type of the response produced by <paramref name="action"/>.</para>
    ///     <para xml:lang="ja"><paramref name="action"/>が生成する応答の型。</para>
    /// </typeparam>
    /// <param name="action">
    ///     <para xml:lang="en">The operation to run inside the transaction.</para>
    ///     <para xml:lang="ja">トランザクション内で実行する処理。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken);
}
