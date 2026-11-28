using System.Text;

using D3Fragments;
using D3Fragments.Application;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace IdentityIntegration.Web.Pages.Account;

public sealed class ConfirmEmailModel(SignInManager<UserAccount> signIn, IRequestExecutor<ConfirmEmailCommand, Result> confirm) : AppPageModel(signIn)
{
    public bool Confirmed { get; private set; }

    // Confirmed with GET because it is opened from the link in the email (same as the Identity template)
    public async Task<IActionResult> OnGetAsync(string? accountId, string? token)
    {
        if (!UuidV7.TryParse(accountId, null, out UuidV7 id) || token is null)
        {
            return RedirectToPage("/Index");
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            ModelState.AddModelError(string.Empty, "The confirmation link is invalid.");
            return Page();
        }

        Result result = await confirm.ExecuteAsync(new ConfirmEmailCommand(id, decoded), HttpContext.RequestAborted);
        AddErrors(result);
        Confirmed = result.IsSuccess;
        return Page();
    }
}
