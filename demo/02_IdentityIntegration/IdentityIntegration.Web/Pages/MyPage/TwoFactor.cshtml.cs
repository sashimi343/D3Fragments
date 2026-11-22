using D3Fragments;
using D3Fragments.Application;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.MyPage;

public sealed class TwoFactorModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<EnableEmailTwoFactorCommand, IReadOnlyList<string>> enableEmail,
    IRequestExecutor<DisableTwoFactorCommand, Result> disable,
    IRequestExecutor<RegenerateRecoveryCodesCommand, IReadOnlyList<string>> regenerateRecoveryCodes) : AppPageModel(signIn)
{
    public bool IsEnabled { get; private set; }

    public bool HasAuthenticator { get; private set; }

    public int RecoveryCodesLeft { get; private set; }

    public bool IsBrowserRemembered { get; private set; }

    public async Task OnGetAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        IsEnabled = await Users.GetTwoFactorEnabledAsync(account);
        HasAuthenticator = !string.IsNullOrEmpty(await Users.GetAuthenticatorKeyAsync(account));
        RecoveryCodesLeft = await Users.CountRecoveryCodesAsync(account);
        IsBrowserRemembered = await SignInManager.IsTwoFactorClientRememberedAsync(account);
    }

    // メールの確認コードで二要素認証を有効にする（ユーザー登録でメールアドレスを確認済みのため、すぐ使える）
    public async Task<IActionResult> OnPostEnableEmailAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        IReadOnlyList<string> codes = await enableEmail.ExecuteAsync(new EnableEmailTwoFactorCommand(account.Id), HttpContext.RequestAborted);
        // 二要素認証の有効化で security stamp が変わるため、自分の Cookie を作り直す
        await SignInManager.RefreshSignInAsync(account);
        return ShowRecoveryCodes(codes, "二要素認証を有効にしました。");
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        await disable.ExecuteAsync(new DisableTwoFactorCommand(account.Id), HttpContext.RequestAborted);
        await SignInManager.RefreshSignInAsync(account);
        TempData["Success"] = "二要素認証を無効にしました。";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRegenerateRecoveryCodesAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        IReadOnlyList<string> codes = await regenerateRecoveryCodes.ExecuteAsync(new RegenerateRecoveryCodesCommand(account.Id), HttpContext.RequestAborted);
        return ShowRecoveryCodes(codes, "回復コードを作り直しました。前の回復コードは使えなくなりました。");
    }

    // 「このブラウザを記憶する」の Cookie を消す（DB は変更しない）
    public async Task<IActionResult> OnPostForgetBrowserAsync()
    {
        await SignInManager.ForgetTwoFactorClientAsync();
        TempData["Success"] = "このブラウザの記憶を解除しました。次回のログインから二要素認証を求めます。";
        return RedirectToPage();
    }

    private RedirectToPageResult ShowRecoveryCodes(IReadOnlyList<string> codes, string message)
    {
        TempData["Success"] = message;
        if (codes.Count == 0)
        {
            return RedirectToPage();
        }
        TempData["RecoveryCodes"] = string.Join(' ', codes);
        return RedirectToPage("/MyPage/RecoveryCodes");
    }
}
