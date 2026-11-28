using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.Account;

// Signing out only deletes cookies and does not change the database, so no Command is used
public sealed class LogoutModel(SignInManager<UserAccount> signIn) : AppPageModel(signIn)
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await SignInManager.SignOutAsync();
        TempData["Success"] = "You have signed out.";
        return RedirectToPage("/Index");
    }
}
