using System.ComponentModel.DataAnnotations;
using System.Text;

using D3Fragments;
using D3Fragments.Application;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace IdentityIntegration.Web.Pages.Account;

public sealed class RegisterModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<RegisterAccountCommand, Result<RegisteredAccount>> register,
    IRequestExecutor<SendConfirmationEmailCommand, Result> sendConfirmation) : AppPageModel(signIn)
{
    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Result<RegisteredAccount> result = await register.ExecuteAsync(
            new RegisterAccountCommand(Input.UserName.Trim(), Input.EmailAddress.Trim(), Input.Password, Input.Nickname), HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            AddErrors(result);
            return Page();
        }

        // Send the confirmation email after the registration is committed
        string token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(result.Value!.EmailConfirmationToken));
        string link = Url.Page("/Account/ConfirmEmail", null, new { accountId = result.Value.AccountId.ToString(), token }, Request.Scheme)!;
        await sendConfirmation.ExecuteAsync(new SendConfirmationEmailCommand(Input.EmailAddress.Trim(), link), HttpContext.RequestAborted);
        return RedirectToPage("/Account/RegisterConfirmation");
    }

    public sealed class RegisterInput
    {
        [Required(ErrorMessage = "Enter a user name.")]
        [Display(Name = "User name")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a nickname.")]
        [Display(Name = "Nickname")]
        public string Nickname { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter an email address."), EmailAddress(ErrorMessage = "The email address format is invalid.")]
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a password."), DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Password (confirm)")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
