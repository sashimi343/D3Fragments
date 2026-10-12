using D3Fragments.Application;

using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore.DomainEvents;

/// <summary>
///     <para xml:lang="en">Delivers domain events to <see cref="IAfterCommitDomainEventHandler{TEvent}"/> in-process, immediately after the transaction commits. Delivery is not persisted, so a handler failure is only logged and is not retried, and the handlers are not invoked if the process stops after the commit.</para>
///     <para xml:lang="ja">トランザクションのCommit直後、インプロセスでドメインイベントを<see cref="IAfterCommitDomainEventHandler{TEvent}"/>へ配信する戦略。配信は永続化されないため、ハンドラーの失敗はログに出力するのみで再試行されず、Commit後にプロセスが停止した場合はハンドラーが呼び出されない。</para>
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
            await dispatcher.DispatchAfterCommitAsync(domainEvent).ConfigureAwait(false);
        }
    }
}
