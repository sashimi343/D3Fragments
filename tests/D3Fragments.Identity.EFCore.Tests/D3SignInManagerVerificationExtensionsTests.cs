using System.Globalization;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Identity;

namespace D3Fragments.Identity.EFCore.Tests;

/// <summary>
/// 同じ初期状態から標準のSignInManagerのメソッドと拡張メソッドを実行し、結果と保存された状態が一致することを確かめる
/// </summary>
public class D3SignInManagerVerificationExtensionsTests
{
    private delegate Task<SignInResult> SignIn(SignInTestHost host, Account account, string? code);

    private static async Task<(SignInResult Result, UserState State)> RunAsync(
        Func<UserManager<Account>, Account, Task<string?>> arrange,
        SignIn signIn,
        Action<IdentityOptions>? configureIdentity,
        Action<IdentityBuilder>? configureBuilder = null)
    {
        using var host = new SignInTestHost(configureIdentity, configureBuilder);
        string? code = null;
        var account = await host.CreateUserAsync(configure: async (users, user) => code = await arrange(users, user));
        var result = await signIn(host, account, code);
        return (result, await host.GetStateAsync());
    }

    private static async Task<(SignInResult Result, UserState State)> AssertEquivalentAsync(
        Func<UserManager<Account>, Account, Task<string?>> arrange,
        SignIn original,
        SignIn verification,
        Action<IdentityOptions>? configureIdentity = null,
        Action<IdentityBuilder>? configureBuilder = null)
    {
        var expected = await RunAsync(arrange, original, configureIdentity, configureBuilder);
        var actual = await RunAsync(arrange, verification, configureIdentity, configureBuilder);

        Assert.Equal(expected.Result.ToString(), actual.Result.ToString());
        Assert.Equal(expected.State, actual.State);
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

    private static Task Lock(UserManager<Account> users, Account account) => users.SetLockoutEndDateAsync(account, DateTimeOffset.MaxValue);

    private static Task Unconfirm(UserManager<Account> users, Account account)
    {
        account.EmailConfirmed = false;
        return Task.CompletedTask;
    }

    private static void RequireConfirmedEmail(IdentityOptions options) => options.SignIn.RequireConfirmedEmail = true;

    // ---------------- パスワード

    private static SignIn OriginalPassword(string password, bool lockoutOnFailure = true, bool remembered = false) =>
        (host, account, _) =>
        {
            if (remembered)
            {
                host.SetClientRemembered(account);
            }
            return host.RunWithHttpContextAsync((_, signInManager, user) => signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure));
        };

    private static SignIn VerifyPassword(string password, bool lockoutOnFailure = true, bool remembered = false) =>
        (host, _, _) => host.RunAsync((_, signInManager, user) => signInManager.VerifyPasswordAsync(user, password, lockoutOnFailure, remembered));

