using System.ComponentModel.DataAnnotations;

using BasicWebApp.Application.Members;
using BasicWebApp.Domain.Members;

using D3Fragments.Application;

using Microsoft.AspNetCore.Mvc;

namespace BasicWebApp.Web.Pages.Members;

public sealed class RegisterModel(IRequestExecutor<RegisterMemberCommand, Member> registerMember, PageErrorHandler errors) : AppPageModel(errors)
{
    [BindProperty]
    public MemberInput Input { get; set; } = new();

    public Task<IActionResult> OnPostAsync()
    {
        MemberType? memberType = null;
        if (Input.MemberTypeKey is int key && !MemberType.TryParse(key, out memberType))
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.MemberTypeKey)}", "Select a member type.");
        }
        if (!ModelState.IsValid || memberType is null)
        {
            return Task.FromResult<IActionResult>(InvalidInput());
        }
        return ExecuteWithErrorsAsync(async () =>
        {
            await registerMember.ExecuteAsync(new RegisterMemberCommand
            {
                MemberName = Input.Name,
                EmailAddress = Input.EmailAddress,
                MemberType = memberType,
            }, HttpContext.RequestAborted);
            TempData["Success"] = "Registration is complete. Sign in with your email address.";
            return RedirectToPage("/Account/Login");
        });
    }

    public sealed class MemberInput
    {
        [Required(ErrorMessage = "Enter a name."), StringLength(50, ErrorMessage = "The name must be 50 characters or fewer."), Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter an email address."), EmailAddress(ErrorMessage = "The email address format is invalid."), Display(Name = "Email address")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Select a member type."), Display(Name = "Member type")]
        public int? MemberTypeKey { get; set; }
    }
}
