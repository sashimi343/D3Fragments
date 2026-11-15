using System.Security.Claims;

using D3Fragments.AspNetCore.Identity;

using Microsoft.AspNetCore.Identity;

namespace D3Fragments.Identity.EFCore.Tests;

/// <summary>
/// 標準のSignInManagerのメソッドと、拡張メソッド（DB側＋Cookie側）の組み合わせで、発行・削除するCookieと結果が一致することを確かめる
/// </summary>
public class D3SignInManagerSessionExtensionsTests
{
    private const string LoginProvider = "External";

    private delegate Task<string> Flow(SignInTestHost host, Account account, string? code);

    // ユーザーごとに変わる値（ID、ユーザー名、security stamp）は比較から除く
    private static readonly HashSet<string> UserSpecificClaimTypes =
        [ClaimTypes.NameIdentifier, ClaimTypes.Name, new ClaimsIdentityOptions().SecurityStampClaimType];

    private static string Describe(RecordingAuthenticationService authentication) =>
        string.Join(
            " | ",
            authentication.Calls.Select(call =>
            {
                var claims = (call.Principal?.Claims ?? [])
                    .Select(x => UserSpecificClaimTypes.Contains(x.Type) ? $"{x.Type}=*" : $"{x.Type}={x.Value}")
                    .Order(StringComparer.Ordinal);
                return $"{call.Kind} {call.Scheme} persistent={call.Properties?.IsPersistent} [{string.Join(", ", claims)}]";
            }));

    private static async Task<string> RunAsync(Func<UserManager<Account>, Account, Task<string?>> arrange, Flow flow)
    {
        using var host = new SignInTestHost();
        string? code = null;
        var account = await host.CreateUserAsync(configure: async (users, user) => code = await arrange(users, user));
        var result = await flow(host, account, code);
        return $"{result} => {Describe(host.Authentication)}";
    }

    private static async Task<string> AssertEquivalentAsync(Func<UserManager<Account>, Account, Task<string?>> arrange, Flow original, Flow split)
    {
        var expected = await RunAsync(arrange, original);
        var actual = await RunAsync(arrange, split);

        Assert.Equal(expected, actual);
        return actual;
    }

    private static Func<UserManager<Account>, Account, Task<string?>> Arrange(params Func<UserManager<Account>, Account, Task>[] steps) =>
        async (users, account) =>
        {
            foreach (var step in steps)
            {
                await step(users, account);
            }
            return null;
        };

    private static Task AddLogin(UserManager<Account> users, Account account) =>
        users.AddLoginAsync(account, new UserLoginInfo(LoginProvider, "key", LoginProvider));

    // DB側の処理（Command相当）とCookie側の処理（プレゼンテーション層相当）を、同じリクエストのスコープで順に行う
    private static Flow SplitPassword(bool isPersistent) =>
        (host, _, _) => host.RunWithHttpContextAsync(async (_, signInManager, user) =>
        {
            var rememberedUserId = await signInManager.GetTwoFactorClientRememberedUserIdAsync();
            var result = await signInManager.VerifyPasswordAsync(user, SignInTestHost.Password, true, rememberedUserId == user.Id.ToString());
            if (result.Succeeded)
            {
                await signInManager.CompleteSignInAsync(user, isPersistent);
            }
            else if (result.RequiresTwoFactor)
            {
                await signInManager.BeginTwoFactorSignInAsync(user);
            }
            return result.ToString();
        });

    private static Flow OriginalPassword(bool isPersistent) =>
        (host, _, _) => host.RunWithHttpContextAsync(async (_, signInManager, user) =>
            (await signInManager.PasswordSignInAsync(user, SignInTestHost.Password, isPersistent, lockoutOnFailure: true)).ToString());

    private static Flow Remembered(Flow flow) =>
        (host, account, code) =>
        {
            host.SetClientRemembered(account);
            return flow(host, account, code);
        };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task パスワードでのサインインが成功するとamr_pwdのCookieを発行する(bool isPersistent)
    {
        var actual = await AssertEquivalentAsync(Arrange(), OriginalPassword(isPersistent), SplitPassword(isPersistent));

        Assert.Contains("amr=pwd", actual, StringComparison.Ordinal);
    }

    [Fact]
    public async Task 二要素認証が必要なら二要素認証待ちのCookieだけを発行する()
    {
        var actual = await AssertEquivalentAsync(Arrange(TwoFactorSetup.EnableAsync), OriginalPassword(false), SplitPassword(false));

        Assert.StartsWith($"RequiresTwoFactor => SignIn {IdentityConstants.TwoFactorUserIdScheme} ", actual, StringComparison.Ordinal);
    }

