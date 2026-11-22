using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.Account;

// ログアウトは Cookie を消すだけで DB を変更しないため、Command を使わない
public sealed class LogoutModel(SignInManager<UserAccount> signIn) : AppPageModel(signIn)
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await SignInManager.SignOutAsync();
        TempData["Success"] = "ログアウトしました。";
        return RedirectToPage("/Index");
    }
}
