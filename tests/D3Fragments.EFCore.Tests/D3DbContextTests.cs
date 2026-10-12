using System.Diagnostics.CodeAnalysis;

using D3Fragments.Application;
using D3Fragments.EFCore.DomainEvents;
using D3Fragments.Testing;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
    public Member(Email email, Email? subEmail, TestColor color, UuidV7? referrerId)
    {
        Email = email;
        SubEmail = subEmail;
        Color = color;
        ReferrerId = referrerId;
    }

    // EF Core用（Complex Typeはコンストラクタ引数にバインドできない）
    private Member()
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

    public MemberDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MemberDbContext>()
            .UseSqlite(_connection)
            .UseD3Fragments([typeof(TestColor).Assembly])
            .Options);

    public void Dispose() => _connection.Dispose();
}

public class D3DbContextTests
{
    [Fact]
    public async Task UuidV7_TypeSafeEnum_ValueObjectを保存して復元できる()
    {
        using var database = new SqliteTestDatabase();
        var referrerId = UuidV7.NewUuidV7();
        var withOptional = new Member(Email.Create("a@example.com"), Email.Create("sub@example.com"), TestColor.Red, referrerId);
        var withoutOptional = new Member(Email.Create("b@example.com"), null, TestColor.Red, null);
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
            new Member(Email.Create("a@example.com"), null, TestColor.Red, null),
            new Member(Email.Create("b@other.test"), null, TestColor.Red, null));
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
        var member = new Member(Email.Create("a@example.com"), null, TestColor.Red, null);
        var createdAt = member.CreatedAt;
        await using (var dbContext = database.CreateContext())
        {
            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // 新規作成時は CreatedAt のまま
            Assert.Equal(createdAt, member.UpdatedAt);
        }

        var before = DateTimeOffset.UtcNow;
        await using (var dbContext = database.CreateContext())
        {
            var loaded = await dbContext.Members.SingleAsync(TestContext.Current.CancellationToken);
            loaded.ChangeEmail(Email.Create("changed@example.com"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = database.CreateContext();
        var reloaded = await readContext.Members.SingleAsync(TestContext.Current.CancellationToken);
        Assert.InRange(reloaded.UpdatedAt, before, DateTimeOffset.UtcNow);
        Assert.Equal(createdAt, reloaded.CreatedAt);
    }

    [Fact]
    public void Versionが同時実行トークンに設定される()
    {
        using var database = new SqliteTestDatabase();
        using var dbContext = database.CreateContext();
        var entityType = dbContext.Model.FindEntityType(typeof(Member))!;

        Assert.True(entityType.FindProperty(nameof(Member.Version))!.IsConcurrencyToken);
    }

    [Fact]
    public async Task 変更したエンティティのVersionが保存時に加算される()
    {
        using var database = new SqliteTestDatabase();
        var member = new Member(Email.Create("a@example.com"), null, TestColor.Red, null);
        await using (var dbContext = database.CreateContext())
        {
            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

            // 新規作成時は加算しない
            Assert.Equal(0, member.Version);

            member.ChangeEmail(Email.Create("changed@example.com"));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var readContext = database.CreateContext();
        Assert.Equal(1, (await readContext.Members.SingleAsync(TestContext.Current.CancellationToken)).Version);
    }

    [Fact]
    public async Task 他のコンテキストで先に更新されていると保存時に競合を検出する()
    {
        using var database = new SqliteTestDatabase();
        await using (var dbContext = database.CreateContext())
        {
            dbContext.Members.Add(new Member(Email.Create("a@example.com"), null, TestColor.Red, null));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var firstMember = await first.Members.SingleAsync(TestContext.Current.CancellationToken);
        var secondMember = await second.Members.SingleAsync(TestContext.Current.CancellationToken);

        firstMember.ChangeEmail(Email.Create("first@example.com"));
        await first.SaveChangesAsync(TestContext.Current.CancellationToken);

        secondMember.ChangeEmail(Email.Create("second@example.com"));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EfUnitOfWorkは競合をConflictOccurredExceptionに変換する()
    {
        using var database = new SqliteTestDatabase();
        await using (var dbContext = database.CreateContext())
        {
            dbContext.Members.Add(new Member(Email.Create("a@example.com"), null, TestColor.Red, null));
            await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        var secondMember = await second.Members.SingleAsync(TestContext.Current.CancellationToken);
        (await first.Members.SingleAsync(TestContext.Current.CancellationToken)).ChangeEmail(Email.Create("first@example.com"));
        await first.SaveChangesAsync(TestContext.Current.CancellationToken);

        secondMember.ChangeEmail(Email.Create("second@example.com"));
        var dispatcher = new SpyDomainEventDispatcher();
        var unitOfWork = new EfUnitOfWork<MemberDbContext>(
            second, dispatcher, new ImmediateDispatchStrategy(dispatcher), NullLogger<EfUnitOfWork<MemberDbContext>>.Instance, Options.Create(new D3FragmentsApplicationOptions()));

        var ex = await Assert.ThrowsAsync<ConflictOccurredException>(() => unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.IsType<DbUpdateConcurrencyException>(ex.InnerException);
    }
}

public class RepositoryBaseTests
{
    [Fact]
    public async Task 追加_取得_件数_存在確認_削除ができる()
    {
        using var database = new SqliteTestDatabase();
        var member = new Member(Email.Create("a@example.com"), null, TestColor.Red, null);
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
