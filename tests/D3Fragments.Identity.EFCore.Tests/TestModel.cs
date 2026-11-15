using D3Fragments.Application;
using D3Fragments.EFCore;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.Identity.EFCore.Tests;

public sealed class Account : IdentityUser<UuidV7>
{
    // EF Core用
    private Account()
    {
    }

    public Account(string userName)
    {
        Id = UuidV7.NewUuidV7();
        UserName = userName;
    }
}

public sealed class AccountRole : IdentityRole<UuidV7>
{
    // EF Core用
    private AccountRole()
    {
    }

    public AccountRole(string name)
    {
        Id = UuidV7.NewUuidV7();
        Name = name;
    }
}

public sealed record MemberRegistered(DateTimeOffset OccurredAt) : IDomainEvent;

public sealed class Member : AggregateRoot
{
    // EF Core用
    private Member()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public static Member Register(string name)
    {
        var member = new Member { Name = name };
        member.AddDomainEvent(new MemberRegistered(DateTimeOffset.UtcNow));
        return member;
    }
}

// Identityとドメインのエンティティを1つのDbContextで扱う（D3DbContextを継承しない構成）
public sealed class AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
    : IdentityDbContext<Account, AccountRole, UuidV7>(options)
{
    public DbSet<Member> Members => Set<Member>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ApplyD3FragmentsConventions(options);
    }
}

// ロールを使わない構成
public sealed class AppUserOnlyDbContext(DbContextOptions<AppUserOnlyDbContext> options)
    : IdentityUserContext<Account, UuidV7>(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ApplyD3FragmentsConventions(options);
    }
}

// Commit前ハンドラーが呼ばれた時点でDBに保存済みのMemberの件数を記録する
public sealed class MemberRegisteredProbe
{
    public int Calls { get; set; }

    public int MembersInDatabase { get; set; } = -1;
}

public sealed class MemberRegisteredHandler(MemberRegisteredProbe probe, AppIdentityDbContext dbContext)
    : IBeforeCommitDomainEventHandler<MemberRegistered>
{
    public async Task HandleAsync(MemberRegistered domainEvent, CancellationToken cancellationToken)
    {
        probe.Calls++;
        probe.MembersInDatabase = await dbContext.Members.AsNoTracking().CountAsync(cancellationToken);
    }
}

/// <summary>
/// インメモリSQLiteを共有し、Identity（D3Fragmentsのストア）とD3FragmentsのUnit of Workを登録したサービスを生成する
/// </summary>
public sealed class IdentityTestHost : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public IdentityTestHost(bool useRoles = true)
    {
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MemberRegisteredProbe>();
        services.AddD3FragmentsApplication([typeof(IdentityTestHost).Assembly]);
        if (useRoles)
        {
            services.AddD3FragmentsEFCore<AppIdentityDbContext>([typeof(IdentityTestHost).Assembly], db => db.UseSqlite(_connection));
            services.AddIdentityCore<Account>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
                .AddRoles<AccountRole>()
                .AddD3FragmentsStores<AppIdentityDbContext>();
        }
        else
        {
            services.AddD3FragmentsEFCore<AppUserOnlyDbContext>([typeof(IdentityTestHost).Assembly], db => db.UseSqlite(_connection));
            services.AddIdentityCore<Account>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3)
                .AddD3FragmentsStores<AppUserOnlyDbContext>();
        }

        Services = services.BuildServiceProvider(validateScopes: true);
        using var scope = Services.CreateScope();
        (useRoles ? (DbContext)scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>() : scope.ServiceProvider.GetRequiredService<AppUserOnlyDbContext>())
            .Database.EnsureCreated();
    }

    public ServiceProvider Services { get; }

    public MemberRegisteredProbe Probe => Services.GetRequiredService<MemberRegisteredProbe>();

    // 新しいスコープ（1リクエスト相当）で処理を実行する
    public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    public Task InScopeAsync(Func<IServiceProvider, Task> action) =>
        InScopeAsync(async provider =>
        {
            await action(provider);
            return 0;
        });

    public void Dispose()
    {
        Services.Dispose();
        _connection.Dispose();
    }
}

public static class ServiceProviderExtensions
{
    public static UserManager<Account> Users(this IServiceProvider provider) => provider.GetRequiredService<UserManager<Account>>();

    public static RoleManager<AccountRole> Roles(this IServiceProvider provider) => provider.GetRequiredService<RoleManager<AccountRole>>();

    public static Task SaveAsync(this IServiceProvider provider) =>
        provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(TestContext.Current.CancellationToken);
}
