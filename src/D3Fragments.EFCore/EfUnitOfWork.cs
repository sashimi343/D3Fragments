using System.Globalization;

using D3Fragments.Application;
using D3Fragments.EFCore.DomainEvents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">The <see cref="IUnitOfWork"/> implementation backed by an EF Core <see cref="DbContext"/>. Domain events are dispatched to <see cref="IBeforeCommitDomainEventHandler{TEvent}"/> before save within the same transaction, prepared by <see cref="IDomainEventDispatchStrategy"/> (e.g. persisted as Outbox rows) before <c>SaveChangesAsync</c>, and delivered to <see cref="IAfterCommitDomainEventHandler{TEvent}"/> by <see cref="IDomainEventDispatchStrategy"/> only after a successful commit.</para>
///     <para xml:lang="ja">EF Coreの<see cref="DbContext"/>に基づく<see cref="IUnitOfWork"/>実装。ドメインイベントは、保存前に同一トランザクション内で<see cref="IBeforeCommitDomainEventHandler{TEvent}"/>へディスパッチし、<c>SaveChangesAsync</c>の前に<see cref="IDomainEventDispatchStrategy"/>で準備（Outbox行としての永続化等）し、Commit成功後にのみ<see cref="IDomainEventDispatchStrategy"/>で<see cref="IAfterCommitDomainEventHandler{TEvent}"/>へ配信する。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Before-commit dispatching is repeated while the handlers raise new domain events, up to 10 rounds; an <see cref="InvalidOperationException"/> is thrown when events are still raised after that. An optimistic concurrency conflict on save (<see cref="DbUpdateConcurrencyException"/>) is converted to <see cref="ConflictOccurredException"/>. When unsaved changes remain after the after-commit delivery (an after-commit handler modified entities directly), they are discarded and an error is logged with <see cref="D3FragmentsApplicationOptions.AfterCommitChangesDiscardedEventId"/>.</para>
///     <para xml:lang="ja">Commit前のディスパッチは、ハンドラーが新たなドメインイベントを発行する限り最大10回繰り返し、それでも発行される場合は<see cref="InvalidOperationException"/>をスローする。保存時の楽観的同時実行制御の競合（<see cref="DbUpdateConcurrencyException"/>）は<see cref="ConflictOccurredException"/>に変換する。Commit後の配信の後に未保存の変更が残っていた（Commit後のハンドラーがエンティティを直接変更した）場合は、変更を破棄し、<see cref="D3FragmentsApplicationOptions.AfterCommitChangesDiscardedEventId"/>のエラーログを出力する。</para>
/// </remarks>
/// <typeparam name="TContext">
///     <para xml:lang="en">The type of the <see cref="DbContext"/>.</para>
///     <para xml:lang="ja"><see cref="DbContext"/>の型。</para>
/// </typeparam>
public sealed class EfUnitOfWork<TContext>(
    TContext dbContext,
    IDomainEventDispatcher dispatcher,
    IDomainEventDispatchStrategy dispatchStrategy,
    ILogger<EfUnitOfWork<TContext>> logger,
    IOptions<D3FragmentsApplicationOptions> options)
    : IUnitOfWork
    where TContext : DbContext
{
    // Commit前のハンドラー同士がイベントを循環して発行した場合に無限ループさせないための上限
    private const int MaxBeforeCommitDispatchRounds = 10;

    private readonly HashSet<IHasDomainEvents> _pendingSources = new(ReferenceEqualityComparer.Instance);

    // Commit前のディスパッチ済みで、Commit後の配信を待っているイベント
    private readonly HashSet<IDomainEvent> _pendingEvents = new(ReferenceEqualityComparer.Instance);

    private IDbContextTransaction? _transaction;

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var newEvents = await DispatchBeforeCommitAsync(cancellationToken).ConfigureAwait(false);

        await dispatchStrategy.PrepareAsync(dbContext, newEvents, cancellationToken).ConfigureAwait(false);

        int affectedRows;
        try
        {
            affectedRows = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConflictOccurredException(ex, ExceptionMessages.ConflictOccurredException_ConcurrencyConflict);
        }

        if (_transaction is null)
        {
            await CompleteDomainEventsAsync().ConfigureAwait(false);
        }

        return affectedRows;
    }

    /// <inheritdoc />
    public async Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException(ExceptionMessages.InvalidOperationException_TransactionAlreadyStarted);
        }

        TResponse response;
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            _transaction = transaction;
            try
            {
                response = await action(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Commit されていない transaction は Dispose 時に EF Core が自動的にロールバックする
                DiscardChanges();
                throw;
            }
            finally
            {
                _transaction = null;
            }
        }

        // トランザクションを破棄してから配信する。Commit後のハンドラーがトランザクション付きのCommandを実行できるようにするため
        await CompleteDomainEventsAsync().ConfigureAwait(false);
        return response;
    }

    /// <inheritdoc />
    public void DiscardChanges()
    {
        _pendingSources.Clear();
        _pendingEvents.Clear();
        dbContext.ChangeTracker.Clear();
    }

    // ハンドラーが新たに発行したイベントがなくなるまで、Commit前のディスパッチを繰り返す。今回ディスパッチしたイベントを返す
    private async Task<IReadOnlyList<IDomainEvent>> DispatchBeforeCommitAsync(CancellationToken cancellationToken)
    {
        var dispatchedEvents = new List<IDomainEvent>();
        for (var round = 0; ; round++)
        {
            var newEvents = CaptureDomainEvents();
            if (newEvents.Count == 0)
            {
                return dispatchedEvents;
            }

            if (round == MaxBeforeCommitDispatchRounds)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    ExceptionMessages.InvalidOperationException_BeforeCommitDispatchLimitExceeded,
                    MaxBeforeCommitDispatchRounds,
                    string.Join(", ", newEvents.Select(x => x.GetType().FullName).Distinct())));
            }

            dispatchedEvents.AddRange(newEvents);
            foreach (var domainEvent in newEvents)
            {
                await dispatcher.DispatchBeforeCommitAsync(domainEvent, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private List<IDomainEvent> CaptureDomainEvents()
    {
        var newEvents = new List<IDomainEvent>();
        var sources = dbContext.ChangeTracker.Entries()
            .Select(x => x.Entity)
            .OfType<IHasDomainEvents>()
            .Where(x => x.DomainEvents.Count > 0)
            .ToList();

        foreach (var source in sources)
        {
            _pendingSources.Add(source);

            foreach (var domainEvent in source.DomainEvents)
            {
                if (_pendingEvents.Add(domainEvent))
                {
                    newEvents.Add(domainEvent);
                }
            }
        }

        return newEvents;
    }

    private async Task CompleteDomainEventsAsync()
    {
        var events = _pendingEvents.ToArray();

        foreach (var source in _pendingSources)
        {
            source.ClearDomainEvents();
        }

        _pendingSources.Clear();
        _pendingEvents.Clear();

        if (events.Length == 0)
        {
            return;
        }

        // 確定した変更の副作用は呼び出し元にキャンセルさせない
        await dispatchStrategy.DispatchAfterCommitAsync(events, CancellationToken.None).ConfigureAwait(false);

        // Commit後のハンドラーがエンティティを直接変更しても保存されないため、破棄してログに出力する。
        // 業務データは確定済みのため、呼び出し元へは例外を伝播しない
        if (dbContext.ChangeTracker.HasChanges())
        {
            var changedEntries = dbContext.ChangeTracker.Entries()
                .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .Select(x => $"{x.Metadata.DisplayName()}: {x.State}");
            logger.LogError(
                options.Value.AfterCommitChangesDiscardedEventId,
                "Changes made after the commit were discarded. After-commit domain event handlers must not modify entities directly; execute another command instead. Discarded changes: {Changes}",
                string.Join(", ", changedEntries));
            dbContext.ChangeTracker.Clear();
        }
    }
}
