using Microsoft.AspNetCore.Identity;

namespace D3Fragments.Identity.EFCore;

/// <summary>
///     <para xml:lang="en">Extension methods that perform the database side of the <see cref="SignInManager{TUser}"/> sign-in methods, without touching cookies. Call them inside a command (through the infrastructure implementation of an interface that the application defines), so that the changes they make (the access failed count, the lockout, redeemed recovery codes and passkey data) are saved by the unit of work together with the domain changes; then issue the cookie in the presentation layer after the command succeeds.</para>
///     <para xml:lang="ja"><see cref="SignInManager{TUser}"/>のサインイン系メソッドのうち、Cookieを操作しないデータベース側の処理を行う拡張メソッド。Commandの中で（アプリケーションが定義したインターフェースのインフラストラクチャ層の実装から）呼び出し、ログイン失敗回数・ロックアウト・使用した回復コード・パスキー情報の変更を、ドメインの変更とまとめてUnit of Workに保存させる。Cookieは、Commandの成功後にプレゼンテーション層で発行する。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Each method follows the corresponding <see cref="SignInManager{TUser}"/> method step by step and returns the same <see cref="SignInResult"/>. The methods only change the tracked entities; use stores that leave saving to the unit of work (<see cref="D3FragmentsIdentityBuilderExtensions.AddD3FragmentsStores{TContext}"/>). Return a failed sign-in as a result, not as an exception: the unit of work discards the changes when the command throws, and then the access failed count is not saved and lockout does not work.</para>
///     <para xml:lang="ja">各メソッドは、対応する<see cref="SignInManager{TUser}"/>のメソッドと同じ手順で処理し、同じ<see cref="SignInResult"/>を返す。追跡中のエンティティを変更するだけのため、保存をUnit of Workに任せるストア（<see cref="D3FragmentsIdentityBuilderExtensions.AddD3FragmentsStores{TContext}"/>）と組み合わせて使う。サインインの失敗は例外ではなく結果として返すこと。Commandが例外を送出するとUnit of Workは変更を破棄するため、ログイン失敗回数が保存されず、ロックアウトが働かない。</para>
///     <para xml:lang="en">The public virtual members of <see cref="SignInManager{TUser}"/> (<see cref="SignInManager{TUser}.CanSignInAsync"/>, <see cref="SignInManager{TUser}.IsTwoFactorEnabledAsync"/>) and of <see cref="SignInManager{TUser}.UserManager"/> are used, so overriding them in a derived class takes effect. The protected members (<c>PreSignInCheck</c>, <c>IsLockedOut</c>, <c>LockedOut</c>, <c>ResetLockout</c>) cannot be called from outside, so the same steps are performed here and overriding them has no effect. Metrics and logs of <see cref="SignInManager{TUser}"/> are not recorded.</para>
///     <para xml:lang="ja"><see cref="SignInManager{TUser}"/>のpublic virtualなメンバー（<see cref="SignInManager{TUser}.CanSignInAsync"/>、<see cref="SignInManager{TUser}.IsTwoFactorEnabledAsync"/>）と<see cref="SignInManager{TUser}.UserManager"/>を使うため、派生クラスでのオーバーライドは反映される。protectedなメンバー（<c>PreSignInCheck</c>、<c>IsLockedOut</c>、<c>LockedOut</c>、<c>ResetLockout</c>）は外部から呼び出せないため同じ手順をここで行い、それらのオーバーライドは反映されない。<see cref="SignInManager{TUser}"/>のメトリクスとログは記録しない。</para>
/// </remarks>
public static class D3SignInManagerVerificationExtensions
{
    private const string AlwaysResetLockoutOnSuccessSwitch = "Microsoft.AspNetCore.Identity.CheckPasswordSignInAlwaysResetLockoutOnSuccess";