    [Fact]
    public async Task VerifyPasswordAsync_正しいパスワードなら成功し失敗回数をリセットする()
    {
        var (result, state) = await AssertEquivalentAsync(Arrange(TwoFactorSetup.FailTwiceAsync), OriginalPassword(SignInTestHost.Password), VerifyPassword(SignInTestHost.Password));

        Assert.True(result.Succeeded);
        Assert.Equal(0, state.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyPasswordAsync_誤ったパスワードなら失敗回数を加算する()
    {
        var (result, state) = await AssertEquivalentAsync(Arrange(), OriginalPassword("wrong"), VerifyPassword("wrong"));

        Assert.Equal(SignInResult.Failed.ToString(), result.ToString());
        Assert.Equal(1, state.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyPasswordAsync_失敗回数が上限に達するとロックアウトする()
    {
        var (result, state) = await AssertEquivalentAsync(Arrange(TwoFactorSetup.FailTwiceAsync), OriginalPassword("wrong"), VerifyPassword("wrong"));

        Assert.True(result.IsLockedOut);
        Assert.True(state.HasLockoutEnd);
    }

    [Fact]
    public async Task VerifyPasswordAsync_ロックアウトに数えない場合は失敗回数を変えない()
    {
        var (result, state) = await AssertEquivalentAsync(Arrange(TwoFactorSetup.FailTwiceAsync), OriginalPassword("wrong", lockoutOnFailure: false), VerifyPassword("wrong", lockoutOnFailure: false));

        Assert.Equal(SignInResult.Failed.ToString(), result.ToString());
        Assert.Equal(2, state.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyPasswordAsync_ロックアウト中ならパスワードを検証しない()
    {
        var (result, _) = await AssertEquivalentAsync(Arrange(Lock), OriginalPassword(SignInTestHost.Password), VerifyPassword(SignInTestHost.Password));

        Assert.True(result.IsLockedOut);
    }

    [Fact]
    public async Task VerifyPasswordAsync_サインインできないユーザーはNotAllowedになる()
    {
        var (result, _) = await AssertEquivalentAsync(Arrange(Unconfirm), OriginalPassword(SignInTestHost.Password), VerifyPassword(SignInTestHost.Password), RequireConfirmedEmail);

        Assert.True(result.IsNotAllowed);
    }

    [Fact]
    public async Task VerifyPasswordAsync_二要素認証が有効なら二要素認証を要求し失敗回数をリセットしない()
    {
        var (result, state) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.EnableAsync, TwoFactorSetup.FailTwiceAsync), OriginalPassword(SignInTestHost.Password), VerifyPassword(SignInTestHost.Password));

        Assert.True(result.RequiresTwoFactor);
        Assert.Equal(2, state.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyPasswordAsync_記憶済みのクライアントなら二要素認証を要求せず失敗回数をリセットする()
    {
        var (result, state) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.EnableAsync, TwoFactorSetup.FailTwiceAsync),
            OriginalPassword(SignInTestHost.Password, remembered: true),
            VerifyPassword(SignInTestHost.Password, remembered: true));

        Assert.True(result.Succeeded);
        Assert.Equal(0, state.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyPasswordAsync_ユーザーのロックアウトが無効なら上限に達してもロックアウトしない()
    {
        var (result, state) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.FailTwiceAsync, (users, account) => users.SetLockoutEnabledAsync(account, false)),
            OriginalPassword("wrong"),
            VerifyPassword("wrong"));

        Assert.Equal(SignInResult.Failed.ToString(), result.ToString());
        // 上限に達すると失敗回数はリセットされるが、ロックアウトは働かない
        Assert.Equal(0, state.AccessFailedCount);
    }

    private static void NoLockout(IdentityBuilder builder) => builder.AddUserManager<NoLockoutUserManager>();

    // ユーザーの更新が検証エラーで失敗する状態にする（作成後に、ユーザー名に使える文字を制限する）
    private static Task InvalidUserName(UserManager<Account> users, Account account)
    {
        users.Options.User.AllowedUserNameCharacters = "x";
        return Task.CompletedTask;
    }

    [Fact]
    public async Task VerifyPasswordAsync_ロックアウトに対応していなければ失敗回数を数えずリセットもしない()
    {
        var wrong = await AssertEquivalentAsync(Arrange(TwoFactorSetup.FailTwiceAsync), OriginalPassword("wrong"), VerifyPassword("wrong"), configureBuilder: NoLockout);
        var correct = await AssertEquivalentAsync(Arrange(TwoFactorSetup.FailTwiceAsync), OriginalPassword(SignInTestHost.Password), VerifyPassword(SignInTestHost.Password), configureBuilder: NoLockout);

        Assert.Equal(SignInResult.Failed.ToString(), wrong.Result.ToString());
        Assert.True(correct.Result.Succeeded);
        Assert.Equal(2, correct.State.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyPasswordAsync_失敗回数を更新できなければ失敗する()
    {
        var wrong = await AssertEquivalentAsync(Arrange(TwoFactorSetup.FailTwiceAsync, InvalidUserName), OriginalPassword("wrong"), VerifyPassword("wrong"));
        var correct = await AssertEquivalentAsync(Arrange(TwoFactorSetup.FailTwiceAsync, InvalidUserName), OriginalPassword(SignInTestHost.Password), VerifyPassword(SignInTestHost.Password));

        Assert.Equal(SignInResult.Failed.ToString(), wrong.Result.ToString());
        Assert.Equal(SignInResult.Failed.ToString(), correct.Result.ToString());
    }

    // ---------------- 二要素認証のコード

    private static SignIn OriginalTwoFactor(string provider, Func<UserManager<Account>, Account, Task<string>> code) =>
        (host, account, _) =>
        {
            host.SetTwoFactorPending(account);
            return host.RunWithHttpContextAsync(async (provider2, signInManager, user) =>
                await signInManager.TwoFactorSignInAsync(provider, await code(provider2.Users(), user), isPersistent: false, rememberClient: false));
        };

    private static SignIn VerifyTwoFactor(string provider, Func<UserManager<Account>, Account, Task<string>> code) =>
        (host, _, _) => host.RunAsync(async (provider2, signInManager, user) =>
            await signInManager.VerifyTwoFactorCodeAsync(user, provider, await code(provider2.Users(), user)));

    private static Task<string> EmailCode(UserManager<Account> users, Account account) =>
        users.GenerateTwoFactorTokenAsync(account, TokenOptions.DefaultEmailProvider);

    private static async Task<string> AuthenticatorCode(UserManager<Account> users, Account account) =>
        Totp.Compute((await users.GetAuthenticatorKeyAsync(account))!);

    private static Task<string> WrongCode(UserManager<Account> users, Account account) => Task.FromResult("000000x");

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_正しいコードなら成功し失敗回数をリセットする()
    {
        var (result, state) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.EnableAsync, TwoFactorSetup.FailTwiceAsync),
            OriginalTwoFactor(TokenOptions.DefaultEmailProvider, EmailCode),
            VerifyTwoFactor(TokenOptions.DefaultEmailProvider, EmailCode));

        Assert.True(result.Succeeded);
        Assert.Equal(0, state.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_認証アプリのコードを検証できる()
    {
        var (result, _) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.EnableAsync),
            OriginalTwoFactor(TokenOptions.DefaultAuthenticatorProvider, AuthenticatorCode),
            VerifyTwoFactor(TokenOptions.DefaultAuthenticatorProvider, AuthenticatorCode));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_誤ったコードなら失敗回数を加算する()
    {
        var (result, state) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.EnableAsync),
            OriginalTwoFactor(TokenOptions.DefaultEmailProvider, WrongCode),
            VerifyTwoFactor(TokenOptions.DefaultEmailProvider, WrongCode));

        Assert.Equal(SignInResult.Failed.ToString(), result.ToString());
        Assert.Equal(1, state.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_失敗回数が上限に達するとロックアウトする()
    {
        var (result, _) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.EnableAsync, TwoFactorSetup.FailTwiceAsync),
            OriginalTwoFactor(TokenOptions.DefaultEmailProvider, WrongCode),
            VerifyTwoFactor(TokenOptions.DefaultEmailProvider, WrongCode));

        Assert.True(result.IsLockedOut);
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_ロックアウト中ならコードを検証しない()
    {
        var (result, _) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.EnableAsync, Lock),
            OriginalTwoFactor(TokenOptions.DefaultEmailProvider, EmailCode),
            VerifyTwoFactor(TokenOptions.DefaultEmailProvider, EmailCode));

        Assert.True(result.IsLockedOut);
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_サインインできないユーザーはNotAllowedになる()
    {
        var (result, _) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.EnableAsync, Unconfirm),
            OriginalTwoFactor(TokenOptions.DefaultEmailProvider, WrongCode),
            VerifyTwoFactor(TokenOptions.DefaultEmailProvider, WrongCode),
            RequireConfirmedEmail);

        Assert.True(result.IsNotAllowed);
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_ユーザーのロックアウトが無効なら上限に達してもロックアウトしない()
    {
        var (result, state) = await AssertEquivalentAsync(
            Arrange(TwoFactorSetup.EnableAsync, TwoFactorSetup.FailTwiceAsync, (users, account) => users.SetLockoutEnabledAsync(account, false)),
            OriginalTwoFactor(TokenOptions.DefaultEmailProvider, WrongCode),
            VerifyTwoFactor(TokenOptions.DefaultEmailProvider, WrongCode));

        Assert.Equal(SignInResult.Failed.ToString(), result.ToString());
        // 上限に達すると失敗回数はリセットされるが、ロックアウトは働かない
        Assert.Equal(0, state.AccessFailedCount);
    }

    // ---------------- 回復コード

    private static async Task<string?> GenerateRecoveryCodes(UserManager<Account> users, Account account)
    {
        await TwoFactorSetup.EnableAsync(users, account);
        await TwoFactorSetup.FailTwiceAsync(users, account);
        return (await users.GenerateNewTwoFactorRecoveryCodesAsync(account, 3))!.First();
    }

    private static Func<UserManager<Account>, Account, Task<string?>> GenerateRecoveryCodesAndLock =>
        async (users, account) =>
        {
            var code = await GenerateRecoveryCodes(users, account);
            await Lock(users, account);
            return code;
        };

    private static SignIn OriginalRecovery(string? overrideCode = null) =>
        (host, account, code) =>
        {
            host.SetTwoFactorPending(account);
            return host.RunWithHttpContextAsync((_, signInManager, _) => signInManager.TwoFactorRecoveryCodeSignInAsync(overrideCode ?? code!));
        };

    private static SignIn VerifyRecovery(string? overrideCode = null) =>
        (host, _, code) => host.RunAsync((_, signInManager, user) => signInManager.VerifyRecoveryCodeAsync(user, overrideCode ?? code!));

    [Fact]
    public async Task VerifyRecoveryCodeAsync_正しいコードなら成功しコードを使用済みにして失敗回数をリセットする()
    {
        var (result, state) = await AssertEquivalentAsync(GenerateRecoveryCodes, OriginalRecovery(), VerifyRecovery());

        Assert.True(result.Succeeded);
        Assert.Equal(2, state.RecoveryCodes);
        Assert.Equal(0, state.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyRecoveryCodeAsync_誤ったコードなら失敗し失敗回数を数えない()
    {
        var (result, state) = await AssertEquivalentAsync(GenerateRecoveryCodes, OriginalRecovery("wrong"), VerifyRecovery("wrong"));

        Assert.Equal(SignInResult.Failed.ToString(), result.ToString());
        Assert.Equal(3, state.RecoveryCodes);
        Assert.Equal(2, state.AccessFailedCount);
    }

    [Fact]
    public async Task VerifyRecoveryCodeAsync_Identityと同じくロックアウト中でも確認しない()
    {
        var (result, _) = await AssertEquivalentAsync(GenerateRecoveryCodesAndLock, OriginalRecovery(), VerifyRecovery());

        Assert.True(result.Succeeded);
    }

    // ---------------- 外部ログイン

    private const string LoginProvider = "External";

    private static Task AddLogin(UserManager<Account> users, Account account) =>
        users.AddLoginAsync(account, new UserLoginInfo(LoginProvider, "key", LoginProvider));

    private static SignIn OriginalExternal(bool remembered = false, bool bypassTwoFactor = false) =>
        (host, account, _) =>
        {
            if (remembered)
            {
                host.SetClientRemembered(account);
            }
            return host.RunWithHttpContextAsync((_, signInManager, _) => signInManager.ExternalLoginSignInAsync(LoginProvider, "key", isPersistent: false, bypassTwoFactor));
        };

    private static SignIn VerifyExternal(bool remembered = false, bool bypassTwoFactor = false) =>
        (host, _, _) => host.RunAsync(async (provider, signInManager, _) =>
            await signInManager.VerifyExternalLoginAsync((await provider.Users().FindByLoginAsync(LoginProvider, "key"))!, remembered, bypassTwoFactor));

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public async Task VerifyExternalLoginAsync_二要素認証の要否をIdentityと同じく判定する(bool twoFactor, bool remembered, bool bypassTwoFactor)
    {
        var (result, _) = await AssertEquivalentAsync(
            Arrange(AddLogin, twoFactor ? TwoFactorSetup.EnableAsync : (_, _) => Task.CompletedTask),
            OriginalExternal(remembered, bypassTwoFactor),
            VerifyExternal(remembered, bypassTwoFactor));

        Assert.Equal(twoFactor && !remembered && !bypassTwoFactor, result.RequiresTwoFactor);
    }

    [Fact]
    public async Task VerifyExternalLoginAsync_ロックアウト中ならLockedOutになる()
    {
        var (result, _) = await AssertEquivalentAsync(Arrange(AddLogin, Lock), OriginalExternal(), VerifyExternal());

        Assert.True(result.IsLockedOut);
    }

    // ---------------- パスキー（標準のメソッドは実際の認証器の署名が必要なため、拡張メソッドだけを確かめる）

    private static readonly byte[] CredentialId = RandomNumberGenerator.GetBytes(16);

    private static async Task<string?> AddPasskey(UserManager<Account> users, Account account)
    {
        var passkey = new UserPasskeyInfo(CredentialId, RandomNumberGenerator.GetBytes(64), DateTimeOffset.UtcNow, 1, null, true, true, false, [1], [1]);
        Assert.True((await users.AddOrUpdatePasskeyAsync(account, passkey)).Succeeded);
        return null;
    }

    private static Task<(SignInResult Result, UserState State)> RunPasskeyAsync(
        Func<UserManager<Account>, Account, Task<string?>> arrange, byte[] credentialId, Action<IdentityOptions>? configureIdentity = null) =>
        RunAsync(arrange, (host, _, _) => host.RunAsync((_, signInManager, user) => signInManager.VerifyPasskeyAsync(user, credentialId, 5, isBackedUp: true)), configureIdentity);

    [Fact]
    public async Task VerifyPasskeyAsync_保存済みのパスキーなら成功し署名回数とバックアップ状態を更新する()
    {
        var (result, state) = await RunPasskeyAsync(AddPasskey, CredentialId);

        Assert.True(result.Succeeded);
        Assert.Equal("5:True", state.Passkeys);
    }

    [Fact]
    public async Task VerifyPasskeyAsync_二要素認証が有効でも要求しない()
    {
        var (result, _) = await RunPasskeyAsync(
            async (users, account) =>
            {
                await TwoFactorSetup.EnableAsync(users, account);
                return await AddPasskey(users, account);
            },
            CredentialId);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task VerifyPasskeyAsync_ユーザーのパスキーでなければ失敗する()
    {
        var (result, state) = await RunPasskeyAsync(AddPasskey, RandomNumberGenerator.GetBytes(16));

        Assert.Equal(SignInResult.Failed.ToString(), result.ToString());
        Assert.Equal("1:False", state.Passkeys);
    }

    [Fact]
    public async Task VerifyPasskeyAsync_ロックアウト中なら更新しない()
    {
        var (result, state) = await RunPasskeyAsync(
            async (users, account) =>
            {
                await Lock(users, account);
                return await AddPasskey(users, account);
            },
            CredentialId);

        Assert.True(result.IsLockedOut);
        Assert.Equal("1:False", state.Passkeys);
    }

    [Fact]
    public async Task VerifyPasskeyAsync_サインインできないユーザーはNotAllowedになる()
    {
        var (result, _) = await RunPasskeyAsync(
            async (users, account) =>
            {
                await Unconfirm(users, account);
                return await AddPasskey(users, account);
            },
            CredentialId,
            RequireConfirmedEmail);

        Assert.True(result.IsNotAllowed);
    }

    // ---------------- 引数

    [Fact]
    public async Task 引数がnullなら例外を送出する()
    {
        using var host = new SignInTestHost();
        await host.CreateUserAsync();
        await host.RunAsync(async (_, signInManager, user) =>
        {
            SignInManager<Account> none = null!;
            await Assert.ThrowsAsync<ArgumentNullException>(() => none.VerifyPasswordAsync(user, "x", true, false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => signInManager.VerifyPasswordAsync(null!, "x", true, false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => none.VerifyTwoFactorCodeAsync(user, "p", "x"));
            await Assert.ThrowsAsync<ArgumentNullException>(() => signInManager.VerifyTwoFactorCodeAsync(null!, "p", "x"));
            await Assert.ThrowsAsync<ArgumentNullException>(() => none.VerifyRecoveryCodeAsync(user, "x"));
            await Assert.ThrowsAsync<ArgumentNullException>(() => signInManager.VerifyRecoveryCodeAsync(null!, "x"));
            await Assert.ThrowsAsync<ArgumentNullException>(() => none.VerifyPasskeyAsync(user, [], 0, false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => signInManager.VerifyPasskeyAsync(null!, [], 0, false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => signInManager.VerifyPasskeyAsync(user, null!, 0, false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => none.VerifyExternalLoginAsync(user, false));
            await Assert.ThrowsAsync<ArgumentNullException>(() => signInManager.VerifyExternalLoginAsync(null!, false));
            return 0;
        });
    }
}

/// <summary>
/// AppContextのスイッチはプロセス全体に効くため、他のテストと並行して実行しない
/// </summary>
[CollectionDefinition(DisableParallelization = true)]
public sealed class AppContextSwitchCollection;

[Collection(typeof(AppContextSwitchCollection))]
public class D3SignInManagerVerificationAppContextSwitchTests
{
    private const string Switch = "Microsoft.AspNetCore.Identity.CheckPasswordSignInAlwaysResetLockoutOnSuccess";

    private static async Task<(string Result, int AccessFailedCount)> RunAsync(bool original)
    {
        using var host = new SignInTestHost();
        await host.CreateUserAsync(configure: async (users, account) =>
        {
            await TwoFactorSetup.EnableAsync(users, account);
            await TwoFactorSetup.FailTwiceAsync(users, account);
        });
        var result = original
            ? await host.RunWithHttpContextAsync((_, signInManager, user) => signInManager.PasswordSignInAsync(user, SignInTestHost.Password, false, true))
            : await host.RunAsync((_, signInManager, user) => signInManager.VerifyPasswordAsync(user, SignInTestHost.Password, true, false));
        return (result.ToString(), (await host.GetStateAsync()).AccessFailedCount);
    }

    [Fact]
    public async Task スイッチを明示的に無効にした場合は二要素認証が必要なら失敗回数をリセットしない()
    {
        AppContext.SetSwitch(Switch, false);

        var expected = await RunAsync(original: true);
        var actual = await RunAsync(original: false);

        Assert.Equal(expected, actual);
        Assert.Equal(2, actual.AccessFailedCount);
    }

    [Fact]
    public async Task スイッチが有効なら二要素認証が必要でも失敗回数をリセットする()
    {
        AppContext.SetSwitch(Switch, true);
        try
        {
            var expected = await RunAsync(original: true);
            var actual = await RunAsync(original: false);

            Assert.Equal(expected, actual);
            Assert.Equal(0, actual.AccessFailedCount);
        }
        finally
        {
            AppContext.SetSwitch(Switch, false);
        }
    }
}

internal static class Totp
{
    // 認証アプリと同じ方法（RFC 6238、SHA-1、30秒、6桁）でコードを計算する
    public static string Compute(string base32Key)
    {
        const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = string.Concat(base32Key.ToUpperInvariant().Where(Alphabet.Contains).Select(c => Convert.ToString(Alphabet.IndexOf(c, StringComparison.Ordinal), 2).PadLeft(5, '0')));
        var key = Enumerable.Range(0, bits.Length / 8).Select(i => Convert.ToByte(bits.Substring(i * 8, 8), 2)).ToArray();
        var counter = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counter);
        }
#pragma warning disable CA5350 // TOTP（RFC 6238）と認証アプリの既定がSHA-1のため
        var hash = HMACSHA1.HashData(key, counter);
#pragma warning restore CA5350
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }
}
