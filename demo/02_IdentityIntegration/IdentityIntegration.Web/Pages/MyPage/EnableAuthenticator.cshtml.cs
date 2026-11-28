using System.Text;

using D3Fragments;
using D3Fragments.Application;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.MyPage;

public sealed class EnableAuthenticatorModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<PrepareAuthenticatorKeyCommand, string> prepareKey,
    IRequestExecutor<EnableAuthenticatorCommand, Result<IReadOnlyList<string>>> enable) : AppPageModel(signIn)
{
    private const string Issuer = "IdentityIntegration";

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    public string SharedKey { get; private set; } = string.Empty;

    public string AuthenticatorUri { get; private set; } = string.Empty;

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ModelState.AddModelError(nameof(Code), "Enter the verification code.");
            await LoadAsync();
            return Page();
        }

        UserAccount account = await GetCurrentAccountAsync();
        Result<IReadOnlyList<string>> result = await enable.ExecuteAsync(new EnableAuthenticatorCommand(account.Id, Code), HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            AddErrors(result);
            await LoadAsync();
            return Page();
        }

        // Enabling two-factor authentication changes the security stamp, so refresh our own cookie
        await SignInManager.RefreshSignInAsync(account);
        TempData["Success"] = "The authenticator app has been set up and two-factor authentication is enabled.";
        if (result.Value!.Count == 0)
        {
            return RedirectToPage("/MyPage/TwoFactor");
        }
        TempData["RecoveryCodes"] = string.Join(' ', result.Value);
        return RedirectToPage("/MyPage/RecoveryCodes");
    }

    private async Task LoadAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        // Create the key if it does not exist yet (done with a Command because it writes to the database; the security stamp also changes)
        string key = await prepareKey.ExecuteAsync(new PrepareAuthenticatorKeyCommand(account.Id), HttpContext.RequestAborted);
        await SignInManager.RefreshSignInAsync(account);

        SharedKey = FormatKey(key);
        string label = Uri.EscapeDataString($"{Issuer}:{account.UserName}");
        AuthenticatorUri = $"otpauth://totp/{label}?secret={key}&issuer={Uri.EscapeDataString(Issuer)}&digits=6";
    }

    // Split into groups of 4 characters for easier manual entry
    private static string FormatKey(string key)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < key.Length; i += 4)
        {
            builder.Append(key.AsSpan(i, Math.Min(4, key.Length - i))).Append(' ');
        }
        return builder.ToString().TrimEnd().ToLowerInvariant();
    }
}
