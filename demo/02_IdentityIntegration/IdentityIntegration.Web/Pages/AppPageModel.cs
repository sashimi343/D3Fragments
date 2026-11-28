using D3Fragments;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityIntegration.Web.Pages;

// UserManager is used only for reading (e.g. getting the user to issue cookies for); writes to the database are done by Commands
public abstract class AppPageModel(SignInManager<UserAccount> signIn) : PageModel
{
    protected SignInManager<UserAccount> SignInManager => signIn;

    protected UserManager<UserAccount> Users => signIn.UserManager;

    // ID of the signed-in account (read from the cookie claims; the database is not queried)
    protected UuidV7 CurrentAccountId => UuidV7.Parse(Users.GetUserId(User)!, null);

    // The signed-in account
    protected async Task<UserAccount> GetCurrentAccountAsync() =>
        await Users.GetUserAsync(User) ?? throw new InvalidOperationException("The signed-in account was not found.");

    // Gets the account returned by a Command (used to issue cookies)
    protected async Task<UserAccount> FindAccountAsync(UuidV7 accountId) =>
        await Users.FindByIdAsync(accountId.ToString()) ?? throw new InvalidOperationException("The account was not found.");

    protected void AddErrors(Result result)
    {
        foreach (Error error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Message);
        }
    }

    // Shows why sign-in failed
    protected void AddSignInError(SignInOutcome outcome) =>
        ModelState.AddModelError(string.Empty, outcome.Status switch
        {
            SignInStatus.LockedOut => "The account is locked because of repeated sign-in failures. Please try again later.",
            SignInStatus.NotAllowed => "Your email address has not been confirmed. Open the link in the confirmation email.",
            _ => "Authentication failed. Check what you entered.",
        });
}
