namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Handles a domain event of type <typeparamref name="TEvent"/>.</para>
///     <para xml:lang="ja">指定した型のドメインイベントを処理するハンドラーのインターフェース。</para>
/// </summary>
/// <typeparam name="TEvent">
///     <para xml:lang="en">The type of the domain event to handle.</para>
///     <para xml:lang="ja">処理対象のドメインイベントの型。</para>
/// </typeparam>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
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
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
