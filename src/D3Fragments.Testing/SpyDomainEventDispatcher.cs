using D3Fragments.Application;

namespace D3Fragments.Testing;

/// <summary>
///     <para xml:lang="en">Represents a spy implementation of <see cref="IDomainEventDispatcher"/> that records dispatched domain events.</para>
///     <para xml:lang="ja">発火されたドメインイベントを記録する<see cref="IDomainEventDispatcher"/>のSpy実装。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">This class does not invoke any domain event handler. Verify handlers in separate tests.</para>
///     <para xml:lang="ja">このクラスはドメインイベントハンドラを呼び出さない。ハンドラは別のテストで検証する。</para>
/// </remarks>
public sealed class SpyDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly List<IDomainEvent> _dispatchedEvents = [];

    /// <summary>
    ///     <para xml:lang="en">Gets the dispatched domain events in dispatch order.</para>
    ///     <para xml:lang="ja">発火されたドメインイベントを発火順に取得する。</para>
    /// </summary>
    public IReadOnlyList<IDomainEvent> DispatchedEvents => _dispatchedEvents;

    /// <inheritdoc/>
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _dispatchedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
