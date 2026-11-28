using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace D3Fragments.EFCore.Tests;

internal sealed class Subscriber : AggregateRoot
{
    public Subscriber(Email email, Email? subEmail)
    {
        Email = email;
        SubEmail = subEmail;
    }

    // EF Core用
    private Subscriber()
    {
        Email = null!;
    }

    public Email Email { get; private set; }

    public Email? SubEmail { get; private set; }

    public string Note { get; private set; } = "";
}

// 自身ではない型を TSelf として IValueObject を実装している型
public sealed class NotSelfValueObject : IValueObject<Email, string>
{
    public string Value => "";

    public static Email Create(string value) => Email.Create(value);

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;
        return true;
    }
}

internal sealed class SubscriberDbContext(DbContextOptions<SubscriberDbContext> options) : D3DbContext(options)
{
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscriber>(builder =>
        {
            builder.Property(x => x.Email).HasValueObjectConversion();
            builder.Property(x => x.SubEmail).HasValueObjectConversion();
            builder.HasIndex(x => x.Email).IsUnique();
        });
    }
}

internal sealed class SubscriberTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SubscriberTestDatabase()
    {
        _connection.Open();
        using var dbContext = CreateContext();
        dbContext.Database.EnsureCreated();
    }

    public SubscriberDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<SubscriberDbContext>()
            .UseSqlite(_connection)
            .UseD3Fragments([typeof(TestColor).Assembly])
            .Options);

    public void Dispose() => _connection.Dispose();
}

public class ValueObjectConversionTests
{
    [Fact]
    public async Task 値コンバーターでマッピングした値オブジェクトを保存して復元できる()
    {
        using var database = new SubscriberTestDatabase();
        var withSub = new Subscriber(Email.Create("a@example.com"), Email.Create("sub@example.com"));
        var withoutSub = new Subscriber(Email.Create("b@example.com"), null);
        await using (var dbContext = database.CreateContext())
        {
            dbContext.Subscribers.AddRange(withSub, withoutSub);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = database.CreateContext();
        var loaded = await readContext.Subscribers.SingleAsync(x => x.Id == withSub.Id, TestContext.Current.CancellationToken);
        var loadedWithoutSub = await readContext.Subscribers.SingleAsync(x => x.Id == withoutSub.Id, TestContext.Current.CancellationToken);

        Assert.Equal(Email.Create("a@example.com"), loaded.Email);
        Assert.Equal(Email.Create("sub@example.com"), loaded.SubEmail);
        Assert.Equal(Email.Create("b@example.com"), loadedWithoutSub.Email);
        Assert.Null(loadedWithoutSub.SubEmail);
    }

    [Fact]
    public async Task 値オブジェクトどうしの比較で検索できる()
    {
        using var database = new SubscriberTestDatabase();
        await using var dbContext = database.CreateContext();
        dbContext.Subscribers.AddRange(
            new Subscriber(Email.Create("a@example.com"), null),
            new Subscriber(Email.Create("b@example.com"), null));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var email = Email.Create("b@example.com");

        var found = await dbContext.Subscribers.SingleAsync(x => x.Email == email, TestContext.Current.CancellationToken);

        Assert.Equal(email, found.Email);
    }

    [Fact]
    public async Task 一意インデックスに違反するとDbUpdateExceptionを送出する()
    {
        using var database = new SubscriberTestDatabase();
        await using var dbContext = database.CreateContext();
        dbContext.Subscribers.AddRange(
            new Subscriber(Email.Create("a@example.com"), null),
            new Subscriber(Email.Create("a@example.com"), null));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DBの値が値オブジェクトの検証を満たさない場合は読み込み時に例外を送出する()
    {
        using var database = new SubscriberTestDatabase();
        await using (var dbContext = database.CreateContext())
        {
            dbContext.Subscribers.Add(new Subscriber(Email.Create("a@example.com"), null));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            await dbContext.Database.ExecuteSqlRawAsync("UPDATE \"Subscribers\" SET \"Email\" = 'invalid'", TestContext.Current.CancellationToken);
        }

        await using var readContext = database.CreateContext();

        await Assert.ThrowsAsync<DomainValidationException>(() => readContext.Subscribers.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void 値コンバーターと一意インデックスが設定され列名は設定されない()
    {
        using var database = new SubscriberTestDatabase();
        using var dbContext = database.CreateContext();
        var entityType = dbContext.Model.FindEntityType(typeof(Subscriber))!;
        var property = entityType.FindProperty(nameof(Subscriber.Email))!;

        Assert.IsType<ValueObjectConverter<Email, string>>(property.GetValueConverter());
        Assert.IsType<ValueObjectConverter<Email, string>>(entityType.FindProperty(nameof(Subscriber.SubEmail))!.GetValueConverter());
        Assert.True(Assert.Single(property.GetContainingIndexes()).IsUnique);
        // 列名を明示しないため、命名規約が列名を決められる（構成元はデザイン時のモデルにだけ残る）
        var designTimeProperty = (IConventionProperty)dbContext.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(Subscriber))!.FindProperty(nameof(Subscriber.Email))!;
        Assert.Null(designTimeProperty.GetColumnNameConfigurationSource());
    }

    [Fact]
    public void 値オブジェクトでない型に使うとInvalidOperationExceptionを送出する()
    {
        var builder = new ModelBuilder().Entity<Subscriber>();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Property(x => x.Note).HasValueObjectConversion());

        Assert.Contains(typeof(string).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void 自身をTSelfとしてIValueObjectを実装していない型に使うとInvalidOperationExceptionを送出する()
    {
        var builder = new ModelBuilder().Entity<Subscriber>();
        builder.Ignore(x => x.Email).Ignore(x => x.SubEmail);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            builder.Property<NotSelfValueObject>("Fake").HasValueObjectConversion());

        Assert.Contains(typeof(NotSelfValueObject).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void builderがnullの場合はArgumentNullExceptionを送出する() =>
        Assert.Throws<ArgumentNullException>(() => ValueObjectConversionExtensions.HasValueObjectConversion<Email>(null!));
}
