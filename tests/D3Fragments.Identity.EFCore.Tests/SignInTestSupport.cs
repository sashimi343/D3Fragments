using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace D3Fragments.Identity.EFCore.Tests;

/// <summary>
/// 標準のSignInManagerを動かすための認証サービス。スキームごとに認証結果を設定でき、サインイン・サインアウトを記録する
/// </summary>
public sealed class RecordingAuthenticationService : IAuthenticationService
{
    public Dictionary<string, ClaimsPrincipal> Principals { get; } = [];

    public List<(string Kind, string? Scheme, ClaimsPrincipal? Principal, AuthenticationProperties? Properties)> Calls { get; } = [];

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
        Task.FromResult(scheme is not null && Principals.TryGetValue(scheme, out var principal)
            ? AuthenticateResult.Success(new AuthenticationTicket(principal, scheme))
            : AuthenticateResult.NoResult());

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    {
        Calls.Add(("SignIn", scheme, principal, properties));
        if (scheme is not null)
        {
            Principals[scheme] = principal;
        }
        return Task.CompletedTask;
    }

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
    {
        Calls.Add(("SignOut", scheme, null, properties));
        if (scheme is not null)
        {
            Principals.Remove(scheme);
        }
        return Task.CompletedTask;
    }
}

/// <summary>
/// サインインのテスト用のホスト。SignInManager・既定のトークンプロバイダー・Identityの各Cookieスキームを登録する
/// </summary>
public sealed class SignInTestHost : IDisposable
{
    public const string Password = "Passw0rd!";

    private readonly IdentityTestHost _host;

    public SignInTestHost(Action<IdentityOptions>? configureIdentity = null, Action<IdentityBuilder>? configureBuilder = null)
    {
        _host = new IdentityTestHost(
            configureIdentity: options =>
            {
                options.Lockout.MaxFailedAccessAttempts = 3;
                configureIdentity?.Invoke(options);
            },
            configureBuilder: builder =>
            {
                builder.AddSignInManager().AddDefaultTokenProviders();
                configureBuilder?.Invoke(builder);
            },
            configureServices: services =>
            {
                services.AddDataProtection();
                services.AddAuthentication().AddIdentityCookies();
                services.AddSingleton(Authentication);
                services.Replace(ServiceDescriptor.Singleton<IAuthenticationService>(Authentication));
            });
    }

    public RecordingAuthenticationService Authentication { get; } = new();

    public IdentityTestHost Host => _host;

    /// <summary>
    /// ユーザーを作成して保存する。メールアドレスは確認済みにする
    /// </summary>
    public Task<Account> CreateUserAsync(string userName = "taro", Func<UserManager<Account>, Account, Task>? configure = null) =>
        _host.InScopeAsync(async provider =>
        {
            var users = provider.Users();
            var account = new Account(userName);
            Assert.True((await users.CreateAsync(account, Password)).Succeeded);
            await users.SetEmailAsync(account, $"{userName}@example.com");
            account.EmailConfirmed = true;
            if (configure is not null)
            {
                await configure(users, account);
            }
            await provider.SaveAsync();
            return account;
        });

    /// <summary>
    /// 新しいスコープで処理を行い、保存する。HttpContextは設定しない（Cookieを使わないことを確かめるため）
    /// </summary>
    public Task<T> RunAsync<T>(Func<IServiceProvider, SignInManager<Account>, Account, Task<T>> action, string userName = "taro") =>
        _host.InScopeAsync(async provider =>
        {
            var account = (await provider.Users().FindByNameAsync(userName))!;
            var result = await action(provider, provider.GetRequiredService<SignInManager<Account>>(), account);
            await provider.SaveAsync();
            return result;
        });

    /// <summary>
    /// HttpContextを設定した新しいスコープで、標準のSignInManagerを使った処理を行い、保存する
    /// </summary>
    public Task<T> RunWithHttpContextAsync<T>(Func<IServiceProvider, SignInManager<Account>, Account, Task<T>> action, string userName = "taro") =>
        _host.InScopeAsync(async provider =>
        {
            provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext { RequestServices = provider };
            var account = (await provider.Users().FindByNameAsync(userName))!;
            var result = await action(provider, provider.GetRequiredService<SignInManager<Account>>(), account);
            await provider.SaveAsync();
            return result;
        });

    /// <summary>
    /// 比較に使う、保存済みのユーザーの状態
    /// </summary>
    public Task<UserState> GetStateAsync(string userName = "taro") =>
        _host.InScopeAsync(async provider =>
        {
            var users = provider.Users();
            var account = (await users.FindByNameAsync(userName))!;
            var passkeys = await users.GetPasskeysAsync(account);
            return new UserState(
                account.AccessFailedCount,
                account.LockoutEnd is not null,
                await users.CountRecoveryCodesAsync(account),
                string.Join(",", passkeys.Select(x => $"{x.SignCount}:{x.IsBackedUp}")));
        });

    /// <summary>
    /// 二要素認証待ちの状態（標準のSignInManagerが読むCookie）を設定する
    /// </summary>
    public void SetTwoFactorPending(Account account) =>
        Authentication.Principals[IdentityConstants.TwoFactorUserIdScheme] =
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, account.Id.ToString())], IdentityConstants.TwoFactorUserIdScheme));

    /// <summary>
    /// クライアントを二要素認証について記憶済みにする（標準のSignInManagerが読むCookie）
    /// </summary>
    public void SetClientRemembered(Account account) =>
        Authentication.Principals[IdentityConstants.TwoFactorRememberMeScheme] =
            new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, account.Id.ToString())], IdentityConstants.TwoFactorRememberMeScheme));

    public void Dispose() => _host.Dispose();
}

public sealed record UserState(int AccessFailedCount, bool HasLockoutEnd, int RecoveryCodes, string Passkeys);

public static class TwoFactorSetup
{
    public static async Task EnableAsync(UserManager<Account> users, Account account)
    {
        await users.ResetAuthenticatorKeyAsync(account);
        await users.SetTwoFactorEnabledAsync(account, true);
    }

    public static async Task FailTwiceAsync(UserManager<Account> users, Account account)
    {
        await users.AccessFailedAsync(account);
        await users.AccessFailedAsync(account);
    }
}

/// <summary>
/// ロックアウトに対応していないストアを使う場合と同じ動作にするUserManager
/// </summary>
public sealed class NoLockoutUserManager(
    IUserStore<Account> store,
    Microsoft.Extensions.Options.IOptions<IdentityOptions> optionsAccessor,
    IPasswordHasher<Account> passwordHasher,
    IEnumerable<IUserValidator<Account>> userValidators,
    IEnumerable<IPasswordValidator<Account>> passwordValidators,
    ILookupNormalizer keyNormalizer,
    IdentityErrorDescriber errors,
    IServiceProvider services,
    Microsoft.Extensions.Logging.ILogger<UserManager<Account>> logger)
    : UserManager<Account>(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
{
    public override bool SupportsUserLockout => false;
}
