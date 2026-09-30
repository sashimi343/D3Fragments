using D3Fragments.Application;
using D3Fragments.EFCore;
using D3Fragments.Testing.EFCore.Tests;

using DotNet.Testcontainers.Containers;

using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(PostgreSqlFixture))]

namespace D3Fragments.Testing.EFCore.Tests;

public sealed record OrderPlaced(UuidV7 OrderId, DateTimeOffset OccurredAt) : IDomainEvent;

public sealed class Order : AggregateRoot
{
    private Order(string name, DateTimeOffset createdAt) : base(createdAt)
    {
        Name = name;
    }

    public string Name { get; private set; }

    public static Order Place(string name)
    {
        var order = new Order(name, TestTime.Now);
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

    private sealed class SeedOnlyTest(PostgreSqlFixture fixture)
        : TransactionalTestBase<TestDbContext, PostgreSqlFixture>(fixture)
    {
        public Task SeedOrderAsync(string name) => SeedAsync(Order.Place(name));
    }
}