    /// <summary>
    ///     <para xml:lang="en">Verifies the password of the user, records the access failure, and decides whether two-factor authentication is required. This is the database side of <see cref="SignInManager{TUser}.PasswordSignInAsync(TUser, string, bool, bool)"/>.</para>
    ///     <para xml:lang="ja">ユーザーのパスワードを検証し、失敗を記録して、二要素認証が必要かを判定する。<see cref="SignInManager{TUser}.PasswordSignInAsync(TUser, string, bool, bool)"/>のデータベース側の処理。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Returns <see cref="SignInResult.NotAllowed"/> or <see cref="SignInResult.LockedOut"/> when the user cannot sign in, <see cref="SignInResult.Failed"/> or <see cref="SignInResult.LockedOut"/> for a wrong password, <see cref="SignInResult.TwoFactorRequired"/> when two-factor authentication is enabled and the client is not remembered, and <see cref="SignInResult.Success"/> otherwise. As with <see cref="SignInManager{TUser}.CheckPasswordSignInAsync"/>, the access failed count is not reset when two-factor authentication is still required.</para>
    ///     <para xml:lang="ja">サインインできない場合は<see cref="SignInResult.NotAllowed"/>または<see cref="SignInResult.LockedOut"/>、パスワードが誤っている場合は<see cref="SignInResult.Failed"/>または<see cref="SignInResult.LockedOut"/>、二要素認証が有効でクライアントが記憶されていない場合は<see cref="SignInResult.TwoFactorRequired"/>、それ以外は<see cref="SignInResult.Success"/>を返す。<see cref="SignInManager{TUser}.CheckPasswordSignInAsync"/>と同じく、二要素認証がまだ必要な場合はログイン失敗回数をリセットしない。</para>
    /// </remarks>
    /// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
    /// <param name="signInManager"><para xml:lang="en">The sign-in manager.</para><para xml:lang="ja">サインインマネージャー。</para></param>
    /// <param name="user"><para xml:lang="en">The user to sign in.</para><para xml:lang="ja">サインインするユーザー。</para></param>
    /// <param name="password"><para xml:lang="en">The password to verify.</para><para xml:lang="ja">検証するパスワード。</para></param>
    /// <param name="lockoutOnFailure"><para xml:lang="en">Whether a wrong password counts toward lockout.</para><para xml:lang="ja">パスワードの誤りをロックアウトの回数に数えるかどうか。</para></param>
    /// <param name="isTwoFactorClientRemembered">
    ///     <para xml:lang="en">Whether the client is remembered for two-factor authentication. Obtain it in the presentation layer (for example, by <c>GetTwoFactorClientRememberedUserIdAsync</c> of <c>D3Fragments.AspNetCore</c>) and compare it with the user.</para>
    ///     <para xml:lang="ja">クライアントが二要素認証について記憶されているかどうか。プレゼンテーション層で取得し（<c>D3Fragments.AspNetCore</c>の<c>GetTwoFactorClientRememberedUserIdAsync</c>など）、ユーザーと照合した結果を渡す。</para>
    /// </param>
    /// <returns><para xml:lang="en">The result of the sign-in attempt.</para><para xml:lang="ja">サインインの試行結果。</para></returns>
    /// <exception cref="ArgumentNullException"><para xml:lang="en">Thrown when <paramref name="signInManager"/> or <paramref name="user"/> is <see langword="null"/>.</para><para xml:lang="ja"><paramref name="signInManager"/>または<paramref name="user"/>が<see langword="null"/>の場合にスローされる。</para></exception>
    public static async Task<SignInResult> VerifyPasswordAsync<TUser>(
        this SignInManager<TUser> signInManager,
        TUser user,
        string password,
        bool lockoutOnFailure,
        bool isTwoFactorClientRemembered)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(user);

        if (await PreSignInCheckAsync(signInManager, user).ConfigureAwait(false) is { } error)
        {
            return error;
        }

        var userManager = signInManager.UserManager;
        if (await userManager.CheckPasswordAsync(user, password).ConfigureAwait(false))
        {
            var alwaysResetLockout = AppContext.TryGetSwitch(AlwaysResetLockoutOnSuccessSwitch, out var enabled) && enabled;
            if (alwaysResetLockout
                || !await signInManager.IsTwoFactorEnabledAsync(user).ConfigureAwait(false)
                || isTwoFactorClientRemembered)
            {
                if (!(await ResetLockoutAsync(userManager, user).ConfigureAwait(false)).Succeeded)
                {
                    return SignInResult.Failed;
                }
            }

            // PasswordSignInAsync が続けて行う SignInOrTwoFactorAsync の判定
            return !isTwoFactorClientRemembered && await signInManager.IsTwoFactorEnabledAsync(user).ConfigureAwait(false)
                ? SignInResult.TwoFactorRequired
                : SignInResult.Success;
        }

