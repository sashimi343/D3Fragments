using D3Fragments;
using D3Fragments.Application;
using D3Fragments.AspNetCore.Identity;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.Account;

public sealed class LoginWith2faModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<SignInWithTwoFactorCodeCommand, SignInOutcome> signInWithCode,
    IRequestExecutor<SendTwoFactorEmailCodeCommand, Result> sendEmailCode) : AppPageModel(signIn)
{
    [BindProperty(SupportsGet = true)]
    public TwoFactorMethod Method { get; set; } = TwoFactorMethod.Authenticator;

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMachine { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool RememberMe { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IReadOnlyList<TwoFactorMethod> Methods { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync() =>
        await LoadAsync() is null ? RedirectToPage("/Account/Login") : Page();

    // メールで確認コードを送る
    public async Task<IActionResult> OnPostSendEmailCodeAsync()
    {
        if (await LoadAsync() is not { } account)
        {
            return RedirectToPage("/Account/Login");
        }
        ModelState.Clear();
        await sendEmailCode.ExecuteAsync(new SendTwoFactorEmailCodeCommand(account.Id), HttpContext.RequestAborted);
        TempData["Success"] = "確認コードをメールで送信しました。";
        return RedirectToPage(new { RememberMe, ReturnUrl, method = TwoFactorMethod.Email });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // 二要素認証を待っているアカウント（1要素目の後に BeginTwoFactorSignInAsync が発行した Cookie から読み取る）
        if (await LoadAsync() is not { } account)
        {
            return RedirectToPage("/Account/Login");
        }
        if (string.IsNullOrWhiteSpace(Code))
        {
            ModelState.AddModelError(nameof(Code), "確認コードを入力してください。");
            return Page();
        }

        // DB 側: コードの検証と、アクセス失敗回数の記録・リセット
        SignInOutcome outcome = await signInWithCode.ExecuteAsync(new SignInWithTwoFactorCodeCommand(account.Id, Method, Code), HttpContext.RequestAborted);
        if (outcome.Status != SignInStatus.Succeeded)
        {
            AddSignInError(outcome);
            return Page();
        }

        // Cookie 側: 二要素認証待ちの Cookie を消してサインインし、選んだ場合はこのブラウザを記憶する
        await SignInManager.CompleteTwoFactorSignInAsync(account, RememberMe, RememberMachine);
        return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/MyPage");
    }

    private async Task<UserAccount?> LoadAsync()
    {
        if (await SignInManager.GetTwoFactorAuthenticationUserAsync() is not { } account)
        {
            return null;
        }
        // 認証アプリはキーを登録済みの場合、メールは確認済みの場合に使える
        IList<string> providers = await Users.GetValidTwoFactorProvidersAsync(account);
        Methods = [.. providers.Contains(Users.Options.Tokens.AuthenticatorTokenProvider) ? [TwoFactorMethod.Authenticator] : Array.Empty<TwoFactorMethod>(),
            .. providers.Contains(TokenOptions.DefaultEmailProvider) ? [TwoFactorMethod.Email] : Array.Empty<TwoFactorMethod>()];
        if (!Methods.Contains(Method) && Methods.Count > 0)
        {
            Method = Methods[0];
        }
        return account;
    }
}
