using System.ComponentModel.DataAnnotations;

using D3Fragments.Application;
using D3Fragments.AspNetCore.Identity;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.Account;

public sealed class LoginModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<SignInWithPasswordCommand, SignInOutcome> signInWithPassword,
    IRequestExecutor<SignInWithPasskeyCommand, SignInOutcome> signInWithPasskey) : AppPageModel(signIn)
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    // パスワードでのログイン
    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // 1. Cookie から「このブラウザを記憶する」のアカウントを読み取る（Cookie 側）
        string? rememberedAccountId = await SignInManager.GetTwoFactorClientRememberedUserIdAsync();

        // 2. パスワードを検証する（DB 側。アクセス失敗回数とロックアウトは UnitOfWork が保存する）
        SignInOutcome outcome = await signInWithPassword.ExecuteAsync(
            new SignInWithPasswordCommand(Input.UserName.Trim(), Input.Password, rememberedAccountId), HttpContext.RequestAborted);

        // 3. Commit 後に Cookie を発行する（Cookie 側）
        switch (outcome.Status)
        {
            case SignInStatus.Succeeded:
                await SignInManager.CompleteSignInAsync(await FindAccountAsync(outcome.AccountId!.Value), Input.RememberMe);
                return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/MyPage");
            case SignInStatus.RequiresTwoFactor:
                await SignInManager.BeginTwoFactorSignInAsync(await FindAccountAsync(outcome.AccountId!.Value));
                return RedirectToPage("/Account/LoginWith2fa", new { rememberMe = Input.RememberMe, returnUrl });
            default:
                AddSignInError(outcome);
                return Page();
        }
    }

    // パスキーでのログイン（1/2）: ブラウザに渡すオプションを作る。チャレンジは Cookie に保存される（DB は変更しない）
    public async Task<IActionResult> OnPostPasskeyOptionsAsync() =>
        Content(await SignInManager.MakePasskeyRequestOptionsAsync(null), "application/json");

    // パスキーでのログイン（2/2）: 署名を検証し、署名回数を保存してから Cookie を発行する
    public async Task<IActionResult> OnPostPasskeyAsync(string? credentialJson, string? returnUrl)
    {
        ModelState.Clear();
        if (string.IsNullOrEmpty(credentialJson))
        {
            ModelState.AddModelError(string.Empty, "パスキーでの認証が取り消されました。");
            return Page();
        }

        // 署名とオリジンの検証（DB は変更しない）
        PasskeyAssertionResult<UserAccount> assertion = await SignInManager.PerformPasskeyAssertionAsync(credentialJson);
        if (!assertion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, $"パスキーでの認証に失敗しました。（{assertion.Failure.Message}）");
            return Page();
        }

        SignInOutcome outcome = await signInWithPasskey.ExecuteAsync(
            new SignInWithPasskeyCommand(assertion.User.Id, assertion.Passkey.CredentialId, assertion.Passkey.SignCount, assertion.Passkey.IsBackedUp),
            HttpContext.RequestAborted);
        if (outcome.Status != SignInStatus.Succeeded)
        {
            AddSignInError(outcome);
            return Page();
        }

        // パスキーはそれ自体が多要素（所持と生体認証・PIN）のため、二要素認証を求めない（Identity と同じ）
        await SignInManager.CompleteSignInAsync(assertion.User, isPersistent: false);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/MyPage");
    }

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "ユーザー名を入力してください。")]
        [Display(Name = "ユーザー名")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "パスワードを入力してください。"), DataType(DataType.Password)]
        [Display(Name = "パスワード")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "ログイン状態を保持する")]
        public bool RememberMe { get; set; }
    }
}
