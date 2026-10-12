using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore.Tests;

internal sealed class Product : AggregateRoot
{
    public int Stock { get; private set; }

    public void ChangeStock(int stock) => Stock = stock;
}

internal sealed class OrderLine : Entity
{
    public OrderLine(UuidV7 productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    // 他の集約（Product）はIDのみで参照する
    public UuidV7 ProductId { get; private set; }

    public int Quantity { get; private set; }

    public void ChangeQuantity(int quantity) => Quantity = quantity;
}

internal sealed class PurchaseOrder : AggregateRoot
{
    private readonly List<OrderLine> _lines = [];

    public IReadOnlyList<OrderLine> Lines => _lines;

    public string Note { get; private set; } = string.Empty;

    public OrderLine AddLine(UuidV7 productId, int quantity)
    {
        var line = new OrderLine(productId, quantity);
        _lines.Add(line);
        return line;
    }

    public void RemoveLine(OrderLine line) => _lines.Remove(line);

    public void ChangeNote(string note) => Note = note;
}

internal sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : D3DbContext(options)
{
    public DbSet<PurchaseOrder> Orders => Set<PurchaseOrder>();

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>().HasMany(x => x.Lines).WithOne().HasForeignKey("OrderId").IsRequired();
        modelBuilder.Entity<OrderLine>().HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId);
    }
}

public sealed class AggregateConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly UuidV7 _productId;

    public AggregateConcurrencyTests()
    {
        _connection.Open();
        using var dbContext = CreateContext();
        dbContext.Database.EnsureCreated();
        var product = new Product();
        var order = new PurchaseOrder();
        order.AddLine(product.Id, 1);
        dbContext.AddRange(product, order);
        dbContext.SaveChanges();
        _productId = product.Id;
    }

    public void Dispose() => _connection.Dispose();

    private OrderDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<OrderDbContext>().UseSqlite(_connection).Options);

    private static Task<PurchaseOrder> LoadOrderAsync(OrderDbContext dbContext) =>
        dbContext.Orders.Include(x => x.Lines).SingleAsync(TestContext.Current.CancellationToken);

    private async Task<(long OrderVersion, long ProductVersion)> SavedVersionsAsync()
    {
        await using var dbContext = CreateContext();
        var order = await dbContext.Orders.SingleAsync(TestContext.Current.CancellationToken);
        var product = await dbContext.Products.SingleAsync(TestContext.Current.CancellationToken);
        return (order.Version, product.Version);
    }

    [Fact]
    public async Task 子エンティティの変更_集約ルートのVersionとUpdatedAtが更新される()
    {
        await using var dbContext = CreateContext();
        var order = await LoadOrderAsync(dbContext);
        var updatedAt = order.UpdatedAt;

        order.Lines[0].ChangeQuantity(5);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, order.Version);
        Assert.True(order.UpdatedAt >= updatedAt);
        Assert.Equal(1, order.Lines[0].Version);
        Assert.Equal((1, 0), await SavedVersionsAsync());
    }

    [Fact]
    public async Task 子エンティティの追加_集約ルートのVersionが更新される()
    {
        await using var dbContext = CreateContext();
        var order = await LoadOrderAsync(dbContext);

        order.AddLine(_productId, 2);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal((1, 0), await SavedVersionsAsync());
    }

    [Fact]
    public async Task 子エンティティの削除_集約ルートのVersionが更新される()
    {
        await using var dbContext = CreateContext();
        var order = await LoadOrderAsync(dbContext);

        order.RemoveLine(order.Lines[0]);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal((1, 0), await SavedVersionsAsync());
    }

    [Fact]
    public async Task 集約ルートと子エンティティの両方の変更_Versionは1回だけ加算される()
    {
        await using var dbContext = CreateContext();
        var order = await LoadOrderAsync(dbContext);

        order.ChangeNote("changed");
        order.Lines[0].ChangeQuantity(5);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal((1, 0), await SavedVersionsAsync());
    }

    [Fact]
    public async Task 別々の子エンティティを同時に追加_後から保存した方が競合する()
    {
        await using var first = CreateContext();
        await using var second = CreateContext();
        var firstOrder = await LoadOrderAsync(first);
        var secondOrder = await LoadOrderAsync(second);

        firstOrder.AddLine(_productId, 2);
        await first.SaveChangesAsync(TestContext.Current.CancellationToken);
        secondOrder.AddLine(_productId, 3);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task 集約ルートが追跡されていない場合_子エンティティのみ保存される()
    {
        await using var dbContext = CreateContext();
        var line = await dbContext.Set<OrderLine>().SingleAsync(TestContext.Current.CancellationToken);

        line.ChangeQuantity(5);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, line.Version);
        Assert.Equal((0, 0), await SavedVersionsAsync());
    }

    // 子エンティティが他の集約をIDで参照する外部キー（親側にナビゲーションがない）はたどらない
    [Fact]
    public async Task 他の集約をIDで参照する外部キー_参照先の集約は更新されない()
    {
        await using var dbContext = CreateContext();
        var order = await LoadOrderAsync(dbContext);
        await dbContext.Products.SingleAsync(TestContext.Current.CancellationToken);

        order.Lines[0].ChangeQuantity(5);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal((1, 0), await SavedVersionsAsync());
    }
}
