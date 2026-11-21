using System.Globalization;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.AspNetCore.Identity;

/// <summary>
///     <para xml:lang="en">Extension methods that perform the cookie side of the <see cref="SignInManager{TUser}"/> sign-in methods, without changing the database. Call them in the presentation layer: read the cookie state before a sign-in command, and issue the cookies after the command succeeds. The database side is performed in the command with the <c>Verify*</c> extension methods of <c>D3Fragments.Identity.EFCore</c>.</para>
///     <para xml:lang="ja"><see cref="SignInManager{TUser}"/>のサインイン系メソッドのうち、データベースを変更しないCookie側の処理を行う拡張メソッド。プレゼンテーション層で呼び出し、サインインのCommandの前にCookieの状態を読み取り、Commandの成功後にCookieを発行する。データベース側の処理は、Commandの中で<c>D3Fragments.Identity.EFCore</c>の<c>Verify*</c>拡張メソッドにより行う。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">The cookies are the same as those of <see cref="SignInManager{TUser}"/> (<see cref="IdentityConstants.TwoFactorUserIdScheme"/>, <see cref="IdentityConstants.TwoFactorRememberMeScheme"/> and <see cref="SignInManager{TUser}.AuthenticationScheme"/>), so <see cref="SignInManager{TUser}.GetTwoFactorAuthenticationUserAsync"/>, <see cref="SignInManager{TUser}.ForgetTwoFactorClientAsync"/>, <see cref="SignInManager{TUser}.RefreshSignInAsync"/> and <see cref="SignInManager{TUser}.SignOutAsync"/> can be used together. Those methods and the passkey ceremony methods (<see cref="SignInManager{TUser}.MakePasskeyRequestOptionsAsync"/>, <see cref="SignInManager{TUser}.PerformPasskeyAssertionAsync"/>, and so on) do not change the database, so call them directly. The claims added to the application cookie are the same as those of <see cref="SignInManager{TUser}"/>; to add other claims, call <see cref="SignInManager{TUser}.SignInWithClaimsAsync(TUser, bool, IEnumerable{Claim})"/> directly.</para>
///     <para xml:lang="ja">Cookieは<see cref="SignInManager{TUser}"/>と同じもの（<see cref="IdentityConstants.TwoFactorUserIdScheme"/>、<see cref="IdentityConstants.TwoFactorRememberMeScheme"/>、<see cref="SignInManager{TUser}.AuthenticationScheme"/>）を使うため、<see cref="SignInManager{TUser}.GetTwoFactorAuthenticationUserAsync"/>、<see cref="SignInManager{TUser}.ForgetTwoFactorClientAsync"/>、<see cref="SignInManager{TUser}.RefreshSignInAsync"/>、<see cref="SignInManager{TUser}.SignOutAsync"/>と組み合わせて使える。これらのメソッドとパスキーの手続きのメソッド（<see cref="SignInManager{TUser}.MakePasskeyRequestOptionsAsync"/>、<see cref="SignInManager{TUser}.PerformPasskeyAssertionAsync"/>など）はデータベースを変更しないため、直接呼び出す。アプリケーションのCookieに追加するクレームは<see cref="SignInManager{TUser}"/>と同じ。別のクレームを追加する場合は、<see cref="SignInManager{TUser}.SignInWithClaimsAsync(TUser, bool, IEnumerable{Claim})"/>を直接呼び出す。</para>
/// </remarks>
public static class D3SignInManagerSessionExtensions
{
    private const string AuthenticationMethodReferenceClaimType = "amr";

    /// <summary>
    ///     <para xml:lang="en">Gets the ID of the user for whom the client is remembered for two-factor authentication, from the cookie issued by <see cref="SignInManager{TUser}.RememberTwoFactorClientAsync"/>. Pass whether it equals the ID of the signing-in user to the sign-in command.</para>
    ///     <para xml:lang="ja"><see cref="SignInManager{TUser}.RememberTwoFactorClientAsync"/>が発行したCookieから、クライアントを二要素認証について記憶しているユーザーのIDを取得する。サインインするユーザーのIDと一致するかを、サインインのCommandに渡す。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">This is the cookie side of <see cref="SignInManager{TUser}.IsTwoFactorClientRememberedAsync"/>. The cookie is validated with the security stamp by the cookie authentication handler registered by <c>AddIdentityCookies</c>. Returns <see langword="null"/> when the scheme is not registered or the client is not remembered.</para>
    ///     <para xml:lang="ja"><see cref="SignInManager{TUser}.IsTwoFactorClientRememberedAsync"/>のCookie側の処理。Cookieは、<c>AddIdentityCookies</c>が登録するCookie認証ハンドラーがsecurity stampで検証する。スキームが登録されていない場合や、クライアントが記憶されていない場合は<see langword="null"/>を返す。</para>
    /// </remarks>
    /// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
    /// <param name="signInManager"><para xml:lang="en">The sign-in manager.</para><para xml:lang="ja">サインインマネージャー。</para></param>
    /// <returns><para xml:lang="en">The ID of the remembered user, or <see langword="null"/>.</para><para xml:lang="ja">記憶しているユーザーのID。ない場合は<see langword="null"/>。</para></returns>
    /// <exception cref="ArgumentNullException"><para xml:lang="en">Thrown when <paramref name="signInManager"/> is <see langword="null"/>.</para><para xml:lang="ja"><paramref name="signInManager"/>が<see langword="null"/>の場合にスローされる。</para></exception>
    public static async Task<string?> GetTwoFactorClientRememberedUserIdAsync<TUser>(this SignInManager<TUser> signInManager)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(signInManager);

