using D3Fragments.Application;

namespace D3Fragments.Testing;

/// <summary>
///     <para xml:lang="en">Represents a spy implementation of <see cref="IDomainEventDispatcher"/> that records dispatched domain events.</para>
///     <para xml:lang="ja">ディスパッチされたドメインイベントを記録する<see cref="IDomainEventDispatcher"/>のSpy実装。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">This class does not invoke any domain event handler. Verify handlers in separate tests.</para>
///     <para xml:lang="ja">このクラスはドメインイベントハンドラを呼び出さない。ハンドラは別のテストで検証する。</para>
/// </remarks>
public sealed class SpyDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly List<IDomainEvent> _beforeCommitEvents = [];
    private readonly List<IDomainEvent> _afterCommitEvents = [];

    /// <summary>
    ///     <para xml:lang="en">Gets the domain events dispatched to the before-commit handlers, in dispatch order.</para>
    ///     <para xml:lang="ja">Commit前のハンドラーへディスパッチされたドメインイベントを、ディスパッチ順に取得する。</para>
    /// </summary>
    public IReadOnlyList<IDomainEvent> BeforeCommitEvents => _beforeCommitEvents;

    /// <summary>
    ///     <para xml:lang="en">Gets the domain events dispatched to the after-commit handlers, in dispatch order.</para>
    ///     <para xml:lang="ja">Commit後のハンドラーへディスパッチされたドメインイベントを、ディスパッチ順に取得する。</para>
    /// </summary>
    public IReadOnlyList<IDomainEvent> AfterCommitEvents => _afterCommitEvents;

    /// <inheritdoc/>
    public Task DispatchBeforeCommitAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _beforeCommitEvents.Add(domainEvent);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DispatchAfterCommitAsync(IDomainEvent domainEvent)
    {
        _afterCommitEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}
