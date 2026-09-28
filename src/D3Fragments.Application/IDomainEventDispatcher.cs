namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Dispatches domain events.</para>
///     <para xml:lang="ja">ドメインイベントをディスパッチするインターフェース。</para>
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    ///     <para xml:lang="en">Dispatches a domain event asynchronously.</para>
    ///     <para xml:lang="ja">ドメインイベントを非同期的にディスパッチする。</para>
    /// </summary>
    /// <param name="domainEvent">
    ///     <para xml:lang="en">The domain event to dispatch.</para>
    ///     <para xml:lang="ja">ディスパッチするドメインイベント。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