        return lockoutOnFailure ? await RecordAccessFailedAsync(userManager, user).ConfigureAwait(false) : SignInResult.Failed;
    }

    /// <summary>
    ///     <para xml:lang="en">Verifies a two-factor authentication code issued by the specified token provider (for example, an authenticator app, an email, or a provider the application registers), and records the access failure. This is the database side of <see cref="SignInManager{TUser}.TwoFactorSignInAsync"/> and <see cref="SignInManager{TUser}.TwoFactorAuthenticatorSignInAsync"/>.</para>
    ///     <para xml:lang="ja">指定したトークンプロバイダー（認証アプリ、メール、アプリケーションが登録したプロバイダーなど）の二要素認証コードを検証し、失敗を記録する。<see cref="SignInManager{TUser}.TwoFactorSignInAsync"/>と<see cref="SignInManager{TUser}.TwoFactorAuthenticatorSignInAsync"/>のデータベース側の処理。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Whether the user can sign in is checked before the code is verified. A wrong code counts toward lockout. On success, the access failed count is reset.</para>
    ///     <para xml:lang="ja">コードの検証前に、サインインできるかを確認する。コードの誤りはロックアウトの回数に数える。成功した場合はログイン失敗回数をリセットする。</para>
    /// </remarks>
    /// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
    /// <param name="signInManager"><para xml:lang="en">The sign-in manager.</para><para xml:lang="ja">サインインマネージャー。</para></param>
    /// <param name="user"><para xml:lang="en">The user waiting for two-factor authentication.</para><para xml:lang="ja">二要素認証を待っているユーザー。</para></param>
    /// <param name="tokenProvider">
    ///     <para xml:lang="en">The name of the token provider, such as <see cref="TokenOptions.AuthenticatorTokenProvider"/> of <c>UserManager.Options.Tokens</c>.</para>
    ///     <para xml:lang="ja"><c>UserManager.Options.Tokens</c>の<see cref="TokenOptions.AuthenticatorTokenProvider"/>などの、トークンプロバイダーの名前。</para>
    /// </param>
    /// <param name="code"><para xml:lang="en">The code to verify.</para><para xml:lang="ja">検証するコード。</para></param>
    /// <returns><para xml:lang="en">The result of the sign-in attempt.</para><para xml:lang="ja">サインインの試行結果。</para></returns>
    /// <exception cref="ArgumentNullException"><para xml:lang="en">Thrown when <paramref name="signInManager"/> or <paramref name="user"/> is <see langword="null"/>.</para><para xml:lang="ja"><paramref name="signInManager"/>または<paramref name="user"/>が<see langword="null"/>の場合にスローされる。</para></exception>
    public static async Task<SignInResult> VerifyTwoFactorCodeAsync<TUser>(
        this SignInManager<TUser> signInManager,
        TUser user,
        string tokenProvider,
        string code)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(user);

        if (await PreSignInCheckAsync(signInManager, user).ConfigureAwait(false) is { } error)
        {
            return error;
        }

        var userManager = signInManager.UserManager;
        if (await userManager.VerifyTwoFactorTokenAsync(user, tokenProvider, code).ConfigureAwait(false))
        {
            return await CompleteTwoFactorAsync(userManager, user).ConfigureAwait(false);
        }

        return await RecordAccessFailedAsync(userManager, user).ConfigureAwait(false);
    }

    /// <summary>
    ///     <para xml:lang="en">Redeems a two-factor recovery code of the user. This is the database side of <see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/>.</para>
    ///     <para xml:lang="ja">ユーザーの二要素認証の回復コードを使用する。<see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/>のデータベース側の処理。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">As with <see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/>, whether the user can sign in or is locked out is not checked, and a wrong code does not count toward lockout. Check them in the command when the application requires it. On success, the access failed count is reset.</para>
    ///     <para xml:lang="ja"><see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/>と同じく、サインインできるか・ロックアウト中かは確認せず、コードの誤りもロックアウトの回数に数えない。アプリケーションが必要とする場合はCommandで確認すること。成功した場合はログイン失敗回数をリセットする。</para>
    /// </remarks>
    /// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
    /// <param name="signInManager"><para xml:lang="en">The sign-in manager.</para><para xml:lang="ja">サインインマネージャー。</para></param>
    /// <param name="user"><para xml:lang="en">The user waiting for two-factor authentication.</para><para xml:lang="ja">二要素認証を待っているユーザー。</para></param>
    /// <param name="recoveryCode"><para xml:lang="en">The recovery code to redeem.</para><para xml:lang="ja">使用する回復コード。</para></param>
    /// <returns><para xml:lang="en">The result of the sign-in attempt.</para><para xml:lang="ja">サインインの試行結果。</para></returns>
    /// <exception cref="ArgumentNullException"><para xml:lang="en">Thrown when <paramref name="signInManager"/> or <paramref name="user"/> is <see langword="null"/>.</para><para xml:lang="ja"><paramref name="signInManager"/>または<paramref name="user"/>が<see langword="null"/>の場合にスローされる。</para></exception>
    public static async Task<SignInResult> VerifyRecoveryCodeAsync<TUser>(
        this SignInManager<TUser> signInManager,
        TUser user,
        string recoveryCode)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(user);

        var userManager = signInManager.UserManager;
        return (await userManager.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode).ConfigureAwait(false)).Succeeded
            ? await CompleteTwoFactorAsync(userManager, user).ConfigureAwait(false)
            : SignInResult.Failed;
    }

    /// <summary>
    ///     <para xml:lang="en">Records a verified passkey assertion: checks whether the user can sign in and stores the new sign count and backup state of the passkey. This is the database side of <see cref="SignInManager{TUser}.PasskeySignInAsync"/>; verify the assertion itself in the presentation layer with <see cref="SignInManager{TUser}.PerformPasskeyAssertionAsync"/>, and pass the values of <see cref="PasskeyAssertionResult{TUser}.Passkey"/>.</para>
    ///     <para xml:lang="ja">検証済みのパスキーのアサーションを記録する。サインインできるかを確認し、パスキーの新しい署名回数とバックアップ状態を保存対象にする。<see cref="SignInManager{TUser}.PasskeySignInAsync"/>のデータベース側の処理。アサーション自体はプレゼンテーション層で<see cref="SignInManager{TUser}.PerformPasskeyAssertionAsync"/>により検証し、<see cref="PasskeyAssertionResult{TUser}.Passkey"/>の値を渡す。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">Returns <see cref="SignInResult.Failed"/> when the user has no passkey with <paramref name="credentialId"/>. As with <see cref="SignInManager{TUser}.PasskeySignInAsync"/>, two-factor authentication is not required (a passkey is treated as strong enough) and the access failed count is not changed. Require two-factor authentication in the command when the application needs it.</para>
    ///     <para xml:lang="ja">ユーザーが<paramref name="credentialId"/>のパスキーを持たない場合は<see cref="SignInResult.Failed"/>を返す。<see cref="SignInManager{TUser}.PasskeySignInAsync"/>と同じく、二要素認証は要求せず（パスキーを十分に強い認証とみなす）、ログイン失敗回数も変更しない。アプリケーションが二要素認証を必要とする場合はCommandで要求すること。</para>
    /// </remarks>
    /// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
    /// <param name="signInManager"><para xml:lang="en">The sign-in manager.</para><para xml:lang="ja">サインインマネージャー。</para></param>
    /// <param name="user"><para xml:lang="en">The user of the assertion.</para><para xml:lang="ja">アサーションのユーザー。</para></param>
    /// <param name="credentialId"><para xml:lang="en">The credential ID of the passkey.</para><para xml:lang="ja">パスキーの認証情報ID。</para></param>
    /// <param name="signCount"><para xml:lang="en">The sign count of the verified assertion.</para><para xml:lang="ja">検証したアサーションの署名回数。</para></param>
    /// <param name="isBackedUp"><para xml:lang="en">Whether the passkey is backed up, according to the verified assertion.</para><para xml:lang="ja">検証したアサーションによる、パスキーがバックアップされているかどうか。</para></param>
    /// <returns><para xml:lang="en">The result of the sign-in attempt.</para><para xml:lang="ja">サインインの試行結果。</para></returns>
    /// <exception cref="ArgumentNullException"><para xml:lang="en">Thrown when <paramref name="signInManager"/>, <paramref name="user"/> or <paramref name="credentialId"/> is <see langword="null"/>.</para><para xml:lang="ja"><paramref name="signInManager"/>、<paramref name="user"/>または<paramref name="credentialId"/>が<see langword="null"/>の場合にスローされる。</para></exception>
    public static async Task<SignInResult> VerifyPasskeyAsync<TUser>(
        this SignInManager<TUser> signInManager,
        TUser user,
        byte[] credentialId,
        uint signCount,
        bool isBackedUp)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(credentialId);

        if (await PreSignInCheckAsync(signInManager, user).ConfigureAwait(false) is { } error)
        {
            return error;
        }

        // パスキーのハンドラーは保存済みのパスキーにこの2つの値を反映して返すため、同じ値を反映して保存する
        var userManager = signInManager.UserManager;
        if (await userManager.GetPasskeyAsync(user, credentialId).ConfigureAwait(false) is not { } passkey)
        {
            return SignInResult.Failed;
        }

        passkey.SignCount = signCount;
        passkey.IsBackedUp = isBackedUp;
        return (await userManager.AddOrUpdatePasskeyAsync(user, passkey).ConfigureAwait(false)).Succeeded
            ? SignInResult.Success
            : SignInResult.Failed;
    }

    /// <summary>
    ///     <para xml:lang="en">Checks whether the user found by an external login can sign in, and decides whether two-factor authentication is required. This is the database side of <see cref="SignInManager{TUser}.ExternalLoginSignInAsync(string, string, bool, bool)"/>; find the user with <see cref="UserManager{TUser}.FindByLoginAsync"/>.</para>
    ///     <para xml:lang="ja">外部ログインで見つけたユーザーがサインインできるかを確認し、二要素認証が必要かを判定する。<see cref="SignInManager{TUser}.ExternalLoginSignInAsync(string, string, bool, bool)"/>のデータベース側の処理。ユーザーは<see cref="UserManager{TUser}.FindByLoginAsync"/>で探す。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">To store the authentication tokens of the external provider, call <see cref="SignInManager{TUser}.UpdateExternalAuthenticationTokensAsync"/> in the same command; it only changes the database.</para>
    ///     <para xml:lang="ja">外部プロバイダーの認証トークンを保存する場合は、同じCommandで<see cref="SignInManager{TUser}.UpdateExternalAuthenticationTokensAsync"/>を呼び出す（データベースの変更のみを行う）。</para>
    /// </remarks>
    /// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
    /// <param name="signInManager"><para xml:lang="en">The sign-in manager.</para><para xml:lang="ja">サインインマネージャー。</para></param>
    /// <param name="user"><para xml:lang="en">The user associated with the external login.</para><para xml:lang="ja">外部ログインに関連付けられたユーザー。</para></param>
    /// <param name="isTwoFactorClientRemembered"><para xml:lang="en">Whether the client is remembered for two-factor authentication.</para><para xml:lang="ja">クライアントが二要素認証について記憶されているかどうか。</para></param>
    /// <param name="bypassTwoFactor"><para xml:lang="en">Whether to skip two-factor authentication.</para><para xml:lang="ja">二要素認証を省略するかどうか。</para></param>
    /// <returns><para xml:lang="en">The result of the sign-in attempt.</para><para xml:lang="ja">サインインの試行結果。</para></returns>
    /// <exception cref="ArgumentNullException"><para xml:lang="en">Thrown when <paramref name="signInManager"/> or <paramref name="user"/> is <see langword="null"/>.</para><para xml:lang="ja"><paramref name="signInManager"/>または<paramref name="user"/>が<see langword="null"/>の場合にスローされる。</para></exception>
    public static async Task<SignInResult> VerifyExternalLoginAsync<TUser>(
        this SignInManager<TUser> signInManager,
        TUser user,
        bool isTwoFactorClientRemembered,
        bool bypassTwoFactor = false)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(user);

        if (await PreSignInCheckAsync(signInManager, user).ConfigureAwait(false) is { } error)
        {
            return error;
        }

        return !bypassTwoFactor && !isTwoFactorClientRemembered && await signInManager.IsTwoFactorEnabledAsync(user).ConfigureAwait(false)
            ? SignInResult.TwoFactorRequired
            : SignInResult.Success;
    }

    // SignInManager.PreSignInCheck と同じ手順（protectedのため外部から呼び出せない）
    private static async Task<SignInResult?> PreSignInCheckAsync<TUser>(SignInManager<TUser> signInManager, TUser user)
        where TUser : class
    {
        if (!await signInManager.CanSignInAsync(user).ConfigureAwait(false))
        {
            return SignInResult.NotAllowed;
        }

        var userManager = signInManager.UserManager;
        return userManager.SupportsUserLockout && await userManager.IsLockedOutAsync(user).ConfigureAwait(false)
            ? SignInResult.LockedOut
            : null;
    }

    // ロックアウトの回数を数え、上限に達したらLockedOutを返す
    private static async Task<SignInResult> RecordAccessFailedAsync<TUser>(UserManager<TUser> userManager, TUser user)
        where TUser : class
    {
        if (!userManager.SupportsUserLockout)
        {
            return SignInResult.Failed;
        }

        if (!(await userManager.AccessFailedAsync(user).ConfigureAwait(false)).Succeeded)
        {
            return SignInResult.Failed;
        }

        return await userManager.IsLockedOutAsync(user).ConfigureAwait(false) ? SignInResult.LockedOut : SignInResult.Failed;
    }

    // 二要素認証の成功時の処理（SignInManager.DoTwoFactorSignInAsync のデータベース側）
    private static async Task<SignInResult> CompleteTwoFactorAsync<TUser>(UserManager<TUser> userManager, TUser user)
        where TUser : class =>
        (await ResetLockoutAsync(userManager, user).ConfigureAwait(false)).Succeeded ? SignInResult.Success : SignInResult.Failed;

    private static async Task<IdentityResult> ResetLockoutAsync<TUser>(UserManager<TUser> userManager, TUser user)
        where TUser : class =>
        userManager.SupportsUserLockout
            ? await userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false)
            : IdentityResult.Success;
}
