using DotNet.Testcontainers.Containers;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace D3Fragments.Testing.EFCore;

/// <summary>
///     <para xml:lang="en">Represents a base class for fixtures that start a database container with Testcontainers and create the schema.</para>
///     <para xml:lang="ja">Testcontainersでデータベースコンテナを起動し、スキーマを作成するフィクスチャの基底クラス。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Register the derived class as an assembly fixture with <c>[assembly: AssemblyFixture(typeof(...))]</c> so that all test classes share one container. The container is disposed when the test assembly finishes.</para>
///     <para xml:lang="ja">派生クラスを<c>[assembly: AssemblyFixture(typeof(...))]</c>でアセンブリフィクスチャとして登録すると、すべてのテストクラスが1つのコンテナを共有する。コンテナはテストアセンブリの終了時に破棄される。</para>
/// </remarks>
/// <typeparam name="TDbContext">
///     <para xml:lang="en">The type of the DbContext.</para>
///     <para xml:lang="ja">DbContextの型。</para>
/// </typeparam>
public abstract class DatabaseContainerFixture<TDbContext> : IAsyncLifetime
    where TDbContext : DbContext
{
    private readonly Lazy<IDatabaseContainer> _container;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="DatabaseContainerFixture{TDbContext}"/> class.</para>
    ///     <para xml:lang="ja"><see cref="DatabaseContainerFixture{TDbContext}"/>クラスの新しいインスタンスを初期化する。</para>
    /// </summary>
    protected DatabaseContainerFixture()
    {
        _container = new Lazy<IDatabaseContainer>(CreateContainer);
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the connection string of the started container.</para>
    ///     <para xml:lang="ja">起動したコンテナへの接続文字列を取得する。</para>
    /// </summary>
    public string ConnectionString => _container.Value.GetConnectionString();

    /// <summary>
    ///     <para xml:lang="en">Creates the database container. For example, return <c>new PostgreSqlBuilder("postgres:18-alpine").Build()</c>.</para>
    ///     <para xml:lang="ja">データベースコンテナを生成する。例えば<c>new PostgreSqlBuilder("postgres:18-alpine").Build()</c>を返す。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">The database container that has not been started.</para>
    ///     <para xml:lang="ja">未起動のデータベースコンテナ。</para>
    /// </returns>
    protected abstract IDatabaseContainer CreateContainer();

    /// <summary>
    ///     <para xml:lang="en">Creates a new <typeparamref name="TDbContext"/> instance connected to <see cref="ConnectionString"/>.</para>
    ///     <para xml:lang="ja"><see cref="ConnectionString"/>に接続する新しい<typeparamref name="TDbContext"/>インスタンスを生成する。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A new <typeparamref name="TDbContext"/> instance. The caller disposes it.</para>
    ///     <para xml:lang="ja">新しい<typeparamref name="TDbContext"/>インスタンス。破棄は呼び出し側が行う。</para>
    /// </returns>
    public abstract TDbContext CreateDbContext();

    /// <summary>
    ///     <para xml:lang="en">Creates the schema after the container starts. The default implementation calls <see cref="Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator.EnsureCreatedAsync(CancellationToken)"/>. Override this method to apply migrations instead.</para>
    ///     <para xml:lang="ja">コンテナの起動後にスキーマを作成する。既定の実装は<see cref="Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator.EnsureCreatedAsync(CancellationToken)"/>を呼び出す。マイグレーションを適用する場合はオーバーライドする。</para>
    /// </summary>
    /// <param name="dbContext">
    ///     <para xml:lang="en">The DbContext connected to the started container.</para>
    ///     <para xml:lang="ja">起動したコンテナに接続したDbContext。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation.</para>
    ///     <para xml:lang="ja">非同期操作を表すタスク。</para>
    /// </returns>
    protected virtual Task InitializeDatabaseAsync(TDbContext dbContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        return dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await _container.Value.StartAsync(cancellationToken).ConfigureAwait(false);

        var dbContext = CreateDbContext();
        await using (dbContext.ConfigureAwait(false))
        {
            await InitializeDatabaseAsync(dbContext, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_container.IsValueCreated)
        {
            await _container.Value.DisposeAsync().ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
    }
}
