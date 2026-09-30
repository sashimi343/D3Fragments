using System.Linq.Expressions;

using D3Fragments.Application;
using D3Fragments.EFCore;
using D3Fragments.EFCore.DomainEvents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Xunit;

namespace D3Fragments.Testing.EFCore;

/// <summary>
///     <para xml:lang="en">Represents a base class for tests that run each test method in a database transaction and roll it back when the test finishes.</para>
///     <para xml:lang="ja">テストメソッドごとにデータベーストランザクションを開始し、テスト終了時にロールバックするテストの基底クラス。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Create repositories with <see cref="DbContext"/> so that the handler under test and the assertions share the same transaction. Data written in the transaction is not visible to other DbContext instances.</para>
///     <para xml:lang="ja">リポジトリは<see cref="DbContext"/>を渡して生成し、テスト対象のHandlerと検証が同じトランザクションを共有するようにする。トランザクション内のデータは他のDbContextインスタンスからは参照できない。</para>
/// </remarks>
/// <typeparam name="TDbContext">
///     <para xml:lang="en">The type of the DbContext.</para>
///     <para xml:lang="ja">DbContextの型。</para>
/// </typeparam>
/// <typeparam name="TFixture">
///     <para xml:lang="en">The type of the fixture that provides the database.</para>
///     <para xml:lang="ja">データベースを提供するフィクスチャの型。</para>
/// </typeparam>
public abstract class TransactionalTestBase<TDbContext, TFixture> : IAsyncLifetime
    where TDbContext : DbContext
    where TFixture : DatabaseContainerFixture<TDbContext>
{
    private readonly SpyDomainEventDispatcher _dispatcher = new();
    private IDbContextTransaction? _transaction;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="TransactionalTestBase{TDbContext, TFixture}"/> class.</para>
    ///     <para xml:lang="ja"><see cref="TransactionalTestBase{TDbContext, TFixture}"/>クラスの新しいインスタンスを初期化する。</para>
    /// </summary>
    /// <param name="fixture">
    ///     <para xml:lang="en">The fixture that provides the database.</para>
    ///     <para xml:lang="ja">データベースを提供するフィクスチャ。</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="fixture"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="fixture"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    protected TransactionalTestBase(TFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        Fixture = fixture;
        DbContext = fixture.CreateDbContext();
        UnitOfWork = new EfUnitOfWork<TDbContext>(DbContext, new ImmediateDispatchStrategy(_dispatcher));
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the fixture that provides the database.</para>
    ///     <para xml:lang="ja">データベースを提供するフィクスチャを取得する。</para>
    /// </summary>
    protected TFixture Fixture { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the DbContext used in the test. It is disposed when the test finishes.</para>
    ///     <para xml:lang="ja">テストで使用するDbContextを取得する。テスト終了時に破棄される。</para>
    /// </summary>
    protected TDbContext DbContext { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the Unit of Work that saves changes of <see cref="DbContext"/> and records dispatched domain events in <see cref="DispatchedEvents"/>.</para>
    ///     <para xml:lang="ja"><see cref="DbContext"/>の変更を保存し、発火したドメインイベントを<see cref="DispatchedEvents"/>に記録するUnit of Workを取得する。</para>
    /// </summary>
    protected IUnitOfWork UnitOfWork { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the domain events dispatched by <see cref="UnitOfWork"/> in dispatch order.</para>
    ///     <para xml:lang="ja"><see cref="UnitOfWork"/>が発火したドメインイベントを発火順に取得する。</para>
    /// </summary>
    protected IReadOnlyList<IDomainEvent> DispatchedEvents => _dispatcher.DispatchedEvents;

    /// <inheritdoc/>
    public virtual async ValueTask InitializeAsync()
    {
        _transaction = await DbContext.Database.BeginTransactionAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public virtual async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            await _transaction.DisposeAsync().ConfigureAwait(false);
            _transaction = null;
        }

        await DbContext.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     <para xml:lang="en">Saves the entities as existing data. Domain events held by the entities are cleared without being dispatched.</para>
    ///     <para xml:lang="ja">エンティティを既存データとして保存する。エンティティが保持するドメインイベントは発火せずに破棄する。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Entities of different types can be passed together. Related entities reachable through navigations are also saved. The change tracker is cleared after saving, so later queries read from the database.</para>
    ///     <para xml:lang="ja">型の異なるエンティティをまとめて渡せる。ナビゲーションで到達できる関連エンティティも保存される。保存後にChangeTrackerをクリアするため、以降のクエリはデータベースから読み込まれる。</para>
    /// </remarks>
    /// <param name="entities">
    ///     <para xml:lang="en">The entities to save.</para>
    ///     <para xml:lang="ja">保存するエンティティ。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation.</para>
    ///     <para xml:lang="ja">非同期操作を表すタスク。</para>
    /// </returns>
    protected async Task SeedAsync(params IEnumerable<object> entities)
    {
        var list = entities.ToList();
        DbContext.AddRange(list);
        await DbContext.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);

        foreach (var source in list.OfType<IHasDomainEvents>())
        {
            source.ClearDomainEvents();
        }

        DbContext.ChangeTracker.Clear();
    }

    /// <summary>
    ///     <para xml:lang="en">Executes the handler, saves the changes with <see cref="UnitOfWork"/> when the request is a command, and clears the change tracker.</para>
    ///     <para xml:lang="ja">Handlerを実行し、リクエストがコマンドの場合は<see cref="UnitOfWork"/>で変更を保存したうえで、ChangeTrackerをクリアする。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">This method reproduces <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/> without other pipeline behaviors. <see cref="TransactionBehavior{TRequest, TResponse}"/> is not applied because the test already runs in a transaction. Domain events are dispatched when saving, and they can be verified with <see cref="DispatchedEvents"/>.</para>
    ///     <para xml:lang="ja">このメソッドは他のPipeline Behaviorを適用せずに<see cref="UnitOfWorkBehavior{TRequest, TResponse}"/>の処理を再現する。テストはすでにトランザクション内で実行されるため、<see cref="TransactionBehavior{TRequest, TResponse}"/>は適用しない。ドメインイベントは保存時に発火し、<see cref="DispatchedEvents"/>で検証できる。</para>
    /// </remarks>
    /// <typeparam name="TRequest">
    ///     <para xml:lang="en">The type of the request.</para>
    ///     <para xml:lang="ja">リクエストの型。</para>
    /// </typeparam>
    /// <typeparam name="TResponse">
    ///     <para xml:lang="en">The type of the response.</para>
    ///     <para xml:lang="ja">レスポンスの型。</para>
    /// </typeparam>
    /// <param name="handler">
    ///     <para xml:lang="en">The handler under test.</para>
    ///     <para xml:lang="ja">テスト対象のHandler。</para>
    /// </param>
    /// <param name="request">
    ///     <para xml:lang="en">The request to handle.</para>
    ///     <para xml:lang="ja">処理するリクエスト。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The response returned by the handler.</para>
    ///     <para xml:lang="ja">Handlerが返したレスポンス。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="handler"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="handler"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    protected async Task<TResponse> ExecuteAsync<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler, TRequest request)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(handler);
        var cancellationToken = TestContext.Current.CancellationToken;

        var response = await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);
        if (request is ICommand)
        {
            await UnitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        DbContext.ChangeTracker.Clear();
        return response;
    }

    /// <summary>
    ///     <para xml:lang="en">Verifies that the database contains an entity that matches the predicate.</para>
    ///     <para xml:lang="ja">条件に一致するエンティティがデータベースに存在することを検証する。</para>
    /// </summary>
    /// <typeparam name="TEntity">
    ///     <para xml:lang="en">The type of the entity.</para>
    ///     <para xml:lang="ja">エンティティの型。</para>
    /// </typeparam>
    /// <param name="predicate">
    ///     <para xml:lang="en">The condition translated into SQL.</para>
    ///     <para xml:lang="ja">SQLに変換される検索条件。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation.</para>
    ///     <para xml:lang="ja">非同期操作を表すタスク。</para>
    /// </returns>
    protected async Task AssertEntityExistsAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
    {
        Assert.True(await DbContext.Set<TEntity>().AnyAsync(predicate, TestContext.Current.CancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    ///     <para xml:lang="en">Verifies that the database contains no entity that matches the predicate.</para>
    ///     <para xml:lang="ja">条件に一致するエンティティがデータベースに存在しないことを検証する。</para>
    /// </summary>
    /// <typeparam name="TEntity">
    ///     <para xml:lang="en">The type of the entity.</para>
    ///     <para xml:lang="ja">エンティティの型。</para>
    /// </typeparam>
    /// <param name="predicate">
    ///     <para xml:lang="en">The condition translated into SQL.</para>
    ///     <para xml:lang="ja">SQLに変換される検索条件。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation.</para>
    ///     <para xml:lang="ja">非同期操作を表すタスク。</para>
    /// </returns>
    protected async Task AssertEntityNotExistsAsync<TEntity>(Expression<Func<TEntity, bool>> predicate)
        where TEntity : class
    {
        Assert.False(await DbContext.Set<TEntity>().AnyAsync(predicate, TestContext.Current.CancellationToken).ConfigureAwait(false));
    }
}
