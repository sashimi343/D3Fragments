using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.Identity.EFCore.Tests;

public sealed class CustomUserClaim : IdentityUserClaim<UuidV7>;

public sealed class CustomUserRole : IdentityUserRole<UuidV7>;

public sealed class CustomUserLogin : IdentityUserLogin<UuidV7>;

public sealed class CustomUserToken : IdentityUserToken<UuidV7>;

public sealed class CustomRoleClaim : IdentityRoleClaim<UuidV7>;

public sealed class CustomUserPasskey : IdentityUserPasskey<UuidV7>;

public sealed class CustomTypesDbContext(DbContextOptions<CustomTypesDbContext> options)
    : IdentityDbContext<Account, AccountRole, UuidV7, CustomUserClaim, CustomUserRole, CustomUserLogin, CustomRoleClaim, CustomUserToken, CustomUserPasskey>(options);

public sealed class CustomTypesUserOnlyDbContext(DbContextOptions<CustomTypesUserOnlyDbContext> options)
    : IdentityUserContext<Account, UuidV7, CustomUserClaim, CustomUserLogin, CustomUserToken, CustomUserPasskey>(options);

// Identityの基底クラスを使わないDbContext
public sealed class NonIdentityDbContext(DbContextOptions<NonIdentityDbContext> options) : DbContext(options);

public sealed class NotIdentityUser;

public sealed class NotIdentityRole;

public class D3FragmentsIdentityBuilderExtensionsTests
{
    private static Type? ImplementationOf<TService>(IServiceCollection services) =>
        Assert.Single(services, x => x.ServiceType == typeof(TService)).ImplementationType;

    [Fact]
    public void IdentityDbContextのエンティティ型でユーザーストアとロールストアを登録する()
    {
        var services = new ServiceCollection();
        services.AddIdentityCore<Account>().AddRoles<AccountRole>().AddD3FragmentsStores<CustomTypesDbContext>();

        Assert.Equal(
            typeof(D3UserStore<Account, AccountRole, CustomTypesDbContext, UuidV7, CustomUserClaim, CustomUserRole, CustomUserLogin, CustomUserToken, CustomRoleClaim, CustomUserPasskey>),
            ImplementationOf<IUserStore<Account>>(services));
        Assert.Equal(
            typeof(D3RoleStore<AccountRole, CustomTypesDbContext, UuidV7, CustomUserRole, CustomRoleClaim>),
            ImplementationOf<IRoleStore<AccountRole>>(services));
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, x => x.ServiceType == typeof(IUserStore<Account>)).Lifetime);
    }

    [Fact]
    public void Identityの基底クラスを使わないDbContextでは既定のエンティティ型を使う()
    {
        var services = new ServiceCollection();
        services.AddIdentityCore<Account>().AddRoles<AccountRole>().AddD3FragmentsStores<NonIdentityDbContext>();

        Assert.Equal(
            typeof(D3UserStore<Account, AccountRole, NonIdentityDbContext, UuidV7, IdentityUserClaim<UuidV7>, IdentityUserRole<UuidV7>, IdentityUserLogin<UuidV7>, IdentityUserToken<UuidV7>, IdentityRoleClaim<UuidV7>, IdentityUserPasskey<UuidV7>>),
            ImplementationOf<IUserStore<Account>>(services));
        Assert.Equal(
            typeof(D3RoleStore<AccountRole, NonIdentityDbContext, UuidV7, IdentityUserRole<UuidV7>, IdentityRoleClaim<UuidV7>>),
            ImplementationOf<IRoleStore<AccountRole>>(services));
    }

    [Fact]
    public void ロールを使わない場合はIdentityUserContextのエンティティ型でユーザーストアだけを登録する()
    {
        var services = new ServiceCollection();
        services.AddIdentityCore<Account>().AddD3FragmentsStores<CustomTypesUserOnlyDbContext>();

        Assert.Equal(
            typeof(D3UserOnlyStore<Account, CustomTypesUserOnlyDbContext, UuidV7, CustomUserClaim, CustomUserLogin, CustomUserToken, CustomUserPasskey>),
            ImplementationOf<IUserStore<Account>>(services));
        Assert.DoesNotContain(services, x => x.ServiceType == typeof(IRoleStore<AccountRole>));
    }

    [Fact]
    public void ロールを使わずIdentityの基底クラスも使わない場合は既定のエンティティ型を使う()
    {
        var services = new ServiceCollection();
        services.AddIdentityCore<Account>().AddD3FragmentsStores<NonIdentityDbContext>();

        Assert.Equal(
            typeof(D3UserOnlyStore<Account, NonIdentityDbContext, UuidV7, IdentityUserClaim<UuidV7>, IdentityUserLogin<UuidV7>, IdentityUserToken<UuidV7>, IdentityUserPasskey<UuidV7>>),
            ImplementationOf<IUserStore<Account>>(services));
    }

    [Fact]
    public void AddEntityFrameworkStoresで登録したストアを置き換える()
    {
        var services = new ServiceCollection();
        services.AddIdentityCore<Account>().AddRoles<AccountRole>()
            .AddEntityFrameworkStores<AppIdentityDbContext>()
            .AddD3FragmentsStores<AppIdentityDbContext>();

        Assert.Equal(typeof(D3UserStore<,,,,,,,,,>), ImplementationOf<IUserStore<Account>>(services)!.GetGenericTypeDefinition());
        Assert.Equal(typeof(D3RoleStore<,,,,>), ImplementationOf<IRoleStore<AccountRole>>(services)!.GetGenericTypeDefinition());
    }

    [Fact]
    public void ユーザーの型がIdentityUserを継承していなければ例外を送出する()
    {
        var builder = new ServiceCollection().AddIdentityCore<NotIdentityUser>();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddD3FragmentsStores<AppIdentityDbContext>());

        Assert.Contains(typeof(NotIdentityUser).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ロールの型がIdentityRoleを継承していなければ例外を送出する()
    {
        var builder = new ServiceCollection().AddIdentityCore<Account>().AddRoles<NotIdentityRole>();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddD3FragmentsStores<AppIdentityDbContext>());

        Assert.Contains(typeof(NotIdentityRole).FullName!, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void builderがnullなら例外を送出する()
    {
        Assert.Throws<ArgumentNullException>(() => ((IdentityBuilder)null!).AddD3FragmentsStores<AppIdentityDbContext>());
    }
}
