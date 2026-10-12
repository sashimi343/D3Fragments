namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Handles a domain event of type <typeparamref name="TEvent"/> after the transaction has been committed.</para>
///     <para xml:lang="ja">指定した型のドメインイベントを、トランザクションのCommit成功後に処理するハンドラーのインターフェース。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Use this handler for external I/O (sending e-mails, calling external APIs, and so on). An exception thrown by the handler is logged and does not propagate to the caller of the command, because the business data has already been committed; the remaining handlers are still invoked. The handler receives <see cref="CancellationToken.None"/>, so that the side effects of committed changes are not cancelled by the caller (for example, by a client disconnect). The handler must not modify entities directly, because the changes are not saved; to update the database, execute another command through <see cref="IRequestExecutor{TRequest, TResponse}"/>. Delivery is best effort: if the process stops after the commit, the handler is not invoked.</para>
///     <para xml:lang="ja">外部I/O（メール送信、外部APIの呼び出し等）に使用する。業務データは確定済みのため、ハンドラーがスローした例外はログに出力し、Commandの呼び出し元へは伝播しない。残りのハンドラーは引き続き呼び出す。確定した変更の副作用が呼び出し元（クライアントの切断等）によってキャンセルされないよう、ハンドラーには<see cref="CancellationToken.None"/>を渡す。ハンドラーによる変更は保存されないため、エンティティを直接変更してはならない。DBを更新する場合は<see cref="IRequestExecutor{TRequest, TResponse}"/>で別のCommandを実行すること。配信はベストエフォートであり、Commit後にプロセスが停止した場合、ハンドラーは呼び出されない。</para>
/// </remarks>
/// <typeparam name="TEvent">
///     <para xml:lang="en">The type of the domain event to handle.</para>
///     <para xml:lang="ja">処理対象のドメインイベントの型。</para>
/// </typeparam>
public interface IAfterCommitDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    ///     <para xml:lang="en">Handles a domain event asynchronously.</para>
    ///     <para xml:lang="ja">ドメインイベントを非同期的に処理する。</para>
    /// </summary>
    /// <param name="domainEvent">
    ///     <para xml:lang="en">The domain event to handle.</para>
    ///     <para xml:lang="ja">処理対象のドメインイベント。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token. <see cref="CancellationToken.None"/> is passed by the default dispatcher.</para>
    ///     <para xml:lang="ja">キャンセルトークン。既定のディスパッチャーは<see cref="CancellationToken.None"/>を渡す。</para>
    /// </param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
