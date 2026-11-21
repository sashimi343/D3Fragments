using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.Identity.EFCore.Tests;

public sealed class D3UserStoreTests : IDisposable
{
    private readonly IdentityTestHost _host = new();

    public void Dispose() => _host.Dispose();

    private Task<Account?> FindAsync(string userName) =>
        _host.InScopeAsync(provider => provider.Users().FindByNameAsync(userName));

    private async Task CreateAsync(string userName, string? password = null)
    {
        await _host.InScopeAsync(async provider =>
        {
            var account = new Account(userName);
            var result = password is null
                ? await provider.Users().CreateAsync(account)
                : await provider.Users().CreateAsync(account, password);
            Assert.True(result.Succeeded);
            await provider.SaveAsync();
        });
    }

    [Fact]
    public async Task UserManagerの操作だけでは保存されず_Unit_of_Workの保存でドメインの変更とまとめて保存される()
    {
        await _host.InScopeAsync(async provider =>
        {
            provider.GetRequiredService<AppIdentityDbContext>().Members.Add(Member.Register("taro"));
            Assert.True((await provider.Users().CreateAsync(new Account("taro"))).Succeeded);

            Assert.Null(await FindAsync("taro"));
            Assert.Equal(0, _host.Probe.Calls);

            await provider.SaveAsync();
        });

        Assert.NotNull(await FindAsync("taro"));
        Assert.Equal(1, _host.Probe.Calls);
        // Commit前ハンドラーはドメインの変更が保存される前に呼ばれる
        Assert.Equal(0, _host.Probe.MembersInDatabase);
    }

    [Fact]
    public async Task UuidV7をキーにしたユーザーをIDで検索できる()
    {
        await CreateAsync("taro");
        var account = await FindAsync("taro");

        var found = await _host.InScopeAsync(provider => provider.Users().FindByIdAsync(account!.Id.ToString()));

        Assert.Equal(account!.Id, found!.Id);
    }

    [Fact]
    public async Task ユーザーの作成とロールへの追加を1回の保存で行える()
    {
        await _host.InScopeAsync(async provider =>
        {
            Assert.True((await provider.Roles().CreateAsync(new AccountRole("Admin"))).Succeeded);
            await provider.SaveAsync();
        });

        await _host.InScopeAsync(async provider =>
        {
            var account = new Account("taro");
            Assert.True((await provider.Users().CreateAsync(account)).Succeeded);
            Assert.True((await provider.Users().AddToRoleAsync(account, "Admin")).Succeeded);
            await provider.SaveAsync();
        });

        var roles = await _host.InScopeAsync(async provider => await provider.Users().GetRolesAsync((await provider.Users().FindByNameAsync("taro"))!));
        Assert.Equal(["Admin"], roles);
    }

    [Fact]
    public async Task ユーザーの作成とsecurity_stampの更新を1回の保存で行える()
    {
        await _host.InScopeAsync(async provider =>
        {
            var account = new Account("taro");
            Assert.True((await provider.Users().CreateAsync(account)).Succeeded);
            Assert.True((await provider.Users().UpdateSecurityStampAsync(account)).Succeeded);
            await provider.SaveAsync();
        });

        Assert.NotNull(await FindAsync("taro"));
    }

    [Fact]
    public async Task 既存のユーザーへの複数の更新を1回の保存で行える()
    {
        await CreateAsync("taro", "Passw0rd!");

        await _host.InScopeAsync(async provider =>
        {
            var users = provider.Users();
            var account = (await users.FindByNameAsync("taro"))!;
            Assert.True((await users.RemovePasswordAsync(account)).Succeeded);
            Assert.True((await users.AddPasswordAsync(account, "NewPassw0rd!")).Succeeded);
            Assert.True((await users.SetLockoutEndDateAsync(account, DateTimeOffset.MaxValue)).Succeeded);
            Assert.True((await users.AddClaimAsync(account, new Claim("type", "value"))).Succeeded);
            await provider.SaveAsync();
        });

        await _host.InScopeAsync(async provider =>
        {
            var account = (await provider.Users().FindByNameAsync("taro"))!;
            Assert.True(await provider.Users().CheckPasswordAsync(account, "NewPassw0rd!"));
            Assert.Equal(DateTimeOffset.MaxValue, account.LockoutEnd);
            Assert.Single(await provider.Users().GetClaimsAsync(account));
        });
    }

