using D3Fragments;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityIntegration.Web.Pages;

// UserManager は読み取り（Cookie の発行に使うユーザーの取得など）だけに使い、DB への書き込みは Command で行う
public abstract class AppPageModel(SignInManager<UserAccount> signIn) : PageModel
{
    protected SignInManager<UserAccount> SignInManager => signIn;

    protected UserManager<UserAccount> Users => signIn.UserManager;

    // ログイン中のアカウントの ID（Cookie のクレームから読み取る。DB は参照しない）
    protected UuidV7 CurrentAccountId => UuidV7.Parse(Users.GetUserId(User)!, null);

    // ログイン中のアカウント
    protected async Task<UserAccount> GetCurrentAccountAsync() =>
        await Users.GetUserAsync(User) ?? throw new InvalidOperationException("ログイン中のアカウントが見つかりません。");

    // Command が返したアカウントを取得する（Cookie の発行に使う）
    protected async Task<UserAccount> FindAccountAsync(UuidV7 accountId) =>
        await Users.FindByIdAsync(accountId.ToString()) ?? throw new InvalidOperationException("アカウントが見つかりません。");

    protected void AddErrors(Result result)
    {
        foreach (Error error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Message);
        }
    }

    // ログインできなかった理由の表示
    protected void AddSignInError(SignInOutcome outcome) =>
        ModelState.AddModelError(string.Empty, outcome.Status switch
        {
            SignInStatus.LockedOut => "ログインの失敗が続いたため、アカウントがロックされています。しばらくしてからやり直してください。",
            SignInStatus.NotAllowed => "メールアドレスの確認が済んでいません。確認メールのリンクを開いてください。",
            _ => "認証に失敗しました。入力内容を確認してください。",
        });
}
