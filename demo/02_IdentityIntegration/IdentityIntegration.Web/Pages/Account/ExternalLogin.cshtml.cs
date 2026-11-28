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

    // Redirects to the external identity provider
    public IActionResult OnPost(string provider)
    {
        string? redirectUrl = Url.Page("/Account/ExternalLogin", "Callback");
        return new ChallengeResult(provider, SignInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl));
    }

    // When returning from the identity provider
    public async Task<IActionResult> OnGetCallbackAsync(string? remoteError)
    {
        // The authentication result from the identity provider (read from the external login cookie; the database is not changed)
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
                // Even with external login, accounts with two-factor authentication enabled require the second step (same as Identity)
                await SignInManager.BeginTwoFactorSignInAsync(await FindAccountAsync(outcome.AccountId!.Value), info.LoginProvider);
                return RedirectToPage("/Account/LoginWith2fa");
            case SignInStatus.NotRegistered:
                // First sign-in: show the page to create an account
                Load(info);
                UserName = info.Principal.FindFirstValue("preferred_username") ?? string.Empty;
                Nickname = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? UserName;
                return Page();
            default:
                AddSignInError(outcome);
                return Page();
        }
    }

    // On the first sign-in, create the account and sign in
    public async Task<IActionResult> OnPostConfirmationAsync()
    {
        if (await SignInManager.GetExternalLoginInfoAsync() is not { } info)
        {
            return RedirectToPage("/Account/Login");
        }
        Load(info);
        if (EmailAddress is null)
        {
            ModelState.AddModelError(string.Empty, "The email address could not be obtained from the identity provider.");
            return Page();
        }
        if (string.IsNullOrWhiteSpace(UserName))
        {
            ModelState.AddModelError(nameof(UserName), "Enter a user name.");
            return Page();
        }

        // The email address comes from the identity provider's claims, not the form
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
