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
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.MemberTypeKey)}", "会員種別を選択してください。");
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
            TempData["Success"] = "会員登録が完了しました。メールアドレスでログインしてください。";
            return RedirectToPage("/Account/Login");
        });
    }

    public sealed class MemberInput
    {
        [Required(ErrorMessage = "氏名を入力してください。"), StringLength(50, ErrorMessage = "氏名は50文字以内で入力してください。"), Display(Name = "氏名")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "メールアドレスを入力してください。"), EmailAddress(ErrorMessage = "メールアドレスの形式が不正です。"), Display(Name = "メールアドレス")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "会員種別を選択してください。"), Display(Name = "会員種別")]
        public int? MemberTypeKey { get; set; }
    }
}
