using D3Fragments;
using D3Fragments.Application;
using D3Fragments.AspNetCore.Identity;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.Account;

public sealed class LoginWith2faModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<SignInWithTwoFactorCodeCommand, SignInOutcome> signInWithCode,
    IRequestExecutor<SendTwoFactorEmailCodeCommand, Result> sendEmailCode) : AppPageModel(signIn)
{
    [BindProperty(SupportsGet = true)]
    public TwoFactorMethod Method { get; set; } = TwoFactorMethod.Authenticator;

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMachine { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool RememberMe { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IReadOnlyList<TwoFactorMethod> Methods { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync() =>
        await LoadAsync() is null ? RedirectToPage("/Account/Login") : Page();

    // Send a verification code by email
    public async Task<IActionResult> OnPostSendEmailCodeAsync()
    {
        if (await LoadAsync() is not { } account)
        {
            return RedirectToPage("/Account/Login");
        }
        ModelState.Clear();
        await sendEmailCode.ExecuteAsync(new SendTwoFactorEmailCodeCommand(account.Id), HttpContext.RequestAborted);
        TempData["Success"] = "A verification code has been sent by email.";
        return RedirectToPage(new { RememberMe, ReturnUrl, method = TwoFactorMethod.Email });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // The account waiting for two-factor authentication (read from the cookie issued by BeginTwoFactorSignInAsync after the first factor)
        if (await LoadAsync() is not { } account)
        {
            return RedirectToPage("/Account/Login");
        }
        if (string.IsNullOrWhiteSpace(Code))
        {
            ModelState.AddModelError(nameof(Code), "Enter the verification code.");
            return Page();
        }

        // Database side: verify the code and record or reset the access failure count
        SignInOutcome outcome = await signInWithCode.ExecuteAsync(new SignInWithTwoFactorCodeCommand(account.Id, Method, Code), HttpContext.RequestAborted);
        if (outcome.Status != SignInStatus.Succeeded)
        {
            AddSignInError(outcome);
            return Page();
        }

        // Cookie side: delete the two-factor cookie, sign in, and remember this browser if selected
        await SignInManager.CompleteTwoFactorSignInAsync(account, RememberMe, RememberMachine);
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/MyPage");
    }

    private async Task<UserAccount?> LoadAsync()
    {
        if (await SignInManager.GetTwoFactorAuthenticationUserAsync() is not { } account)
        {
            return null;
        }
        // The authenticator app is available if its key is registered, and email if it is confirmed
        IList<string> providers = await Users.GetValidTwoFactorProvidersAsync(account);
        Methods = [.. providers.Contains(Users.Options.Tokens.AuthenticatorTokenProvider) ? [TwoFactorMethod.Authenticator] : Array.Empty<TwoFactorMethod>(),
            .. providers.Contains(TokenOptions.DefaultEmailProvider) ? [TwoFactorMethod.Email] : Array.Empty<TwoFactorMethod>()];
        if (!Methods.Contains(Method) && Methods.Count > 0)
        {
            Method = Methods[0];
        }
        return account;
    }
}
