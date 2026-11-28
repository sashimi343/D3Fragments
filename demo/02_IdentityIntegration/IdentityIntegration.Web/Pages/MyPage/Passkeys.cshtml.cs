using D3Fragments;
using D3Fragments.Application;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace IdentityIntegration.Web.Pages.MyPage;

public sealed class PasskeysModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<AddPasskeyCommand, Result> addPasskey,
    IRequestExecutor<RemovePasskeyCommand, Result> removePasskey) : AppPageModel(signIn)
{
    public IReadOnlyList<UserPasskeyInfo> Passkeys { get; private set; } = [];

    public async Task OnGetAsync() => await LoadAsync();

    // Registration (1/2): create the options to pass to the browser. The challenge is stored in a cookie (the database is not changed)
    public async Task<IActionResult> OnPostCreationOptionsAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        string options = await SignInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = account.Id.ToString(),
            Name = account.UserName!,
            DisplayName = account.UserName!,
        });
        return Content(options, "application/json");
    }

    // Registration (2/2): verify the passkey created by the browser and save it with a Command
    public async Task<IActionResult> OnPostAddAsync(string? credentialJson, string? name)
    {
        UserAccount account = await GetCurrentAccountAsync();
        if (string.IsNullOrEmpty(credentialJson))
        {
            ModelState.AddModelError(string.Empty, "Passkey registration was canceled.");
            await LoadAsync();
            return Page();
        }

        // Verify the signature and origin (the database is not changed)
        PasskeyAttestationResult attestation = await SignInManager.PerformPasskeyAttestationAsync(credentialJson);
        if (!attestation.Succeeded)
        {
            ModelState.AddModelError(string.Empty, $"Passkey registration failed. ({attestation.Failure.Message})");
            await LoadAsync();
            return Page();
        }

        UserPasskeyInfo passkey = attestation.Passkey;
        Result result = await addPasskey.ExecuteAsync(new AddPasskeyCommand(account.Id, new PasskeyCredential(
            passkey.CredentialId, passkey.PublicKey, passkey.CreatedAt, passkey.SignCount, passkey.Transports,
            passkey.IsUserVerified, passkey.IsBackupEligible, passkey.IsBackedUp, passkey.AttestationObject, passkey.ClientDataJson,
            string.IsNullOrWhiteSpace(name) ? "Passkey" : name.Trim())), HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            AddErrors(result);
            await LoadAsync();
            return Page();
        }

        TempData["Success"] = "The passkey has been registered. You can use it with \"Sign in with a passkey\" on the sign-in page.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string credentialId)
    {
        UserAccount account = await GetCurrentAccountAsync();
        await removePasskey.ExecuteAsync(new RemovePasskeyCommand(account.Id, WebEncoders.Base64UrlDecode(credentialId)), HttpContext.RequestAborted);
        // Deleting a passkey does not change the security stamp, so other sessions signed in with that passkey are not ended
        TempData["Success"] = "The passkey has been deleted.";
        return RedirectToPage();
    }

    private async Task LoadAsync() => Passkeys = [.. await Users.GetPasskeysAsync(await GetCurrentAccountAsync())];
}
