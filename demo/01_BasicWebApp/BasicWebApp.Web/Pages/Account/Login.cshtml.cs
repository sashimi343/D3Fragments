using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

using BasicWebApp.Application.Members;
using BasicWebApp.Domain.Members;

using D3Fragments.Application;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebApp.Web.Pages.Account;

public sealed class LoginModel(IRequestExecutor<FindMemberByEmailAddressQuery, Member?> findMember, PageErrorHandler errors) : AppPageModel(errors)
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public Task<IActionResult> OnPostAsync(string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<IActionResult>(InvalidInput());
        }
        return ExecuteWithErrorsAsync(async () =>
        {
            Member? member = await findMember.ExecuteAsync(new FindMemberByEmailAddressQuery { EmailAddress = Input.EmailAddress.Trim() }, HttpContext.RequestAborted);
            if (member is null)
            {
                ModelState.AddModelError(string.Empty, "This email address is not registered.");
                return InvalidInput();
            }
            Claim[] claims =
            [
                new(ClaimTypes.NameIdentifier, member.Id.ToString()),
                new(ClaimTypes.Email, member.EmailAddress.Value),
            ];
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToPage("/Members/Index");
        });
    }

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "Enter an email address."), EmailAddress(ErrorMessage = "The email address format is invalid.")]
        [Display(Name = "Email address")]
        public string EmailAddress { get; set; } = string.Empty;
    }
}