        if (!await IsSchemeRegisteredAsync(signInManager, IdentityConstants.TwoFactorRememberMeScheme).ConfigureAwait(false))
        {
            return null;
        }

        var result = await signInManager.Context.AuthenticateAsync(IdentityConstants.TwoFactorRememberMeScheme).ConfigureAwait(false);
        return result.Principal?.FindFirstValue(ClaimTypes.Name);
    }

    /// <summary>
    ///     <para xml:lang="en">Issues the cookie that records the user waiting for two-factor authentication. Call this when the sign-in command reports that two-factor authentication is required. This is the cookie side of <see cref="SignInManager{TUser}.PasswordSignInAsync(TUser, string, bool, bool)"/> and <see cref="SignInManager{TUser}.ExternalLoginSignInAsync(string, string, bool, bool)"/> when they return <see cref="SignInResult.TwoFactorRequired"/>.</para>
    ///     <para xml:lang="ja">二要素認証を待っているユーザーを記録するCookieを発行する。サインインのCommandが二要素認証を必要と返したときに呼び出す。<see cref="SignInManager{TUser}.PasswordSignInAsync(TUser, string, bool, bool)"/>・<see cref="SignInManager{TUser}.ExternalLoginSignInAsync(string, string, bool, bool)"/>が<see cref="SignInResult.TwoFactorRequired"/>を返す場合のCookie側の処理。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">The cookie has the same contents as that of <see cref="SignInManager{TUser}"/>, so <see cref="SignInManager{TUser}.GetTwoFactorAuthenticationUserAsync"/> can read it. Nothing is issued when <see cref="IdentityConstants.TwoFactorUserIdScheme"/> is not registered.</para>
    ///     <para xml:lang="ja">Cookieの内容は<see cref="SignInManager{TUser}"/>と同じため、<see cref="SignInManager{TUser}.GetTwoFactorAuthenticationUserAsync"/>で読み取れる。<see cref="IdentityConstants.TwoFactorUserIdScheme"/>が登録されていない場合は何も発行しない。</para>
    /// </remarks>
    /// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
    /// <param name="signInManager"><para xml:lang="en">The sign-in manager.</para><para xml:lang="ja">サインインマネージャー。</para></param>
    /// <param name="user"><para xml:lang="en">The user waiting for two-factor authentication.</para><para xml:lang="ja">二要素認証を待つユーザー。</para></param>
    /// <param name="loginProvider"><para xml:lang="en">The external login provider used for the first factor, or <see langword="null"/>.</para><para xml:lang="ja">1要素目に使った外部ログインのプロバイダー。ない場合は<see langword="null"/>。</para></param>
    /// <returns><para xml:lang="en">A task that represents the operation.</para><para xml:lang="ja">操作を表すタスク。</para></returns>
    /// <exception cref="ArgumentNullException"><para xml:lang="en">Thrown when <paramref name="signInManager"/> or <paramref name="user"/> is <see langword="null"/>.</para><para xml:lang="ja"><paramref name="signInManager"/>または<paramref name="user"/>が<see langword="null"/>の場合にスローされる。</para></exception>
    public static async Task BeginTwoFactorSignInAsync<TUser>(this SignInManager<TUser> signInManager, TUser user, string? loginProvider = null)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(user);

        if (!await IsSchemeRegisteredAsync(signInManager, IdentityConstants.TwoFactorUserIdScheme).ConfigureAwait(false))
        {
            return;
        }

        var identity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
        identity.AddClaim(new Claim(ClaimTypes.Name, await signInManager.UserManager.GetUserIdAsync(user).ConfigureAwait(false)));
        if (loginProvider is not null)
        {
            identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, loginProvider));
        }

        await signInManager.Context.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, new ClaimsPrincipal(identity)).ConfigureAwait(false);
    }

    /// <summary>
    ///     <para xml:lang="en">Issues the application cookie after a sign-in command succeeds without two-factor authentication (with a password, a passkey or an external login). This is the cookie side of the successful <see cref="SignInManager{TUser}.PasswordSignInAsync(TUser, string, bool, bool)"/>, <see cref="SignInManager{TUser}.PasskeySignInAsync"/> and <see cref="SignInManager{TUser}.ExternalLoginSignInAsync(string, string, bool, bool)"/>.</para>
    ///     <para xml:lang="ja">二要素認証なしでサインインのCommandが成功した後（パスワード、パスキー、外部ログイン）に、アプリケーションのCookieを発行する。成功した<see cref="SignInManager{TUser}.PasswordSignInAsync(TUser, string, bool, bool)"/>・<see cref="SignInManager{TUser}.PasskeySignInAsync"/>・<see cref="SignInManager{TUser}.ExternalLoginSignInAsync(string, string, bool, bool)"/>のCookie側の処理。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">As with <see cref="SignInManager{TUser}"/>, an <c>amr</c> claim of <c>pwd</c> is added when <paramref name="loginProvider"/> is <see langword="null"/> (also for a passkey); otherwise the external cookie is removed and an authentication method claim of <paramref name="loginProvider"/> is added.</para>
    ///     <para xml:lang="ja"><see cref="SignInManager{TUser}"/>と同じく、<paramref name="loginProvider"/>が<see langword="null"/>の場合（パスキーの場合も）は値が<c>pwd</c>の<c>amr</c>クレームを追加する。それ以外の場合は外部ログインのCookieを削除し、<paramref name="loginProvider"/>を認証方法のクレームとして追加する。</para>
    /// </remarks>
    /// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
    /// <param name="signInManager"><para xml:lang="en">The sign-in manager.</para><para xml:lang="ja">サインインマネージャー。</para></param>
    /// <param name="user"><para xml:lang="en">The user to sign in.</para><para xml:lang="ja">サインインするユーザー。</para></param>
    /// <param name="isPersistent"><para xml:lang="en">Whether the cookie persists after the browser is closed.</para><para xml:lang="ja">ブラウザを閉じた後もCookieを保持するかどうか。</para></param>
    /// <param name="loginProvider"><para xml:lang="en">The external login provider, or <see langword="null"/>.</para><para xml:lang="ja">外部ログインのプロバイダー。ない場合は<see langword="null"/>。</para></param>
    /// <returns><para xml:lang="en">A task that represents the operation.</para><para xml:lang="ja">操作を表すタスク。</para></returns>
    /// <exception cref="ArgumentNullException"><para xml:lang="en">Thrown when <paramref name="signInManager"/> or <paramref name="user"/> is <see langword="null"/>.</para><para xml:lang="ja"><paramref name="signInManager"/>または<paramref name="user"/>が<see langword="null"/>の場合にスローされる。</para></exception>
    public static async Task CompleteSignInAsync<TUser>(this SignInManager<TUser> signInManager, TUser user, bool isPersistent, string? loginProvider = null)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(user);

        if (loginProvider is null)
        {
            await signInManager.SignInWithClaimsAsync(user, isPersistent, [new Claim(AuthenticationMethodReferenceClaimType, "pwd")]).ConfigureAwait(false);
            return;
        }

        await signInManager.Context.SignOutAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false);
        await signInManager.SignInAsync(user, isPersistent, loginProvider).ConfigureAwait(false);
    }

    /// <summary>
    ///     <para xml:lang="en">Issues the application cookie after a two-factor sign-in command succeeds (with a code or a recovery code), removes the cookie of the waiting user, and remembers the client when requested. This is the cookie side of the successful <see cref="SignInManager{TUser}.TwoFactorSignInAsync"/>, <see cref="SignInManager{TUser}.TwoFactorAuthenticatorSignInAsync"/> and <see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/>.</para>
    ///     <para xml:lang="ja">二要素認証のCommand（コードまたは回復コード）が成功した後に、アプリケーションのCookieを発行し、待っているユーザーのCookieを削除して、要求された場合はクライアントを記憶する。成功した<see cref="SignInManager{TUser}.TwoFactorSignInAsync"/>・<see cref="SignInManager{TUser}.TwoFactorAuthenticatorSignInAsync"/>・<see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/>のCookie側の処理。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">As with <see cref="SignInManager{TUser}"/>, an <c>amr</c> claim of <c>mfa</c> is added, and the external login provider of the first factor is added as an authentication method claim. <see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/> passes <see langword="false"/> to both <paramref name="isPersistent"/> and <paramref name="rememberClient"/>. To prevent signing in a different user, <paramref name="user"/> must be the user waiting for two-factor authentication.</para>
    ///     <para xml:lang="ja"><see cref="SignInManager{TUser}"/>と同じく、値が<c>mfa</c>の<c>amr</c>クレームを追加し、1要素目の外部ログインのプロバイダーを認証方法のクレームとして追加する。<see cref="SignInManager{TUser}.TwoFactorRecoveryCodeSignInAsync"/>は<paramref name="isPersistent"/>・<paramref name="rememberClient"/>のどちらにも<see langword="false"/>を渡す。別のユーザーをサインインさせないよう、<paramref name="user"/>は二要素認証を待っているユーザーでなければならない。</para>
    /// </remarks>
    /// <typeparam name="TUser"><para xml:lang="en">The type of the user.</para><para xml:lang="ja">ユーザーの型。</para></typeparam>
    /// <param name="signInManager"><para xml:lang="en">The sign-in manager.</para><para xml:lang="ja">サインインマネージャー。</para></param>
    /// <param name="user"><para xml:lang="en">The user waiting for two-factor authentication.</para><para xml:lang="ja">二要素認証を待っているユーザー。</para></param>
    /// <param name="isPersistent"><para xml:lang="en">Whether the cookie persists after the browser is closed.</para><para xml:lang="ja">ブラウザを閉じた後もCookieを保持するかどうか。</para></param>
    /// <param name="rememberClient"><para xml:lang="en">Whether to remember the client so that two-factor authentication is not required next time.</para><para xml:lang="ja">次回から二要素認証を求めないよう、クライアントを記憶するかどうか。</para></param>
    /// <returns><para xml:lang="en">A task that represents the operation.</para><para xml:lang="ja">操作を表すタスク。</para></returns>
    /// <exception cref="ArgumentNullException"><para xml:lang="en">Thrown when <paramref name="signInManager"/> or <paramref name="user"/> is <see langword="null"/>.</para><para xml:lang="ja"><paramref name="signInManager"/>または<paramref name="user"/>が<see langword="null"/>の場合にスローされる。</para></exception>
    /// <exception cref="InvalidOperationException"><para xml:lang="en">Thrown when no user is waiting for two-factor authentication, or <paramref name="user"/> is not that user.</para><para xml:lang="ja">二要素認証を待っているユーザーがいない場合、または<paramref name="user"/>がそのユーザーでない場合にスローされる。</para></exception>
    public static async Task CompleteTwoFactorSignInAsync<TUser>(this SignInManager<TUser> signInManager, TUser user, bool isPersistent, bool rememberClient)
        where TUser : class
    {
        ArgumentNullException.ThrowIfNull(signInManager);
        ArgumentNullException.ThrowIfNull(user);

        var pending = await IsSchemeRegisteredAsync(signInManager, IdentityConstants.TwoFactorUserIdScheme).ConfigureAwait(false)
            ? (await signInManager.Context.AuthenticateAsync(IdentityConstants.TwoFactorUserIdScheme).ConfigureAwait(false)).Principal
            : null;
        var pendingUserId = pending?.FindFirstValue(ClaimTypes.Name)
            ?? throw new InvalidOperationException(ExceptionMessages.InvalidOperationException_TwoFactorNotPending);
        if (pendingUserId != await signInManager.UserManager.GetUserIdAsync(user).ConfigureAwait(false))
        {
            throw new InvalidOperationException(ExceptionMessages.InvalidOperationException_TwoFactorUserMismatch);
        }

        List<Claim> claims = [new Claim(AuthenticationMethodReferenceClaimType, "mfa")];
        if (pending!.FindFirstValue(ClaimTypes.AuthenticationMethod) is { } loginProvider)
        {
            claims.Add(new Claim(ClaimTypes.AuthenticationMethod, loginProvider));
        }

        if (await IsSchemeRegisteredAsync(signInManager, IdentityConstants.ExternalScheme).ConfigureAwait(false))
        {
            await signInManager.Context.SignOutAsync(IdentityConstants.ExternalScheme).ConfigureAwait(false);
        }

        await signInManager.Context.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme).ConfigureAwait(false);
        if (rememberClient)
        {
            await signInManager.RememberTwoFactorClientAsync(user).ConfigureAwait(false);
        }

        await signInManager.SignInWithClaimsAsync(user, isPersistent, claims).ConfigureAwait(false);
    }

    private static async Task<bool> IsSchemeRegisteredAsync<TUser>(SignInManager<TUser> signInManager, string scheme)
        where TUser : class =>
        await signInManager.Context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>().GetSchemeAsync(scheme).ConfigureAwait(false) is not null;
}
