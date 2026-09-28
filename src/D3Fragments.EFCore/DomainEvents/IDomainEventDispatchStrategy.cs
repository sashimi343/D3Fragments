using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore.DomainEvents;

/// <summary>
///     <para xml:lang="en">Strategy for dispatching domain events, invoked by <see cref="EfUnitOfWork{TContext}"/> around the save/commit boundary. Swap the DI registration to change how events are delivered (immediate in-process dispatch, an Outbox, etc.) without changing any Handler or Repository.</para>
///     <para xml:lang="ja">ドメインイベントのディスパッチ戦略。<see cref="EfUnitOfWork{TContext}"/>が保存・Commit境界の前後で呼び出す。DI登録を切り替えるだけで配信方式（即時ディスパッチ、Outbox等）を変更でき、HandlerやRepositoryを変更する必要はない。</para>
/// </summary>
public interface IDomainEventDispatchStrategy
{
    /// <summary>
    ///     <para xml:lang="en">Called before <c>SaveChangesAsync</c>. An Outbox strategy adds Outbox rows to the same <see cref="DbContext"/> here so they are persisted atomically with the business data. Immediate/Channel strategies do nothing.</para>
    ///     <para xml:lang="ja"><c>SaveChangesAsync</c>の前に呼ばれる。Outbox戦略は同じ<see cref="DbContext"/>へOutbox行を追加し、業務データと原子的に保存されるようにする。Immediate/Channel戦略は何もしない。</para>
    /// </summary>
    /// <param name="dbContext">
    ///     <para xml:lang="en">The <see cref="DbContext"/> whose changes are about to be saved.</para>
    ///     <para xml:lang="ja">これから保存される<see cref="DbContext"/>。</para>
    /// </param>
    /// <param name="events">
    ///     <para xml:lang="en">The domain events raised in this unit of work.</para>
    ///     <para xml:lang="ja">この単位のユニットオブワークで発行されたドメインイベント一覧。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task PrepareAsync(DbContext dbContext, IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Called after the Unit of Work's transaction has committed successfully.</para>
    ///     <para xml:lang="ja">Unit of WorkがDBトランザクションのCommitに成功した後に呼ばれる。</para>
    /// </summary>
    /// <param name="events">
    ///     <para xml:lang="en">The domain events raised in this unit of work.</para>
    ///     <para xml:lang="ja">この単位のユニットオブワークで発行されたドメインイベント一覧。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task DispatchAfterCommitAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken);
}
