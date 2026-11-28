using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace D3Fragments.EFCore.Tests;

internal sealed class ComplexContact
{
    public int Id { get; set; }

    public Email EmailAddress { get; set; } = null!;

    public Email OverriddenAddress { get; set; } = null!;
}

internal sealed class ConvertedContact
{
    public int Id { get; set; }

    public Email EmailAddress { get; set; } = null!;
}

internal sealed class NamingConventionDbContext(DbContextOptions<NamingConventionDbContext> options) : D3DbContext(options)
{
    public DbSet<ComplexContact> ComplexContacts => Set<ComplexContact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComplexContact>(builder =>
        {
            builder.ComplexValueObject(x => x.EmailAddress);
            builder.ComplexValueObject(x => x.OverriddenAddress).Property(x => x.Value).HasColumnName("custom_column");
        });
        modelBuilder.Entity<ConvertedContact>().Property(x => x.EmailAddress).HasValueObjectConversion();
    }
}

public class ValueObjectNamingConventionTests
{
    public static TheoryData<string> NamingConventions => ["none", "snake", "camel", "lower", "upper", "uppersnake"];

    [Theory]
    [MemberData(nameof(NamingConventions))]
    public void ComplexValueObjectの列名はHasValueObjectConversionと同じ命名規約で決まる(string namingConvention)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        using var dbContext = CreateContext(connection, namingConvention);

        Assert.Equal(GetConvertedColumnName(dbContext.Model), GetComplexColumnName(dbContext.Model, nameof(ComplexContact.EmailAddress)));
    }

    [Theory]
    [MemberData(nameof(NamingConventions))]
    public void ComplexValueObjectで明示した列名は命名規約で書き換えられない(string namingConvention)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        using var dbContext = CreateContext(connection, namingConvention);

        Assert.Equal("custom_column", GetComplexColumnName(dbContext.Model, nameof(ComplexContact.OverriddenAddress)));
    }

    [Fact]
    public async Task snake_caseの命名規約でComplexValueObjectを保存して検索できる()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        await using (var dbContext = CreateContext(connection, "snake"))
        {
            await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            dbContext.ComplexContacts.Add(new ComplexContact { EmailAddress = Email.Create("a@example.com"), OverriddenAddress = Email.Create("b@example.com") });
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = CreateContext(connection, "snake");
        var found = await readContext.ComplexContacts.SingleAsync(x => x.EmailAddress.Value.EndsWith("@example.com"), TestContext.Current.CancellationToken);

        Assert.Equal(Email.Create("a@example.com"), found.EmailAddress);
        Assert.Contains("\"email_address\"", readContext.Database.GenerateCreateScript(), StringComparison.Ordinal);
    }

    private static NamingConventionDbContext CreateContext(SqliteConnection connection, string namingConvention)
    {
        var builder = new DbContextOptionsBuilder<NamingConventionDbContext>()
            .UseSqlite(connection)
            .UseD3Fragments([typeof(TestColor).Assembly]);
        _ = namingConvention switch
        {
            "none" => builder,
            "snake" => builder.UseSnakeCaseNamingConvention(),
            "camel" => builder.UseCamelCaseNamingConvention(),
            "lower" => builder.UseLowerCaseNamingConvention(),
            "upper" => builder.UseUpperCaseNamingConvention(),
            "uppersnake" => builder.UseUpperSnakeCaseNamingConvention(),
            _ => throw new ArgumentOutOfRangeException(nameof(namingConvention)),
        };
        return new NamingConventionDbContext(builder.Options);
    }

    private static string? GetComplexColumnName(IModel model, string propertyName)
    {
        var entityType = model.FindEntityType(typeof(ComplexContact))!;
        return entityType.FindComplexProperty(propertyName)!.ComplexType.FindProperty(nameof(Email.Value))!
            .GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName()!));
    }

    private static string? GetConvertedColumnName(IModel model)
    {
        var entityType = model.FindEntityType(typeof(ConvertedContact))!;
        return entityType.FindProperty(nameof(ConvertedContact.EmailAddress))!
            .GetColumnName(StoreObjectIdentifier.Table(entityType.GetTableName()!));
    }
}