    [Fact]
    public async Task 追跡されていないユーザーも更新できる()
    {
        await CreateAsync("taro");
        var detached = await _host.InScopeAsync(provider =>
            provider.GetRequiredService<AppIdentityDbContext>().Users.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken));

        await _host.InScopeAsync(async provider =>
        {
            detached.PhoneNumber = "090-0000-0000";
            // UserManager.UpdateAsyncは検証で同じユーザーを別インスタンスとして読み込むため（Identityの制約）、ストアを直接呼ぶ
            Assert.True((await provider.GetRequiredService<IUserStore<Account>>().UpdateAsync(detached, TestContext.Current.CancellationToken)).Succeeded);
            await provider.SaveAsync();
        });

        Assert.Equal("090-0000-0000", (await FindAsync("taro"))!.PhoneNumber);
    }

    [Fact]
    public async Task 同じ保存単位でのユーザー名の重複は保存時にDbUpdateExceptionになる()
    {
        await _host.InScopeAsync(async provider =>
        {
            Assert.True((await provider.Users().CreateAsync(new Account("taro"))).Succeeded);
            Assert.True((await provider.Users().CreateAsync(new Account("taro"))).Succeeded);

            await Assert.ThrowsAsync<DbUpdateException>(provider.SaveAsync);
        });
    }

    [Fact]
    public async Task 同じユーザーを別々に更新すると後から保存した方が競合する()
    {
        await CreateAsync("taro");
        await using var first = _host.Services.CreateAsyncScope();
        await using var second = _host.Services.CreateAsyncScope();
        var firstAccount = (await first.ServiceProvider.Users().FindByNameAsync("taro"))!;
        var secondAccount = (await second.ServiceProvider.Users().FindByNameAsync("taro"))!;

        await first.ServiceProvider.Users().UpdateSecurityStampAsync(firstAccount);
        await first.ServiceProvider.SaveAsync();
        Assert.True((await second.ServiceProvider.Users().UpdateSecurityStampAsync(secondAccount)).Succeeded);

        await Assert.ThrowsAsync<ConflictOccurredException>(second.ServiceProvider.SaveAsync);
    }

    [Fact]
    public async Task UpdateAsync_引数と状態を検証する()
    {
        await _host.InScopeAsync(async provider =>
        {
            var store = provider.GetRequiredService<IUserStore<Account>>();
            // 追跡中のユーザーは基底クラスの処理を通らないため、こちらで検証していることを確かめる
            var account = new Account("taro");
            provider.GetRequiredService<AppIdentityDbContext>().Add(account);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null!, TestContext.Current.CancellationToken));
            Assert.Equal("user", exception.ParamName);
            await Assert.ThrowsAsync<OperationCanceledException>(() => store.UpdateAsync(account, new CancellationToken(canceled: true)));
            store.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => store.UpdateAsync(account, TestContext.Current.CancellationToken));
        });
    }
}

public sealed class D3RoleStoreTests : IDisposable
{
    private readonly IdentityTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task ロールの作成と更新を1回の保存で行える()
    {
        await _host.InScopeAsync(async provider =>
        {
            var role = new AccountRole("Admin");
            Assert.True((await provider.Roles().CreateAsync(role)).Succeeded);
            Assert.True((await provider.Roles().SetRoleNameAsync(role, "Administrators")).Succeeded);
            Assert.True((await provider.Roles().UpdateAsync(role)).Succeeded);

            Assert.False(await _host.InScopeAsync(other => other.Roles().RoleExistsAsync("Admin")));
            Assert.False(await _host.InScopeAsync(other => other.Roles().RoleExistsAsync("Administrators")));
            await provider.SaveAsync();
        });

        Assert.True(await _host.InScopeAsync(provider => provider.Roles().RoleExistsAsync("Administrators")));
    }

