using D3Fragments;
using D3Fragments.Application;

using IdentityIntegration.Core.Accounts;
using IdentityIntegration.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace IdentityIntegration.Web.Pages.MyPage;

public sealed class PasskeysModel(
    SignInManager<UserAccount> signIn,
    IRequestExecutor<AddPasskeyCommand, Result> addPasskey,
    IRequestExecutor<RemovePasskeyCommand, Result> removePasskey) : AppPageModel(signIn)
{
    public IReadOnlyList<UserPasskeyInfo> Passkeys { get; private set; } = [];

    public async Task OnGetAsync() => await LoadAsync();

    // 登録（1/2）: ブラウザに渡すオプションを作る。チャレンジは Cookie に保存される（DB は変更しない）
    public async Task<IActionResult> OnPostCreationOptionsAsync()
    {
        UserAccount account = await GetCurrentAccountAsync();
        string options = await SignInManager.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity
        {
            Id = account.Id.ToString(),
            Name = account.UserName!,
            DisplayName = account.UserName!,
        });
        return Content(options, "application/json");
    }

    // 登録（2/2）: ブラウザが作ったパスキーを検証し、Command で保存する
    public async Task<IActionResult> OnPostAddAsync(string? credentialJson, string? name)
    {
        UserAccount account = await GetCurrentAccountAsync();
        if (string.IsNullOrEmpty(credentialJson))
        {
            ModelState.AddModelError(string.Empty, "パスキーの登録が取り消されました。");
            await LoadAsync();
            return Page();
        }

        // 署名とオリジンの検証（DB は変更しない）
        PasskeyAttestationResult attestation = await SignInManager.PerformPasskeyAttestationAsync(credentialJson);
        if (!attestation.Succeeded)
        {
            ModelState.AddModelError(string.Empty, $"パスキーの登録に失敗しました。（{attestation.Failure.Message}）");
            await LoadAsync();
            return Page();
        }

        UserPasskeyInfo passkey = attestation.Passkey;
        Result result = await addPasskey.ExecuteAsync(new AddPasskeyCommand(account.Id, new PasskeyCredential(
            passkey.CredentialId, passkey.PublicKey, passkey.CreatedAt, passkey.SignCount, passkey.Transports,
            passkey.IsUserVerified, passkey.IsBackupEligible, passkey.IsBackedUp, passkey.AttestationObject, passkey.ClientDataJson,
            string.IsNullOrWhiteSpace(name) ? "パスキー" : name.Trim())), HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            AddErrors(result);
            await LoadAsync();
            return Page();
        }

        TempData["Success"] = "パスキーを登録しました。ログイン画面の「パスキーでログイン」で使えます。";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(string credentialId)
    {
        UserAccount account = await GetCurrentAccountAsync();
        await removePasskey.ExecuteAsync(new RemovePasskeyCommand(account.Id, WebEncoders.Base64UrlDecode(credentialId)), HttpContext.RequestAborted);
        // パスキーの削除では security stamp が変わらないため、そのパスキーでログインした他のセッションは切れない
        TempData["Success"] = "パスキーを削除しました。";
        return RedirectToPage();
    }

    private async Task LoadAsync() => Passkeys = [.. await Users.GetPasskeysAsync(await GetCurrentAccountAsync())];
}
