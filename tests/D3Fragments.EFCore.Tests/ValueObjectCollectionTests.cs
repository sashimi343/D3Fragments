using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace D3Fragments.EFCore.Tests;

internal sealed class Contact : AggregateRoot
{
    private readonly List<Email> _aliases = [];

    public IReadOnlyList<Email> Aliases => _aliases;

    public IReadOnlyList<string> Tags { get; private set; } = [];

    public void AddAlias(Email alias) => _aliases.Add(alias);

    public void RemoveAlias(Email alias) => _aliases.Remove(alias);
}

// バッキングフィールドが EF Core の命名規約に合わないエンティティ
internal sealed class MisnamedFieldContact : AggregateRoot
{
    private readonly List<Email> _aliasList = [];

    public IReadOnlyList<Email> Aliases => _aliasList;
}

internal sealed class ContactDbContext(DbContextOptions<ContactDbContext> options, bool complex) : D3DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();

    public bool Complex { get; } = complex;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>(builder =>
        {
            if (Complex)
            {
                builder.ComplexValueObjectCollection(x => x.Aliases);
            }
            else
            {
                builder.PrimitiveCollection(x => x.Aliases).HasValueObjectCollectionConversion();
            }
        });
    }
}

// complex が異なるコンテキストは別のモデルになるよう、キャッシュのキーに含める
internal sealed class ContactModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) => (context.GetType(), ((ContactDbContext)context).Complex, designTime);
}

internal sealed class ContactTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly bool _complex;

    public ContactTestDatabase(bool complex)
    {
        _complex = complex;
        _connection.Open();
        using var dbContext = CreateContext();
        dbContext.Database.EnsureCreated();
    }

    public ContactDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ContactDbContext>()
            .UseSqlite(_connection)
            .UseD3Fragments([typeof(TestColor).Assembly])
            .ReplaceService<IModelCacheKeyFactory, ContactModelCacheKeyFactory>()
            .Options, _complex);

    public string? ReadRawAliases()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT \"Aliases\" FROM \"Contacts\"";
        return command.ExecuteScalar() as string;
    }

    public void Dispose() => _connection.Dispose();
}

public class ValueObjectCollectionTests
{
    // バッキングフィールドの照合は規約が行うため、規約を含む ModelBuilder で構成を試す
    private static ModelBuilder CreateModelBuilder() => new(SqliteConventionSetBuilder.Build());

