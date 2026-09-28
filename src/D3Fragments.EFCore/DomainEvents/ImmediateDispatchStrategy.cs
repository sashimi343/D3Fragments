using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore.DomainEvents;

/// <summary>
///     <para xml:lang="en">Dispatches domain events synchronously and in-process, immediately after the transaction commits. Delivery is not persisted, so a dispatch failure after commit is not retried; use an Outbox-based strategy when guaranteed delivery is required.</para>
///     <para xml:lang="ja">トランザクションのCommit直後、同期的にインプロセスでドメインイベントをディスパッチする戦略。配信は永続化されないため、Commit後のディスパッチ失敗は再試行されない。確実な配信が必要な場合はOutboxベースの戦略を使用すること。</para>
/// </summary>
public sealed class ImmediateDispatchStrategy(IDomainEventDispatcher dispatcher) : IDomainEventDispatchStrategy
{
    /// <inheritdoc />
    public Task PrepareAsync(DbContext dbContext, IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task DispatchAfterCommitAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in events)
        {
            await dispatcher.DispatchAsync(domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
