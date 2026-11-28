using D3Fragments.Application;
using D3Fragments.EFCore.DomainEvents;
using D3Fragments.Testing;

using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore.Tests;

internal sealed record TestDomainEvent(DateTimeOffset OccurredAt) : IDomainEvent;

internal sealed class TestEntity : AggregateRoot
{
    public TestEntity(int value)
    {
        Value = value;
    }

    public int Value { get; private set; }

    public void RaiseTestEvent(DateTimeOffset occurredAt) => AddDomainEvent(new TestDomainEvent(occurredAt));

    public void Raise(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);

    public void ChangeValue(int value) => Value = value;
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
