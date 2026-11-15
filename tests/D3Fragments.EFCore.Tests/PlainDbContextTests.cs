using D3Fragments.Application;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.EFCore.Tests;

// D3DbContextを継承せず、規約の拡張メソッドとUseD3Fragmentsのインターセプターで同じ動作を得るDbContext
internal sealed class PlainOrderDbContext(DbContextOptions<PlainOrderDbContext> options) : DbContext(options)
{
    public DbSet<PurchaseOrder> Orders => Set<PurchaseOrder>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Member> Members => Set<Member>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ApplyD3FragmentsConventions(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PurchaseOrder>().HasMany(x => x.Lines).WithOne().HasForeignKey("OrderId").IsRequired();
        modelBuilder.Entity<OrderLine>().HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId);
        modelBuilder.Entity<Member>(builder =>
        {
            builder.ComplexValueObject(x => x.Email);
            builder.ComplexValueObject(x => x.SubEmail);
        });
    }
}

// 規約を適用し忘れたDbContext
internal sealed class NoConventionsDbContext(DbContextOptions<NoConventionsDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}

internal sealed class NullOptionsDbContext(DbContextOptions<NullOptionsDbContext> options) : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.ApplyD3FragmentsConventions(null!);
}

// DI登録の確認用。TypeSafeEnumを含まないため、走査対象にテストアセンブリを渡さなくてよい
internal sealed class PlainProductDbContext(DbContextOptions<PlainProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ApplyD3FragmentsConventions(options);
    }
}

public sealed class PlainDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public PlainDbContextTests()
    {
        _connection.Open();
        using var dbContext = CreateContext();
        dbContext.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private PlainOrderDbContext CreateContext(Func<DbContextOptionsBuilder<PlainOrderDbContext>, DbContextOptionsBuilder<PlainOrderDbContext>>? configure = null)
    {
        var builder = new DbContextOptionsBuilder<PlainOrderDbContext>()
            .UseSqlite(_connection)
            .UseD3Fragments([typeof(TestColor).Assembly]);
        return new(configure?.Invoke(builder).Options ?? builder.Options);
    }

    private UuidV7 SeedOrder()
    {
        using var dbContext = CreateContext();
        var product = new Product();
        var order = new PurchaseOrder();
        order.AddLine(product.Id, 1);
        dbContext.AddRange(product, order);
        dbContext.SaveChanges();
        return order.Id;
    }

    [Fact]
    public void 規約を適用するとUuidV7とTypeSafeEnumが変換され_Versionが同時実行トークンになる()
    {
        var referrerId = UuidV7.NewUuidV7();
        var member = new Member(Email.Create("a@example.com"), null, TestColor.Red, referrerId);
        using (var dbContext = CreateContext())
        {
            dbContext.Members.Add(member);
            dbContext.SaveChanges();
        }

        using var readContext = CreateContext();
        var loaded = readContext.Members.Single();
        Assert.Equal(referrerId, loaded.ReferrerId);
        Assert.Same(TestColor.Red, loaded.Color);
        Assert.True(readContext.Model.FindEntityType(typeof(PurchaseOrder))!.FindProperty(nameof(IUpdateTrackable.Version))!.IsConcurrencyToken);
    }

    [Fact]
    public void 子エンティティを変更すると集約ルートのVersionが1つ増える()
    {
        var orderId = SeedOrder();
        using var dbContext = CreateContext();
        var order = dbContext.Orders.Include(x => x.Lines).Single(x => x.Id == orderId);

        order.Lines[0].ChangeQuantity(5);
        dbContext.SaveChanges();

        Assert.Equal(1, order.Version);
    }

    [Fact]
    public async Task 非同期で保存しても集約ルートのVersionが1つ増える()
    {
        var orderId = SeedOrder();
        await using var dbContext = CreateContext();
        var order = await dbContext.Orders.SingleAsync(x => x.Id == orderId, TestContext.Current.CancellationToken);

        order.ChangeNote("note");
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, order.Version);
    }

    [Fact]
    public void UseD3Fragmentsを複数回呼んでもVersionは1つだけ増える()
    {
        var orderId = SeedOrder();
        using var dbContext = CreateContext(builder => builder.UseD3Fragments([typeof(TestColor).Assembly]));
        var order = dbContext.Orders.Single(x => x.Id == orderId);

        order.ChangeNote("note");
        dbContext.SaveChanges();

        Assert.Equal(1, order.Version);
    }

    [Fact]
    public void 同じ集約を別々に変更すると後から保存した方が競合する()
    {
        var orderId = SeedOrder();
        using var first = CreateContext();
        using var second = CreateContext();
        var firstOrder = first.Orders.Single(x => x.Id == orderId);
        var secondOrder = second.Orders.Single(x => x.Id == orderId);

        firstOrder.ChangeNote("first");
        first.SaveChanges();
        secondOrder.ChangeNote("second");

        Assert.Throws<DbUpdateConcurrencyException>(() => second.SaveChanges());
    }

    [Fact]
    public void D3DbContextにUseD3Fragmentsを適用してもVersionは二重に増えない()
    {
        using var database = new SqliteTestDatabase();
        var member = new Member(Email.Create("a@example.com"), null, TestColor.Red, null);
        using (var dbContext = database.CreateContext())
        {
            dbContext.Members.Add(member);
            dbContext.SaveChanges();
        }

        using var updateContext = database.CreateContext();
        var loaded = updateContext.Members.Single();
        loaded.ChangeEmail(Email.Create("b@example.com"));
        updateContext.SaveChanges();

        Assert.Equal(1, loaded.Version);
    }

    [Fact]
    public async Task 規約を適用せずにUseD3Fragmentsで構成したDbContextは保存時に例外を送出する()
    {
        using var dbContext = new NoConventionsDbContext(new DbContextOptionsBuilder<NoConventionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseD3Fragments([typeof(TestColor).Assembly])
            .Options);
        dbContext.Products.Add(new Product());

        var exception = Assert.Throws<InvalidOperationException>(() => dbContext.SaveChanges());
        await Assert.ThrowsAsync<InvalidOperationException>(() => dbContext.SaveChangesAsync(TestContext.Current.CancellationToken));

        Assert.Contains(typeof(NoConventionsDbContext).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyD3FragmentsConventions_引数がnullなら例外を送出する()
    {
        var options = new DbContextOptionsBuilder().Options;

        Assert.Throws<ArgumentNullException>(() => ((ModelConfigurationBuilder)null!).ApplyD3FragmentsConventions(options));

        // ModelConfigurationBuilderはモデル構築時にEF Coreが生成するため、ConfigureConventions経由で確かめる
        using var dbContext = new NullOptionsDbContext(new DbContextOptionsBuilder<NullOptionsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var exception = Assert.Throws<ArgumentNullException>(() => dbContext.Model);
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public async Task AddD3FragmentsEFCoreでD3DbContextを継承しないDbContextを登録できる()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddD3FragmentsApplication([typeof(D3DbContext).Assembly]);
        services.AddD3FragmentsEFCore<PlainProductDbContext>([typeof(D3DbContext).Assembly], db => db.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        await using var provider = services.BuildServiceProvider(validateScopes: true);
        await using var scope = provider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<PlainProductDbContext>();
        var product = new Product();
        dbContext.Products.Add(product);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(TestContext.Current.CancellationToken);
        product.ChangeStock(3);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, product.Version);
    }
}