    [Theory]
    [InlineData(true, """[{"Value":"a@example.com"},{"Value":"b@test.jp"}]""")]
    [InlineData(false, """["a@example.com","b@test.jp"]""")]
    public async Task 値オブジェクトのコレクションを単一のJSON列に保存して復元できる(bool complex, string expectedJson)
    {
        using var database = new ContactTestDatabase(complex);
        var contact = new Contact();
        contact.AddAlias(Email.Create("a@example.com"));
        contact.AddAlias(Email.Create("b@test.jp"));
        await using (var dbContext = database.CreateContext())
        {
            dbContext.Contacts.Add(contact);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = database.CreateContext();
        var loaded = await readContext.Contacts.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal(expectedJson, database.ReadRawAliases());
        Assert.Equal([Email.Create("a@example.com"), Email.Create("b@test.jp")], loaded.Aliases);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task 読み込んだ後のコレクションへの追加と削除が保存される(bool complex)
    {
        using var database = new ContactTestDatabase(complex);
        var contact = new Contact();
        contact.AddAlias(Email.Create("a@example.com"));
        contact.AddAlias(Email.Create("b@test.jp"));
        await using (var dbContext = database.CreateContext())
        {
            dbContext.Contacts.Add(contact);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        await using (var dbContext = database.CreateContext())
        {
            var loaded = await dbContext.Contacts.SingleAsync(TestContext.Current.CancellationToken);
            loaded.RemoveAlias(Email.Create("a@example.com"));
            loaded.AddAlias(Email.Create("c@example.com"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = database.CreateContext();
        var reloaded = await readContext.Contacts.SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal([Email.Create("b@test.jp"), Email.Create("c@example.com")], reloaded.Aliases);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task 値オブジェクトどうしの比較でコレクションの要素を検索できる(bool complex)
    {
        using var database = new ContactTestDatabase(complex);
        await using var dbContext = database.CreateContext();
        var withAlias = new Contact();
        withAlias.AddAlias(Email.Create("a@example.com"));
        var withoutAlias = new Contact();
        withoutAlias.AddAlias(Email.Create("b@test.jp"));
        dbContext.Contacts.AddRange(withAlias, withoutAlias);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        var email = Email.Create("a@example.com");

        var found = await dbContext.Contacts.SingleAsync(x => x.Aliases.Contains(email), TestContext.Current.CancellationToken);

        Assert.Equal(withAlias.Id, found.Id);
    }

    [Fact]
    public async Task ComplexValueObjectCollectionでは要素のプリミティブ値で検索できる()
    {
        using var database = new ContactTestDatabase(complex: true);
        await using var dbContext = database.CreateContext();
        var withAlias = new Contact();
        withAlias.AddAlias(Email.Create("a@example.com"));
        var withoutAlias = new Contact();
        withoutAlias.AddAlias(Email.Create("b@test.jp"));
        dbContext.Contacts.AddRange(withAlias, withoutAlias);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var found = await dbContext.Contacts.SingleAsync(
            x => x.Aliases.Any(a => a.Value.EndsWith("@example.com")), TestContext.Current.CancellationToken);

        Assert.Equal(withAlias.Id, found.Id);
    }

    [Theory]
    [InlineData(true, """[{"Value":"invalid"}]""")]
    [InlineData(false, """["invalid"]""")]
    public async Task DBの要素が値オブジェクトの検証を満たさない場合は読み込み時に例外を送出する(bool complex, string invalidJson)
    {
        using var database = new ContactTestDatabase(complex);
        await using (var dbContext = database.CreateContext())
        {
            var contact = new Contact();
            contact.AddAlias(Email.Create("a@example.com"));
            dbContext.Contacts.Add(contact);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
            await dbContext.Database.ExecuteSqlAsync($"UPDATE \"Contacts\" SET \"Aliases\" = {invalidJson}", TestContext.Current.CancellationToken);
        }

        await using var readContext = database.CreateContext();

        await Assert.ThrowsAsync<DomainValidationException>(() => readContext.Contacts.ToListAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ComplexValueObjectCollectionの要素が値オブジェクトでない場合はInvalidOperationExceptionを送出する()
    {
        var builder = CreateModelBuilder().Entity<Contact>();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.ComplexValueObjectCollection(x => x.Tags));

        Assert.Contains(typeof(string).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ValueObjectComplexTypeExtensions.ComplexValueObjectCollection), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HasValueObjectCollectionConversionの要素が値オブジェクトでない場合はInvalidOperationExceptionを送出する()
    {
        var builder = CreateModelBuilder().Entity<Contact>();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.PrimitiveCollection(x => x.Tags).HasValueObjectCollectionConversion());

        Assert.Contains(typeof(string).FullName!, exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ValueObjectConversionExtensions.HasValueObjectCollectionConversion), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ComplexValueObjectにコレクションを渡すとInvalidOperationExceptionを送出する()
    {
        var builder = CreateModelBuilder().Entity<Mailbox>();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.ComplexValueObject(x => x.Aliases));

        Assert.Contains(typeof(List<Email>).ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ValueObjectComplexTypeExtensions.ComplexValueObject), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ComplexValueObjectCollectionでバッキングフィールドが見つからない場合はInvalidOperationExceptionを送出する()
    {
        var builder = CreateModelBuilder().Entity<MisnamedFieldContact>();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.ComplexValueObjectCollection(x => x.Aliases));

        Assert.Contains("MisnamedFieldContact.Aliases", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HasValueObjectCollectionConversionでバッキングフィールドが見つからない場合はInvalidOperationExceptionを送出する()
    {
        var builder = CreateModelBuilder().Entity<MisnamedFieldContact>();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.PrimitiveCollection(x => x.Aliases).HasValueObjectCollectionConversion());

        Assert.Contains("MisnamedFieldContact.Aliases", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void setterのあるコレクションはバッキングフィールドがなくても構成できる()
    {
        var builder = CreateModelBuilder().Entity<Mailbox>();

        var exception = Record.Exception(() => builder.ComplexValueObjectCollection(x => x.Aliases));

        Assert.Null(exception);
    }

    [Fact]
    public void ComplexValueObjectCollectionに単純なプロパティアクセスでない式を渡すとArgumentExceptionを送出する()
    {
        var builder = CreateModelBuilder().Entity<Contact>();

        Assert.Throws<ArgumentException>(() => builder.ComplexValueObjectCollection(x => x.Aliases.Skip(1)));
    }

    [Fact]
    public void ComplexValueObjectCollectionの引数がnullの場合はArgumentNullExceptionを送出する()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ValueObjectComplexTypeExtensions.ComplexValueObjectCollection<Contact, Email>(null!, x => x.Aliases));
        Assert.Throws<ArgumentNullException>(() => CreateModelBuilder().Entity<Contact>().ComplexValueObjectCollection<Contact, Email>(null!));
    }

    [Fact]
    public void HasValueObjectCollectionConversionのbuilderがnullの場合はArgumentNullExceptionを送出する() =>
        Assert.Throws<ArgumentNullException>(() => ValueObjectConversionExtensions.HasValueObjectCollectionConversion<List<Email>>(null!));
}
