using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using D3Fragments;
using D3Fragments.Application;
using D3Fragments.AspNetCore.Identity;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.Account;

public sealed class ExternalLoginModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<SignInWithExternalLoginCommand, SignInOutcome> signInWithExternalLogin,
    IRequestExecutor<RegisterWithExternalLoginCommand, Result<UuidV7>> registerWithExternalLogin) : AppPageModel(signIn)
{
    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string Nickname { get; set; } = string.Empty;

    public string? EmailAddress { get; private set; }

    public string? ProviderDisplayName { get; private set; }

    public IActionResult OnGet() => RedirectToPage("/Account/Login");

    // 外部の ID プロバイダーへリダイレクトする
    public IActionResult OnPost(string provider)
    {
        string? redirectUrl = Url.Page("/Account/ExternalLogin", "Callback");
        return new ChallengeResult(provider, SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl));
    }

    // ID プロバイダーから戻ってきたとき
    public async Task<IActionResult> OnGetCallbackAsync(string? remoteError)
    {
        // ID プロバイダーでの認証結果（外部ログイン用の Cookie から読み取る。DB は変更しない）
        if (remoteError is not null || await SignInManager.GetExternalLoginInfoAsync() is not { } info)
        {
            return RedirectToPage("/Account/Login");
        }

        string? rememberedAccountId = await SignInManager.GetTwoFactorClientRememberedUserIdAsync();
        SignInOutcome outcome = await signInWithExternalLogin.ExecuteAsync(
            new SignInWithExternalLoginCommand(info.LoginProvider, info.ProviderKey, rememberedAccountId), HttpContext.RequestAborted);
        switch (outcome.Status)
        {
            case SignInStatus.Succeeded:
                await SignInManager.CompleteSignInAsync(await FindAccountAsync(outcome.AccountId!.Value), isPersistent: false, info.LoginProvider);
                return LocalRedirect("/MyPage");
            case SignInStatus.RequiresTwoFactor:
                // 外部ログインでも、二要素認証を有効にしたアカウントは2段目を求める（Identity と同じ）
                await SignInManager.BeginTwoFactorSignInAsync(await FindAccountAsync(outcome.AccountId!.Value), info.LoginProvider);
                return RedirectToPage("/Account/LoginWith2fa");
            case SignInStatus.NotRegistered:
                // 初めてのログイン: アカウントを作る確認画面を出す
                Load(info);
                UserName = info.Principal.FindFirstValue("preferred_username") ?? string.Empty;
                Nickname = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? UserName;
                return Page();
            default:
                AddSignInError(outcome);
                return Page();
        }
    }

    // 初めてのログインで、アカウントを作ってサインインする
    public async Task<IActionResult> OnPostConfirmationAsync()
    {
        if (await SignInManager.GetExternalLoginInfoAsync() is not { } info)
        {
            return RedirectToPage("/Account/Login");
        }
        Load(info);
        if (EmailAddress is null)
        {
            ModelState.AddModelError(string.Empty, "ID プロバイダーからメールアドレスを取得できませんでした。");
            return Page();
        }
        if (string.IsNullOrWhiteSpace(UserName))
        {
            ModelState.AddModelError(nameof(UserName), "ユーザー名を入力してください。");
            return Page();
        }

        // メールアドレスはフォームではなく、ID プロバイダーのクレームから使う
        Result<UuidV7> result = await registerWithExternalLogin.ExecuteAsync(
            new RegisterWithExternalLoginCommand(UserName.Trim(), EmailAddress, Nickname, info.LoginProvider, info.ProviderKey, info.ProviderDisplayName ?? info.LoginProvider),
            HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            AddErrors(result);
            return Page();
        }

        await SignInManager.CompleteSignInAsync(await FindAccountAsync(result.Value), isPersistent: false, info.LoginProvider);
        return LocalRedirect("/MyPage");
    }

    private void Load(ExternalLoginInfo info)
    {
        EmailAddress = info.Principal.FindFirstValue(ClaimTypes.Email);
        ProviderDisplayName = info.ProviderDisplayName ?? info.LoginProvider;
    }
}
