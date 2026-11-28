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

    // Enable two-factor authentication with email verification codes (usable right away because the email address was confirmed at registration)
    public async Task<IActionResult> OnPostEnableEmailAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        IReadOnlyList<string> codes = await enableEmail.ExecuteAsync(new EnableEmailTwoFactorCommand(account.Id), HttpContext.RequestAborted);
        // Enabling two-factor authentication changes the security stamp, so refresh our own cookie
        await SignInManager.RefreshSignInAsync(account);
        return ShowRecoveryCodes(codes, "Two-factor authentication has been enabled.");
    }

    public async Task<IActionResult> OnPostDisableAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        await disable.ExecuteAsync(new DisableTwoFactorCommand(account.Id), HttpContext.RequestAborted);
        await SignInManager.RefreshSignInAsync(account);
        TempData["Success"] = "Two-factor authentication has been disabled.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRegenerateRecoveryCodesAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        IReadOnlyList<string> codes = await regenerateRecoveryCodes.ExecuteAsync(new RegenerateRecoveryCodesCommand(account.Id), HttpContext.RequestAborted);
        return ShowRecoveryCodes(codes, "The recovery codes have been regenerated. The previous recovery codes can no longer be used.");
    }

    // Delete the "remember this browser" cookie (the database is not changed)
    public async Task<IActionResult> OnPostForgetBrowserAsync()
    {
        await SignInManager.ForgetTwoFactorClientAsync();
        TempData["Success"] = "This browser is no longer remembered. Two-factor authentication will be required from the next sign-in.";
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
