using System.Text;

using D3Fragments;
using D3Fragments.Application;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityIntegration.Web.Pages.MyPage;

public sealed class EnableAuthenticatorModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<PrepareAuthenticatorKeyCommand, string> prepareKey,
    IRequestExecutor<EnableAuthenticatorCommand, Result<IReadOnlyList<string>>> enable) : AppPageModel(signIn)
{
    private const string Issuer = "IdentityIntegration";

    [BindProperty]
    public string Code { get; set; } = string.Empty;

    public string SharedKey { get; private set; } = string.Empty;

    public string AuthenticatorUri { get; private set; } = string.Empty;

    public async Task OnGetAsync() => await LoadAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ModelState.AddModelError(nameof(Code), "確認コードを入力してください。");
            await LoadAsync();
            return Page();
        }

        UserAccount account = await GetCurrentAccountAsync();
        Result<IReadOnlyList<string>> result = await enable.ExecuteAsync(new EnableAuthenticatorCommand(account.Id, Code), HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            AddErrors(result);
            await LoadAsync();
            return Page();
        }

        // 二要素認証の有効化で security stamp が変わるため、自分の Cookie を作り直す
        await SignInManager.RefreshSignInAsync(account);
        TempData["Success"] = "認証アプリを設定し、二要素認証を有効にしました。";
        if (result.Value!.Count == 0)
        {
            return RedirectToPage("/MyPage/TwoFactor");
        }
        TempData["RecoveryCodes"] = string.Join(' ', result.Value);
        return RedirectToPage("/MyPage/RecoveryCodes");
    }

    private async Task LoadAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        // キーがまだなければ作る（DB への書き込みのため Command で行う。security stamp も変わる）
        string key = await prepareKey.ExecuteAsync(new PrepareAuthenticatorKeyCommand(account.Id), HttpContext.RequestAborted);
        await SignInManager.RefreshSignInAsync(account);

        SharedKey = FormatKey(key);
        string label = Uri.EscapeDataString($"{Issuer}:{account.UserName}");
        AuthenticatorUri = $"otpauth://totp/{label}?secret={key}&issuer={Uri.EscapeDataString(Issuer)}&digits=6";
    }

    // 手入力しやすいよう4文字ずつ区切る
    private static string FormatKey(string key)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < key.Length; i += 4)
        {
            builder.Append(key.AsSpan(i, Math.Min(4, key.Length - i))).Append(' ');
        }
        return builder.ToString().TrimEnd().ToLowerInvariant();
    }
}
