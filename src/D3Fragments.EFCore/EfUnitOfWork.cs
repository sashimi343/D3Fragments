using D3Fragments.Application;
using D3Fragments.EFCore.DomainEvents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">The <see cref="IUnitOfWork"/> implementation backed by an EF Core <see cref="DbContext"/>. Connects Unit of Work semantics with <see cref="IDomainEventDispatchStrategy"/>: domain events are captured before save, prepared (e.g. persisted as Outbox rows) before <c>SaveChangesAsync</c>, and dispatched only after a successful commit.</para>
///     <para xml:lang="ja">EF Coreの<see cref="DbContext"/>に基づく<see cref="IUnitOfWork"/>実装。Unit of Workのセマンティクスと<see cref="IDomainEventDispatchStrategy"/>を接続する。ドメインイベントは保存前に捕捉され、<c>SaveChangesAsync</c>の前に準備（Outbox行としての永続化等）され、Commit成功後にのみディスパッチされる。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">An optimistic concurrency conflict on save (<see cref="DbUpdateConcurrencyException"/>) is converted to <see cref="ConflictOccurredException"/>.</para>
///     <para xml:lang="ja">保存時の楽観的同時実行制御の競合（<see cref="DbUpdateConcurrencyException"/>）は<see cref="ConflictOccurredException"/>に変換する。</para>
/// </remarks>
/// <typeparam name="TContext">
///     <para xml:lang="en">The type of the <see cref="DbContext"/>.</para>
///     <para xml:lang="ja"><see cref="DbContext"/>の型。</para>
/// </typeparam>
public sealed class EfUnitOfWork<TContext>(
    TContext dbContext,
    IDomainEventDispatchStrategy dispatchStrategy)
    : IUnitOfWork
    where TContext : DbContext
{
    private readonly HashSet<IHasDomainEvents> _pendingSources = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<IDomainEvent> _pendingEvents = new(ReferenceEqualityComparer.Instance);

    private IDbContextTransaction? _transaction;

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        var newEvents = CaptureDomainEvents();

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
            await CompleteDomainEventsAsync(cancellationToken).ConfigureAwait(false);
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        _transaction = transaction;

        try
        {
            var response = await action(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            await CompleteDomainEventsAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch
        {
            // action が例外を投げた場合はここに来る。await using の transaction は
            // Commit されていないまま Dispose されるため、EF Core が自動的にロールバックする。
            DiscardChanges();
            throw;
        }
        finally
        {
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public void DiscardChanges()
    {
        _pendingSources.Clear();
        _pendingEvents.Clear();
        dbContext.ChangeTracker.Clear();
    }

    private IReadOnlyList<IDomainEvent> CaptureDomainEvents()
    {
        var newEvents = new List<IDomainEvent>();
        var sources = dbContext.ChangeTracker.Entries()
            .Select(x => x.Entity)
            .OfType<IHasDomainEvents>()
            .Where(x => x.DomainEvents.Count > 0);

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

    private async Task CompleteDomainEventsAsync(CancellationToken cancellationToken)
    {
        var events = _pendingEvents.ToArray();

        foreach (var source in _pendingSources)
        {
            source.ClearDomainEvents();
        }

        _pendingSources.Clear();
        _pendingEvents.Clear();

        await dispatchStrategy.DispatchAfterCommitAsync(events, cancellationToken).ConfigureAwait(false);
    }
}
