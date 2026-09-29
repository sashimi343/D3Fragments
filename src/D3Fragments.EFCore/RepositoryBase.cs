using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Base class for EF Core implementations of <see cref="IRepository{TAggregate, TId}"/>. Methods only change the tracked state of <typeparamref name="TContext"/>; saving is done by the Unit of Work.</para>
///     <para xml:lang="ja"><see cref="IRepository{TAggregate, TId}"/>のEF Core実装の基底クラス。各メソッドは<typeparamref name="TContext"/>の追跡状態を変更するのみで、保存はUnit of Workが行う。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Aggregates must be loaded with change tracking enabled. Changes to an aggregate loaded with <c>AsNoTracking()</c> are silently lost. Override <see cref="FindAsync"/> to load child entities (e.g. with <c>Include</c>).</para>
///     <para xml:lang="ja">集約は変更追跡を有効にしたまま取得すること。<c>AsNoTracking()</c>で取得した集約への変更は、エラーにならず失われる。子エンティティを読み込む場合は<see cref="FindAsync"/>をオーバーライドする（<c>Include</c>等）。</para>
/// </remarks>
/// <typeparam name="TContext">
///     <para xml:lang="en">The type of the <see cref="DbContext"/>.</para>
///     <para xml:lang="ja"><see cref="DbContext"/>の型。</para>
/// </typeparam>
/// <typeparam name="TAggregate">
///     <para xml:lang="en">The type of the aggregate root.</para>
///     <para xml:lang="ja">集約ルートの型。</para>
/// </typeparam>
/// <typeparam name="TId">
///     <para xml:lang="en">The type of the aggregate root's ID.</para>
///     <para xml:lang="ja">集約ルートのID型。</para>
/// </typeparam>
/// <param name="dbContext">
///     <para xml:lang="en">The <see cref="DbContext"/> shared with the Unit of Work in the same scope.</para>
///     <para xml:lang="ja">同じスコープのUnit of Workと共有する<see cref="DbContext"/>。</para>
/// </param>
public abstract class RepositoryBase<TContext, TAggregate, TId>(TContext dbContext) : IRepository<TAggregate, TId>
    where TContext : DbContext
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    ///     <para xml:lang="en">Gets the <see cref="DbContext"/>.</para>
    ///     <para xml:lang="ja"><see cref="DbContext"/>を取得する。</para>
    /// </summary>
    protected TContext DbContext { get; } = dbContext;

    /// <summary>
    ///     <para xml:lang="en">Gets the <see cref="DbSet{TEntity}"/> of the aggregate root.</para>
    ///     <para xml:lang="ja">集約ルートの<see cref="DbSet{TEntity}"/>を取得する。</para>
    /// </summary>
    protected DbSet<TAggregate> Set => DbContext.Set<TAggregate>();

    /// <inheritdoc />
    public virtual async Task<TAggregate?> FindAsync(TId id, CancellationToken cancellationToken) =>
        await Set.FindAsync([id], cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public virtual void Add(TAggregate aggregate) => Set.Add(aggregate);

    /// <inheritdoc />
    public virtual void Delete(DeletionToken<TAggregate> token)
    {
        ArgumentNullException.ThrowIfNull(token);
        Set.Remove(token.Entity);
    }

    /// <inheritdoc />
    public virtual Task<int> CountAsync(CancellationToken cancellationToken) => Set.CountAsync(cancellationToken);

    /// <inheritdoc />
    public virtual Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken) =>
        Set.AnyAsync(x => x.Id.Equals(id), cancellationToken);
}

/// <summary>
///     <para xml:lang="en">Base class for EF Core repositories of aggregate roots whose ID is a <see cref="UuidV7"/>. A shortcut for <see cref="RepositoryBase{TContext, TAggregate, TId}"/>.</para>
///     <para xml:lang="ja">IDが<see cref="UuidV7"/>の集約ルート用EF Coreリポジトリの基底クラス。<see cref="RepositoryBase{TContext, TAggregate, TId}"/>のショートカット。</para>
/// </summary>
/// <typeparam name="TContext">
///     <para xml:lang="en">The type of the <see cref="DbContext"/>.</para>
///     <para xml:lang="ja"><see cref="DbContext"/>の型。</para>
/// </typeparam>
/// <typeparam name="TAggregate">
///     <para xml:lang="en">The type of the aggregate root.</para>
///     <para xml:lang="ja">集約ルートの型。</para>
/// </typeparam>
/// <param name="dbContext">
///     <para xml:lang="en">The <see cref="DbContext"/> shared with the Unit of Work in the same scope.</para>
///     <para xml:lang="ja">同じスコープのUnit of Workと共有する<see cref="DbContext"/>。</para>
/// </param>
public abstract class RepositoryBase<TContext, TAggregate>(TContext dbContext)
    : RepositoryBase<TContext, TAggregate, UuidV7>(dbContext), IRepository<TAggregate>
    where TContext : DbContext
    where TAggregate : AggregateRoot<UuidV7>
{
}
