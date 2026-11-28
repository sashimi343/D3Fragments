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

    // Sign-in with a password
    public async Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // 1. Read the "remember this browser" account from the cookie (cookie side)
        string? rememberedAccountId = await SignInManager.GetTwoFactorClientRememberedUserIdAsync();

        // 2. Verify the password (database side; the UnitOfWork saves the access failure count and lockout)
        SignInOutcome outcome = await signInWithPassword.ExecuteAsync(
            new SignInWithPasswordCommand(Input.UserName.Trim(), Input.Password, rememberedAccountId), HttpContext.RequestAborted);

        // 3. Issue the cookie after the commit (cookie side)
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

    // Sign-in with a passkey (1/2): create the options to pass to the browser. The challenge is stored in a cookie (the database is not changed)
    public async Task<IActionResult> OnPostPasskeyOptionsAsync() =>
        Content(await SignInManager.MakePasskeyRequestOptionsAsync(null), "application/json");

    // Sign-in with a passkey (2/2): verify the signature, save the signature count, then issue the cookie
    public async Task<IActionResult> OnPostPasskeyAsync(string? credentialJson, string? returnUrl)
    {
        ModelState.Clear();
        if (string.IsNullOrEmpty(credentialJson))
        {
            ModelState.AddModelError(string.Empty, "Passkey authentication was canceled.");
            return Page();
        }

        // Verify the signature and origin (the database is not changed)
        PasskeyAssertionResult<UserAccount> assertion = await SignInManager.PerformPasskeyAssertionAsync(credentialJson);
        if (!assertion.Succeeded)
        {
            ModelState.AddModelError(string.Empty, $"Passkey authentication failed. ({assertion.Failure.Message})");
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

        // A passkey is multi-factor by itself (possession plus biometrics or PIN), so two-factor authentication is not required (same as Identity)
        await SignInManager.CompleteSignInAsync(assertion.User, isPersistent: false);
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : "/MyPage");
    }

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "Enter a user name.")]
        [Display(Name = "User name")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a password."), DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Keep me signed in")]
        public bool RememberMe { get; set; }
    }
}
