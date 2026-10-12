namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Dispatches domain events to <see cref="IBeforeCommitDomainEventHandler{TEvent}"/> and <see cref="IAfterCommitDomainEventHandler{TEvent}"/>.</para>
///     <para xml:lang="ja">ドメインイベントを<see cref="IBeforeCommitDomainEventHandler{TEvent}"/>および<see cref="IAfterCommitDomainEventHandler{TEvent}"/>へディスパッチするインターフェース。</para>
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    ///     <para xml:lang="en">Dispatches a domain event to the <see cref="IBeforeCommitDomainEventHandler{TEvent}"/> instances asynchronously. An exception thrown by a handler propagates to the caller.</para>
    ///     <para xml:lang="ja">ドメインイベントを<see cref="IBeforeCommitDomainEventHandler{TEvent}"/>へ非同期的にディスパッチする。ハンドラーがスローした例外は呼び出し元へ伝播する。</para>
    /// </summary>
    /// <param name="domainEvent">
    ///     <para xml:lang="en">The domain event to dispatch.</para>
    ///     <para xml:lang="ja">ディスパッチするドメインイベント。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task DispatchBeforeCommitAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Dispatches a domain event to the <see cref="IAfterCommitDomainEventHandler{TEvent}"/> instances asynchronously. An exception thrown by a handler does not propagate to the caller.</para>
    ///     <para xml:lang="ja">ドメインイベントを<see cref="IAfterCommitDomainEventHandler{TEvent}"/>へ非同期的にディスパッチする。ハンドラーがスローした例外は呼び出し元へ伝播しない。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">No cancellation token is accepted, because the side effects of committed changes must not be cancelled by the caller.</para>
    ///     <para xml:lang="ja">確定した変更の副作用を呼び出し元がキャンセルできないよう、キャンセルトークンは受け取らない。</para>
    /// </remarks>
    /// <param name="domainEvent">
    ///     <para xml:lang="en">The domain event to dispatch.</para>
    ///     <para xml:lang="ja">ディスパッチするドメインイベント。</para>
    /// </param>
    Task DispatchAfterCommitAsync(IDomainEvent domainEvent);
}
