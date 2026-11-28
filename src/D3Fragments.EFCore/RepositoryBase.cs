using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Base class for EF Core implementations of <see cref="IRepository{TAggregate, TId}"/>. Methods only change the tracked state of <typeparamref name="TContext"/>; saving is done by the Unit of Work. The only exception is <see cref="ExecuteImmediateAsync{T}"/>, which saves pending changes without committing them.</para>
///     <para xml:lang="ja"><see cref="IRepository{TAggregate, TId}"/>のEF Core実装の基底クラス。各メソッドは<typeparamref name="TContext"/>の追跡状態を変更するのみで、保存はUnit of Workが行う。例外は<see cref="ExecuteImmediateAsync{T}"/>のみで、未保存の変更をCommitせずに保存する。</para>
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

    /// <summary>
    ///     <para xml:lang="en">Executes an operation that runs against the database immediately, such as <c>ExecuteUpdateAsync</c>, <c>ExecuteDeleteAsync</c>, or <c>Database.ExecuteSqlAsync</c>. Pending tracked changes are saved before the operation, without committing the transaction.</para>
    ///     <para xml:lang="ja"><c>ExecuteUpdateAsync</c>・<c>ExecuteDeleteAsync</c>・<c>Database.ExecuteSqlAsync</c>など、データベースに即時に実行される操作を実行する。操作の前に、追跡中の未保存の変更を保存する（トランザクションはCommitしない）。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">The pending changes are saved in the same way as the Unit of Work saves them: <c>UpdatedAt</c> and <c>Version</c> are updated, and optimistic concurrency conflicts are converted to <see cref="ConflictOccurredException"/>. Before-commit domain event handlers are not called by this save; they are called when the Unit of Work saves at the end of the command. If the command fails, the saved changes are rolled back with the transaction.</para>
    ///     <para xml:lang="ja">未保存の変更は、Unit of Workと同じく<c>UpdatedAt</c>・<c>Version</c>を更新して保存し、楽観ロックの競合は<see cref="ConflictOccurredException"/>に変換する。この保存ではCommit前のドメインイベントハンドラーは呼ばれず、Commandの終了時にUnit of Workが保存するときに呼ばれる。Commandが失敗した場合、保存した変更はトランザクションとともにロールバックされる。</para>
    ///     <para xml:lang="en">The operation is not reflected in tracked entities. An <c>ExecuteUpdate</c> on an aggregate table does not update <c>UpdatedAt</c> or <c>Version</c>; set them in the operation if needed.</para>
    ///     <para xml:lang="ja">操作の結果は、追跡中のエンティティには反映されない。集約のテーブルに対する<c>ExecuteUpdate</c>では<c>UpdatedAt</c>・<c>Version</c>は更新されないため、必要であれば操作の中で設定すること。</para>
    /// </remarks>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the result of the operation.</para>
    ///     <para xml:lang="ja">操作の結果の型。</para>
    /// </typeparam>
    /// <param name="operation">
    ///     <para xml:lang="en">The operation to execute. It receives <paramref name="cancellationToken"/>.</para>
    ///     <para xml:lang="ja">実行する操作。<paramref name="cancellationToken"/>を受け取る。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">A token to cancel the operation.</para>
    ///     <para xml:lang="ja">操作をキャンセルするためのトークン。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The result of <paramref name="operation"/>.</para>
    ///     <para xml:lang="ja"><paramref name="operation"/>の結果。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="operation"/> is null.</para>
    ///     <para xml:lang="ja"><paramref name="operation"/>がnullの場合にスローされる。</para>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     <para xml:lang="en">Thrown when no transaction has been started on <see cref="DbContext"/>. Make the command implement <c>ITransactionalCommand</c>.</para>
    ///     <para xml:lang="ja"><see cref="DbContext"/>でトランザクションが開始されていない場合にスローされる。Commandに<c>ITransactionalCommand</c>を実装すること。</para>
    /// </exception>
    /// <exception cref="ConflictOccurredException">
    ///     <para xml:lang="en">Thrown when saving the pending changes causes an optimistic concurrency conflict.</para>
    ///     <para xml:lang="ja">未保存の変更の保存で楽観ロックの競合が発生した場合にスローされる。</para>
    /// </exception>
    protected async Task<T> ExecuteImmediateAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // TransactionalTestBase はトランザクションを DbContext で直接開始するため、EfUnitOfWork ではなく DbContext で判定する
        if (DbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(ExceptionMessages.InvalidOperationException_ImmediateOperationOutsideTransaction);
        }

        await EfUnitOfWork<TContext>.SaveChangesConvertingConflictAsync(DbContext, cancellationToken).ConfigureAwait(false);
        return await operation(cancellationToken).ConfigureAwait(false);
    }
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
