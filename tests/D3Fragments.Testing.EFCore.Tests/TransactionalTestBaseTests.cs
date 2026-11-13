using D3Fragments.Application;
using D3Fragments.EFCore;
using D3Fragments.Testing.EFCore.Tests;

using DotNet.Testcontainers.Containers;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

using Xunit.Sdk;

[assembly: AssemblyFixture(typeof(PostgreSqlFixture))]

namespace D3Fragments.Testing.EFCore.Tests;

public sealed record OrderPlaced(UuidV7 OrderId, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed class Order : AggregateRoot
{
    private Order(string name)
    {
        Name = name;
    }

    public string Name { get; private set; }

    public static Order Place(string name)
    {
        var order = new Order(name);
        order.AddDomainEvent(new OrderPlaced(order.Id, TestTime.Now));
        return order;
    }
}

public sealed class TestDbContext(DbContextOptions<TestDbContext> options) : D3DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}

public sealed class OrderRepository(TestDbContext dbContext) : RepositoryBase<TestDbContext, Order>(dbContext);

public sealed record PlaceOrderCommand(string Name) : ICommand<UuidV7>;

public sealed class PlaceOrderHandler(OrderRepository repository) : IRequestHandler<PlaceOrderCommand, UuidV7>
{
    public Task<UuidV7> HandleAsync(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var order = Order.Place(request.Name);
        repository.Add(order);
        return Task.FromResult(order.Id);
    }
}

// Queryで保存されないことを確認するため、あえて集約を追加するQuery
public sealed record AddingQuery(string Name) : IQuery<UuidV7>;

public sealed class AddingQueryHandler(OrderRepository repository) : IRequestHandler<AddingQuery, UuidV7>
{
    public Task<UuidV7> HandleAsync(AddingQuery request, CancellationToken cancellationToken)
    {
        var order = Order.Place(request.Name);
        repository.Add(order);
        return Task.FromResult(order.Id);
    }
}

public sealed class PostgreSqlFixture : DatabaseContainerFixture<TestDbContext>
{
    protected override IDatabaseContainer CreateContainer() => new PostgreSqlBuilder("postgres:18-alpine").Build();

    public override TestDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<TestDbContext>().UseNpgsql(ConnectionString).Options);
}

public sealed class TransactionalTestBaseTests(PostgreSqlFixture fixture)
    : TransactionalTestBase<TestDbContext, PostgreSqlFixture>(fixture)
{
    [Fact]
    public async Task SeedAsync_保存したデータを検証でき_ドメインイベントは発火しない()
    {
        var order = Order.Place(TestData.Unique("seed"));

        await SeedAsync(order);

        await AssertEntityExistsAsync<Order>(x => x.Id == order.Id);
        DomainEventAssert.NotRaised<OrderPlaced>(order);
        Assert.Empty(DispatchedEvents);
    }

    [Fact]
    public async Task ExecuteAsync_コマンドの変更を保存し発火したイベントを記録する()
    {
        var name = TestData.Unique("command");

        var id = await ExecuteAsync(new PlaceOrderHandler(new OrderRepository(DbContext)), new PlaceOrderCommand(name));

        await AssertEntityExistsAsync<Order>(x => x.Id == id && x.Name == name);
        Assert.Equal(id, DomainEventAssert.Raised<OrderPlaced>(DispatchedEvents).OrderId);
    }

    [Fact]
    public async Task ExecuteAsync_クエリでは変更を保存しない()
    {
        var id = await ExecuteAsync(new AddingQueryHandler(new OrderRepository(DbContext)), new AddingQuery(TestData.Unique("query")));

        await AssertEntityNotExistsAsync<Order>(x => x.Id == id);
        Assert.Empty(DispatchedEvents);
    }

    [Fact]
    public async Task テスト終了時に変更がロールバックされる()
    {
        var name = TestData.Unique("rollback");
        var other = new SeedOnlyTest(Fixture);
        await other.InitializeAsync();
        await other.SeedOrderAsync(name);
        await other.DisposeAsync();

        await AssertEntityNotExistsAsync<Order>(x => x.Name == name);
    }

    [Fact]
    public async Task DisposeAsync_DbContextを破棄し2回呼んでも例外にならない()
    {
        var other = new SeedOnlyTest(Fixture);
        await other.InitializeAsync();

        await other.DisposeAsync();
        await other.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => other.Context.Model);
    }

    [Fact]
    public async Task AssertEntityExistsAsync_該当するデータがない場合は失敗する()
    {
        var name = TestData.Unique("missing");

        await Assert.ThrowsAnyAsync<XunitException>(() => AssertEntityExistsAsync<Order>(x => x.Name == name));
    }

    [Fact]
    public async Task AssertEntityNotExistsAsync_該当するデータがある場合は失敗する()
    {
        var order = Order.Place(TestData.Unique("exists"));
        await SeedAsync(order);

        await Assert.ThrowsAnyAsync<XunitException>(() => AssertEntityNotExistsAsync<Order>(x => x.Id == order.Id));
    }

    [Fact]
    public async Task SeedAsync_保存後に変更追跡をクリアする()
    {
        await SeedAsync(Order.Place(TestData.Unique("seed")));

        Assert.Empty(DbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ExecuteAsync_実行後に変更追跡をクリアする()
    {
        await ExecuteAsync(new PlaceOrderHandler(new OrderRepository(DbContext)), new PlaceOrderCommand(TestData.Unique("command")));
        Assert.Empty(DbContext.ChangeTracker.Entries());

        // クエリで追加された未保存の変更も破棄され、後続の保存に混ざらない
        await ExecuteAsync(new AddingQueryHandler(new OrderRepository(DbContext)), new AddingQuery(TestData.Unique("query")));
        Assert.Empty(DbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task 不正な引数は例外になる()
    {
        Assert.Throws<ArgumentNullException>(() => new SeedOnlyTest(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ExecuteAsync<PlaceOrderCommand, UuidV7>(null!, new PlaceOrderCommand("x")));
    }

    private sealed class SeedOnlyTest(PostgreSqlFixture fixture)
        : TransactionalTestBase<TestDbContext, PostgreSqlFixture>(fixture)
    {
        public TestDbContext Context => DbContext;

        public Task SeedOrderAsync(string name) => SeedAsync(Order.Place(name));
    }
}
