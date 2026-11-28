using D3Fragments.Application;
using D3Fragments.AspNetCore.Identity;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.Account;

public sealed class LoginWithRecoveryCodeModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<SignInWithRecoveryCodeCommand, SignInOutcome> signInWithRecoveryCode) : AppPageModel(signIn)
{
    [BindProperty]
    public string RecoveryCode { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync() =>
        await SignInManager.GetTwoFactorAuthenticationUserAsync() is null ? RedirectToPage("/Account/Login") : Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (await SignInManager.GetTwoFactorAuthenticationUserAsync() is not { } account)
        {
            return RedirectToPage("/Account/Login");
        }
        if (string.IsNullOrWhiteSpace(RecoveryCode))
        {
            ModelState.AddModelError(nameof(RecoveryCode), "Enter a recovery code.");
            return Page();
        }

        // Database side: redeem the recovery code (a used code cannot be used again)
        SignInOutcome outcome = await signInWithRecoveryCode.ExecuteAsync(new SignInWithRecoveryCodeCommand(account.Id, RecoveryCode), HttpContext.RequestAborted);
        if (outcome.Status != SignInStatus.Succeeded)
        {
            AddSignInError(outcome);
            return Page();
        }

        // Cookie side: with a recovery code, neither keep the sign-in nor remember the browser (same as Identity)
        await SignInManager.CompleteTwoFactorSignInAsync(account, isPersistent: false, rememberClient: false);
        TempData["Success"] = "You have signed in with a recovery code. If you have few recovery codes left, regenerate them on My page.";
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/MyPage");
    }
}