    [Fact]
    public async Task 追跡されていないロールも更新できる()
    {
        await _host.InScopeAsync(async provider =>
        {
            await provider.Roles().CreateAsync(new AccountRole("Admin"));
            await provider.SaveAsync();
        });
        var detached = await _host.InScopeAsync(provider =>
            provider.GetRequiredService<AppIdentityDbContext>().Roles.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken));

        await _host.InScopeAsync(async provider =>
        {
            detached.Name = "Administrators";
            detached.NormalizedName = "ADMINISTRATORS";
            Assert.True((await provider.Roles().UpdateAsync(detached)).Succeeded);
            await provider.SaveAsync();
        });

        Assert.True(await _host.InScopeAsync(provider => provider.Roles().RoleExistsAsync("Administrators")));
    }

    [Fact]
    public async Task UpdateAsync_引数と状態を検証する()
    {
        await _host.InScopeAsync(async provider =>
        {
            var store = provider.GetRequiredService<IRoleStore<AccountRole>>();
            var role = new AccountRole("Admin");
            provider.GetRequiredService<AppIdentityDbContext>().Add(role);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null!, TestContext.Current.CancellationToken));
            Assert.Equal("role", exception.ParamName);
            await Assert.ThrowsAsync<OperationCanceledException>(() => store.UpdateAsync(role, new CancellationToken(canceled: true)));
            store.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => store.UpdateAsync(role, TestContext.Current.CancellationToken));
        });
    }
}

public sealed class D3UserOnlyStoreTests : IDisposable
{
    private readonly IdentityTestHost _host = new(useRoles: false);

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task ユーザーの作成と更新を1回の保存で行い_保存するまでは書き込まない()
    {
        await _host.InScopeAsync(async provider =>
        {
            var account = new Account("taro");
            Assert.True((await provider.Users().CreateAsync(account, "Passw0rd!")).Succeeded);
            Assert.True((await provider.Users().UpdateSecurityStampAsync(account)).Succeeded);

            Assert.Null(await _host.InScopeAsync(other => other.Users().FindByNameAsync("taro")));
            await provider.SaveAsync();
        });

        Assert.NotNull(await _host.InScopeAsync(provider => provider.Users().FindByNameAsync("taro")));
    }

    [Fact]
    public async Task 追跡されていないユーザーも更新できる()
    {
        await _host.InScopeAsync(async provider =>
        {
            await provider.Users().CreateAsync(new Account("taro"));
            await provider.SaveAsync();
        });
        var detached = await _host.InScopeAsync(provider =>
            provider.GetRequiredService<AppUserOnlyDbContext>().Users.AsNoTracking().SingleAsync(TestContext.Current.CancellationToken));

        await _host.InScopeAsync(async provider =>
        {
            detached.PhoneNumber = "090-0000-0000";
            // UserManager.UpdateAsyncは検証で同じユーザーを別インスタンスとして読み込むため（Identityの制約）、ストアを直接呼ぶ
            Assert.True((await provider.GetRequiredService<IUserStore<Account>>().UpdateAsync(detached, TestContext.Current.CancellationToken)).Succeeded);
            await provider.SaveAsync();
        });

        Assert.Equal("090-0000-0000", (await _host.InScopeAsync(provider => provider.Users().FindByNameAsync("taro")))!.PhoneNumber);
    }

    [Fact]
    public async Task UpdateAsync_引数と状態を検証する()
    {
        await _host.InScopeAsync(async provider =>
        {
            var store = provider.GetRequiredService<IUserStore<Account>>();
            var account = new Account("taro");
            provider.GetRequiredService<AppUserOnlyDbContext>().Add(account);

            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => store.UpdateAsync(null!, TestContext.Current.CancellationToken));
            Assert.Equal("user", exception.ParamName);
            await Assert.ThrowsAsync<OperationCanceledException>(() => store.UpdateAsync(account, new CancellationToken(canceled: true)));
            store.Dispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() => store.UpdateAsync(account, TestContext.Current.CancellationToken));
        });
    }
}
