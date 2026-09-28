using D3Fragments.Application;
using D3Fragments.EFCore.DomainEvents;

using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore.Tests;

internal sealed record TestDomainEvent(DateTimeOffset OccurredAt) : IDomainEvent;

internal sealed class TestEntity : AggregateRoot
{
    public TestEntity(int value, DateTimeOffset createdAt) : base(createdAt)
    {
        Value = value;
    }

    public int Value { get; private set; }

    public void RaiseTestEvent(DateTimeOffset occurredAt) => AddDomainEvent(new TestDomainEvent(occurredAt));
}

internal sealed class SpyDomainEventDispatcher : IDomainEventDispatcher
{
    public List<IDomainEvent> DispatchedEvents { get; } = [];

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        DispatchedEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> Entities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>(builder =>
        {
            builder.Property(x => x.Id).HasConversion(x => x.Value, x => UuidV7.FromGuid(x));
        });
    }
}

internal static class TestDbContextFactory
{
    public static TestDbContext Create() =>
        new(new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}

public class PagingExtensionsTests
{
    [Fact]
    public async Task ToPagedListAsync_ページ番号が不正な場合は失敗結果を返す()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await dbContext.Entities.ToPagedListAsync(pageNo: 0, pageSize: 10, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ToPagedListAsync_ページサイズが上限を超える場合は失敗結果を返す()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await dbContext.Entities.ToPagedListAsync(pageNo: 1, pageSize: 10, CancellationToken.None, maxPageSize: 5);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ToPagedListAsync_データが0件の場合は空ページを返す()
    {
        await using var dbContext = TestDbContextFactory.Create();

        var result = await dbContext.Entities.ToPagedListAsync(pageNo: 1, pageSize: 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }

    [Fact]
    public async Task ToPagedListAsync_指定したページのデータを返す()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            dbContext.Entities.Add(new TestEntity(i, now));
        }
        await dbContext.SaveChangesAsync();

        var result = await dbContext.Entities.OrderBy(x => x.Value).ToPagedListAsync(pageNo: 2, pageSize: 2, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(5, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
    }
}

public sealed class TestColor : TypeSafeEnum<TestColor, int>
{
    public static readonly TestColor Red = new(1);

    private TestColor(int key) : base(key)
    {
    }
}

public class TypeSafeEnumConverterTests
{
    [Fact]
    public void キーと相互変換する()
    {
        var converter = new TypeSafeEnumConverter<TestColor, int>();

        Assert.Equal(1, converter.ConvertToProvider(TestColor.Red));
        Assert.Same(TestColor.Red, converter.ConvertFromProvider(1));
    }
}

public class EfUnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_保存成功後にドメインイベントがディスパッチされる()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var dispatcher = new SpyDomainEventDispatcher();
        var unitOfWork = new EfUnitOfWork<TestDbContext>(dbContext, new ImmediateDispatchStrategy(dispatcher));
        var entity = new TestEntity(1, DateTimeOffset.UtcNow);
        entity.RaiseTestEvent(DateTimeOffset.UtcNow);
        dbContext.Entities.Add(entity);

        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        Assert.Single(dispatcher.DispatchedEvents);
    }

    [Fact]
    public async Task DiscardChanges_追跡中の変更が破棄される()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var dispatcher = new SpyDomainEventDispatcher();
        var unitOfWork = new EfUnitOfWork<TestDbContext>(dbContext, new ImmediateDispatchStrategy(dispatcher));
        dbContext.Entities.Add(new TestEntity(1, DateTimeOffset.UtcNow));

        unitOfWork.DiscardChanges();

        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_トランザクションのネストは例外になる()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var dispatcher = new SpyDomainEventDispatcher();
        IUnitOfWork unitOfWork = new EfUnitOfWork<TestDbContext>(dbContext, new ImmediateDispatchStrategy(dispatcher));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            unitOfWork.ExecuteInTransactionAsync(
                async ct => await unitOfWork.ExecuteInTransactionAsync(_ => Task.FromResult(0), ct),
                CancellationToken.None));
    }
}