    [Fact]
    public async Task 記憶済みのクライアントなら二要素認証なしでCookieを発行する()
    {
        var actual = await AssertEquivalentAsync(Arrange(TwoFactorSetup.EnableAsync), Remembered(OriginalPassword(false)), Remembered(SplitPassword(false)));

        Assert.StartsWith("Succeeded", actual, StringComparison.Ordinal);
    }

    // ---------------- 二要素認証の完了

    private static Flow Pending(Flow flow) =>
        (host, account, code) =>
        {
            host.SetTwoFactorPending(account);
            return flow(host, account, code);
        };

    private static async Task<string?> EnableAndGenerateRecoveryCode(UserManager<Account> users, Account account)
    {
        await TwoFactorSetup.EnableAsync(users, account);
        return (await users.GenerateNewTwoFactorRecoveryCodesAsync(account, 1))!.Single();
    }

    private static Flow OriginalTwoFactor(bool isPersistent, bool rememberClient) =>
        Pending((host, _, _) => host.RunWithHttpContextAsync(async (provider, signInManager, user) =>
        {
            var code = await provider.Users().GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            return (await signInManager.TwoFactorSignInAsync(TokenOptions.DefaultEmailProvider, code, isPersistent, rememberClient)).ToString();
        }));

    private static Flow SplitTwoFactor(bool isPersistent, bool rememberClient) =>
        Pending((host, _, _) => host.RunWithHttpContextAsync(async (provider, signInManager, _) =>
        {
            var user = (await signInManager.GetTwoFactorAuthenticationUserAsync())!;
            var code = await provider.Users().GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
            var result = await signInManager.VerifyTwoFactorCodeAsync(user, TokenOptions.DefaultEmailProvider, code);
            if (result.Succeeded)
            {
                await signInManager.CompleteTwoFactorSignInAsync(user, isPersistent, rememberClient);
            }
            return result.ToString();
        }));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task 二要素認証が成功するとamr_mfaのCookieを発行し待機のCookieを削除する(bool isPersistent, bool rememberClient)
    {
        var actual = await AssertEquivalentAsync(Arrange(TwoFactorSetup.EnableAsync), OriginalTwoFactor(isPersistent, rememberClient), SplitTwoFactor(isPersistent, rememberClient));

        Assert.Contains("amr=mfa", actual, StringComparison.Ordinal);
        Assert.Contains($"SignOut {IdentityConstants.TwoFactorUserIdScheme}", actual, StringComparison.Ordinal);
        Assert.Equal(rememberClient, actual.Contains($"SignIn {IdentityConstants.TwoFactorRememberMeScheme}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task 回復コードでの二要素認証が成功するとCookieを発行する()
    {
        var actual = await AssertEquivalentAsync(
            EnableAndGenerateRecoveryCode,
            Pending((host, _, code) => host.RunWithHttpContextAsync(async (_, signInManager, _) =>
                (await signInManager.TwoFactorRecoveryCodeSignInAsync(code!)).ToString())),
            Pending((host, _, code) => host.RunWithHttpContextAsync(async (_, signInManager, _) =>
            {
                var user = (await signInManager.GetTwoFactorAuthenticationUserAsync())!;
                var result = await signInManager.VerifyRecoveryCodeAsync(user, code!);
                await signInManager.CompleteTwoFactorSignInAsync(user, isPersistent: false, rememberClient: false);
                return result.ToString();
            })));

        Assert.StartsWith("Succeeded", actual, StringComparison.Ordinal);
    }

    // ---------------- 外部ログイン

    private static Flow OriginalExternal() =>
        (host, _, _) => host.RunWithHttpContextAsync(async (_, signInManager, _) =>
            (await signInManager.ExternalLoginSignInAsync(LoginProvider, "key", isPersistent: false)).ToString());

    private static Flow SplitExternal() =>
        (host, _, _) => host.RunWithHttpContextAsync(async (provider, signInManager, _) =>
        {
            var user = (await provider.Users().FindByLoginAsync(LoginProvider, "key"))!;
            var rememberedUserId = await signInManager.GetTwoFactorClientRememberedUserIdAsync();
            var result = await signInManager.VerifyExternalLoginAsync(user, rememberedUserId == user.Id.ToString());
            if (result.Succeeded)
            {
                await signInManager.CompleteSignInAsync(user, isPersistent: false, LoginProvider);
            }
            else if (result.RequiresTwoFactor)
            {
                await signInManager.BeginTwoFactorSignInAsync(user, LoginProvider);
            }
            return result.ToString();
        });

    [Fact]
    public async Task 外部ログインが成功すると外部ログインのCookieを削除しプロバイダーを認証方法に記録する()
    {
        var actual = await AssertEquivalentAsync(Arrange(AddLogin), OriginalExternal(), SplitExternal());

        Assert.Contains($"SignOut {IdentityConstants.ExternalScheme}", actual, StringComparison.Ordinal);
        Assert.Contains($"{ClaimTypes.AuthenticationMethod}={LoginProvider}", actual, StringComparison.Ordinal);
    }

    [Fact]
    public async Task 外部ログインで二要素認証が必要なら待機のCookieにプロバイダーを記録し_完了時に認証方法に含める()
    {
        var begin = await AssertEquivalentAsync(Arrange(AddLogin, TwoFactorSetup.EnableAsync), OriginalExternal(), SplitExternal());

        Assert.Contains($"{ClaimTypes.AuthenticationMethod}={LoginProvider}", begin, StringComparison.Ordinal);

        using var host = new SignInTestHost();
        await host.CreateUserAsync(configure: async (users, account) =>
        {
            await AddLogin(users, account);
            await TwoFactorSetup.EnableAsync(users, account);
        });
        await host.RunWithHttpContextAsync(async (_, signInManager, user) =>
        {
            await signInManager.BeginTwoFactorSignInAsync(user, LoginProvider);
            await signInManager.CompleteTwoFactorSignInAsync(user, isPersistent: false, rememberClient: false);
            return 0;
        });
        var signIn = host.Authentication.Calls.Last();
        Assert.Equal(IdentityConstants.ApplicationScheme, signIn.Scheme);
        Assert.Equal(LoginProvider, signIn.Principal!.FindFirstValue(ClaimTypes.AuthenticationMethod));
    }

    // ---------------- その他

    [Fact]
    public async Task 記憶済みでなければGetTwoFactorClientRememberedUserIdAsyncはnullを返す()
    {
        using var host = new SignInTestHost();
        var account = await host.CreateUserAsync();

        Assert.Null(await host.RunWithHttpContextAsync((_, signInManager, _) => signInManager.GetTwoFactorClientRememberedUserIdAsync()));
        host.SetClientRemembered(account);
        Assert.Equal(account.Id.ToString(), await host.RunWithHttpContextAsync((_, signInManager, _) => signInManager.GetTwoFactorClientRememberedUserIdAsync()));
    }

    [Fact]
    public async Task 二要素認証を待っているユーザーと違うユーザーでは完了できない()
    {
        using var host = new SignInTestHost();
        await host.CreateUserAsync();
        var other = await host.CreateUserAsync("jiro");
        host.SetTwoFactorPending(other);

        await host.RunWithHttpContextAsync(async (_, signInManager, user) =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => signInManager.CompleteTwoFactorSignInAsync(user, false, false));
            return 0;
        });
        Assert.Empty(host.Authentication.Calls);
    }

    [Fact]
    public async Task 二要素認証を待っているユーザーがいなければ完了できない()
    {
        using var host = new SignInTestHost();
        await host.CreateUserAsync();

        await host.RunWithHttpContextAsync(async (_, signInManager, user) =>
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => signInManager.CompleteTwoFactorSignInAsync(user, false, false));
            return 0;
        });
    }

    [Fact]
    public async Task 二要素認証のスキームが登録されていなければCookieを読み書きしない()
    {
        using var host = new SignInTestHost(registerIdentityCookies: false);
        var account = await host.CreateUserAsync();
        host.SetClientRemembered(account);
        host.SetTwoFactorPending(account);

        await host.RunWithHttpContextAsync(async (_, signInManager, user) =>
        {
            Assert.Null(await signInManager.GetTwoFactorClientRememberedUserIdAsync());
            await signInManager.BeginTwoFactorSignInAsync(user);
            await Assert.ThrowsAsync<InvalidOperationException>(() => signInManager.CompleteTwoFactorSignInAsync(user, false, false));
            return 0;
        });
        Assert.Empty(host.Authentication.Calls);
    }

    [Fact]
    public async Task 引数がnullなら例外を送出する()
    {
        using var host = new SignInTestHost();
        await host.CreateUserAsync();
        await host.RunWithHttpContextAsync(async (_, signInManager, user) =>
        {
            SignInManager<Account> none = null!;
            await Assert.ThrowsAsync<ArgumentNullException>(() => none.GetTwoFactorClientRememberedUserIdAsync());
            await Assert.ThrowsAsync<ArgumentNullException>(() => none.BeginTwoFactorSignInAsync(user));
            await Assert.ThrowsAsync<ArgumentNullException>(() => signInManager.BeginTwoFactorSignInAsync(null!));
            await Assert.ThrowsAsync<ArgumentNullException>(() => none.CompleteSignInAsync(user, false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => signInManager.CompleteSignInAsync(null!, false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => none.CompleteTwoFactorSignInAsync(user, false, false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => signInManager.CompleteTwoFactorSignInAsync(null!, false, false));
            return 0;
        });
    }
}
