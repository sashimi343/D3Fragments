using System.Diagnostics.CodeAnalysis;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore.Tests;

public sealed class Email : ValueObjectBase<Email, string>, IValueObject<Email, string>
{
    private Email(string value) : base(value)
    {
    }

    public static Email Create(string value) => new(value);

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = value.Contains('@', StringComparison.Ordinal) ? null : "invalid email";
        return errorMessage is null;
    }
}

internal sealed class Member : AggregateRoot
{
    public Member(Email email, Email? subEmail, TestColor color, UuidV7? referrerId, DateTimeOffset createdAt) : base(createdAt)
    {
        Email = email;
        SubEmail = subEmail;
        Color = color;
        ReferrerId = referrerId;
    }

    // EF Core用（Complex Typeはコンストラクタ引数にバインドできない）
    private Member() : base(DateTimeOffset.MinValue)
    {
        Email = null!;
        Color = null!;
    }

    public Email Email { get; private set; }

    public Email? SubEmail { get; private set; }

    public TestColor Color { get; private set; }

    public UuidV7? ReferrerId { get; private set; }

    public void ChangeEmail(Email email) => Email = email;

    public DeletionToken<Member> Delete() => AllowDeletion(this);
}

internal sealed class MemberDbContext(DbContextOptions<MemberDbContext> options) : D3DbContext(options)
{
    public DbSet<Member> Members => Set<Member>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Member>(builder =>
        {
            builder.ComplexValueObject(x => x.Email);
            builder.ComplexValueObject(x => x.SubEmail);
        });
    }
}

internal sealed class MemberRepository(MemberDbContext dbContext) : RepositoryBase<MemberDbContext, Member>(dbContext);

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>
/// 1つのインメモリSQLiteデータベースを共有するDbContextを生成する（接続を閉じるとDBが消える）
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using var dbContext = CreateContext();
        dbContext.Database.EnsureCreated();
    }

    public MemberDbContext CreateContext(TimeProvider? timeProvider = null) =>
        new(new DbContextOptionsBuilder<MemberDbContext>()
            .UseSqlite(_connection)
            .UseD3Fragments([typeof(TestColor).Assembly], timeProvider)
            .Options);

    public void Dispose() => _connection.Dispose();
}

public class D3DbContextTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UuidV7_TypeSafeEnum_ValueObjectを保存して復元できる()
    {
        using var database = new SqliteTestDatabase();
        var referrerId = UuidV7.NewUuidV7();
        var withOptional = new Member(Email.Create("a@example.com"), Email.Create("sub@example.com"), TestColor.Red, referrerId, CreatedAt);
        var withoutOptional = new Member(Email.Create("b@example.com"), null, TestColor.Red, null, CreatedAt);
        await using (var dbContext = database.CreateContext())
        {
            dbContext.Members.AddRange(withOptional, withoutOptional);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = database.CreateContext();
        var loaded = await readContext.Members.SingleAsync(x => x.Id == withOptional.Id, TestContext.Current.CancellationToken);
        var loadedWithoutOptional = await readContext.Members.SingleAsync(x => x.Id == withoutOptional.Id, TestContext.Current.CancellationToken);

        Assert.Equal(Email.Create("a@example.com"), loaded.Email);
        Assert.Equal(Email.Create("sub@example.com"), loaded.SubEmail);
        Assert.Same(TestColor.Red, loaded.Color);
        Assert.Equal(referrerId, loaded.ReferrerId);
        Assert.Null(loadedWithoutOptional.SubEmail);
        Assert.Null(loadedWithoutOptional.ReferrerId);
    }

    [Fact]
    public async Task ValueObjectのプリミティブ値で部分一致検索できる()
    {
        using var database = new SqliteTestDatabase();
        await using var dbContext = database.CreateContext();
        dbContext.Members.AddRange(
            new Member(Email.Create("a@example.com"), null, TestColor.Red, null, CreatedAt),
            new Member(Email.Create("b@other.test"), null, TestColor.Red, null, CreatedAt));
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var count = await dbContext.Members.CountAsync(x => x.Email.Value.Contains("example"), TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
    }

    [Fact]
    public void モデルにコンバーターと列名が設定される()
    {
        using var database = new SqliteTestDatabase();
        using var dbContext = database.CreateContext();
        var entityType = dbContext.Model.FindEntityType(typeof(Member))!;

        Assert.IsType<UuidV7Converter>(entityType.FindProperty(nameof(Member.Id))!.GetValueConverter());
        Assert.IsType<UuidV7Converter>(entityType.FindProperty(nameof(Member.ReferrerId))!.GetValueConverter());
        Assert.IsType<TypeSafeEnumConverter<TestColor, int>>(entityType.FindProperty(nameof(Member.Color))!.GetValueConverter());
        Assert.Equal("Email", entityType.FindComplexProperty(nameof(Member.Email))!.ComplexType.FindProperty("Value")!.GetColumnName());
    }

    [Fact]
    public async Task 変更したエンティティのUpdatedAtが保存時に更新される()
    {
        using var database = new SqliteTestDatabase();
        var member = new Member(Email.Create("a@example.com"), null, TestColor.Red, null, CreatedAt);
        await using (var dbContext = database.CreateContext(new FixedTimeProvider(CreatedAt.AddDays(1))))
        {
            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // 新規作成時は CreatedAt のまま
            Assert.Equal(CreatedAt, member.UpdatedAt);
        }

        var updatedAt = CreatedAt.AddDays(2);
        await using (var dbContext = database.CreateContext(new FixedTimeProvider(updatedAt)))
        {
            var loaded = await dbContext.Members.SingleAsync(TestContext.Current.CancellationToken);
            loaded.ChangeEmail(Email.Create("changed@example.com"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = database.CreateContext();
        var reloaded = await readContext.Members.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(updatedAt, reloaded.UpdatedAt);
        Assert.Equal(CreatedAt, reloaded.CreatedAt);
    }
}

public class RepositoryBaseTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task 追加_取得_件数_存在確認_削除ができる()
    {
        using var database = new SqliteTestDatabase();
        var member = new Member(Email.Create("a@example.com"), null, TestColor.Red, null, CreatedAt);
        await using (var dbContext = database.CreateContext())
        {
            new MemberRepository(dbContext).Add(member);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var dbContext = database.CreateContext())
        {
            var repository = new MemberRepository(dbContext);

            Assert.Equal(1, await repository.CountAsync(CancellationToken.None));
            Assert.True(await repository.ExistsAsync(member.Id, CancellationToken.None));
            Assert.False(await repository.ExistsAsync(UuidV7.NewUuidV7(), CancellationToken.None));

            var loaded = await repository.FindAsync(member.Id, CancellationToken.None);
            Assert.NotNull(loaded);
            repository.Delete(loaded.Delete());
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var dbContext = database.CreateContext())
        {
            var repository = new MemberRepository(dbContext);
            Assert.False(await repository.ExistsAsync(member.Id, CancellationToken.None));
            Assert.Null(await repository.FindAsync(member.Id, CancellationToken.None));
        }
    }
}
