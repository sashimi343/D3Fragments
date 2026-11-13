using System.Reflection;

using DotNet.Testcontainers.Containers;

using Microsoft.EntityFrameworkCore;

namespace D3Fragments.Testing.EFCore.Tests;

// コンテナを起動せずに、フィクスチャがコンテナと DbContext をどう扱うかを検証する
public class DatabaseContainerFixtureTests
{
    // 呼び出されたメソッド名を記録するだけの偽のコンテナ
    public class FakeContainer : DispatchProxy
    {
        public List<string> Calls { get; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls.Add(targetMethod!.Name);
            var returnType = targetMethod.ReturnType;
            if (returnType == typeof(Task))
            {
                return Task.CompletedTask;
            }
            if (returnType == typeof(string))
            {
                return "Host=fake";
            }
            return returnType.IsValueType && returnType != typeof(void) ? Activator.CreateInstance(returnType) : null;
        }
    }

    private sealed class FakeFixture : DatabaseContainerFixture<TestDbContext>
    {
        public int CreateContainerCount { get; private set; }

        public FakeContainer? Container { get; private set; }

        public TestDbContext? InitializedWith { get; private set; }

        protected override IDatabaseContainer CreateContainer()
        {
            CreateContainerCount++;
            var proxy = DispatchProxy.Create<IDatabaseContainer, FakeContainer>();
            Container = (FakeContainer)(object)proxy;
            return proxy;
        }

        public override TestDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(ConnectionString).Options);

        protected override Task InitializeDatabaseAsync(TestDbContext dbContext, CancellationToken cancellationToken)
        {
            InitializedWith = dbContext;
            return Task.CompletedTask;
        }

        public Task InitializeDatabaseByDefaultAsync(TestDbContext dbContext) =>
            base.InitializeDatabaseAsync(dbContext, CancellationToken.None);
    }

    [Fact]
    public async Task InitializeAsync_コンテナを起動してDBを初期化し使ったDbContextを破棄する()
    {
        await using var fixture = new FakeFixture();

        await fixture.InitializeAsync();

        Assert.Equal(1, fixture.CreateContainerCount);
        Assert.Contains(nameof(IContainer.StartAsync), fixture.Container!.Calls);
        Assert.NotNull(fixture.InitializedWith);
        Assert.Throws<ObjectDisposedException>(() => fixture.InitializedWith.Model);
    }

    [Fact]
    public async Task DisposeAsync_作成したコンテナを破棄する()
    {
        var fixture = new FakeFixture();
        await fixture.InitializeAsync();

        await fixture.DisposeAsync();

        Assert.Contains(nameof(IAsyncDisposable.DisposeAsync), fixture.Container!.Calls);
    }

    [Fact]
    public async Task DisposeAsync_初期化していなければコンテナを作成しない()
    {
        var fixture = new FakeFixture();

        await fixture.DisposeAsync();

        Assert.Equal(0, fixture.CreateContainerCount);
    }

    [Fact]
    public async Task InitializeDatabaseAsync_nullのDbContextは例外になる()
    {
        await using var fixture = new FakeFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(() => fixture.InitializeDatabaseByDefaultAsync(null!));
    }
}
