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

        // 登録の Commit 後に確認メールを送る
        string token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(result.Value!.EmailConfirmationToken));
        string link = Url.Page("/Account/ConfirmEmail", null, new { accountId = result.Value.AccountId.ToString(), token }, Request.Scheme)!;
        await sendConfirmation.ExecuteAsync(new SendConfirmationEmailCommand(Input.EmailAddress.Trim(), link), HttpContext.RequestAborted);
        return RedirectToPage("/Account/RegisterConfirmation");
    }

    public sealed class RegisterInput
    {
        [Required(ErrorMessage = "ユーザー名を入力してください。")]
        [Display(Name = "ユーザー名")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ニックネームを入力してください。")]
        [Display(Name = "ニックネーム")]
        public string Nickname { get; set; } = string.Empty;

        [Required(ErrorMessage = "メールアドレスを入力してください。"), EmailAddress(ErrorMessage = "メールアドレスの形式が正しくありません。")]
        [Display(Name = "メールアドレス")]
        public string EmailAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "パスワードを入力してください。"), DataType(DataType.Password)]
        [Display(Name = "パスワード")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "パスワードが一致しません。")]
        [Display(Name = "パスワード（確認）")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
