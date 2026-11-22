using D3Fragments;

namespace IdentityIntegration.Core.Accounts;

public enum TwoFactorMethod
{
    Authenticator,
    Email,
}

// サインインの検証（DB 側の処理）。実装は Infrastructure 層（ASP.NET Core Identity）
public interface IAccountAuthenticator
{
    // rememberedAccountId: 「このブラウザを記憶する」の Cookie に記録されたアカウントの ID
    Task<SignInOutcome> SignInWithPasswordAsync(string userName, string password, string? rememberedAccountId);

    Task<SignInOutcome> SignInWithTwoFactorCodeAsync(UuidV7 accountId, TwoFactorMethod method, string code);

    Task<SignInOutcome> SignInWithRecoveryCodeAsync(UuidV7 accountId, string recoveryCode);

    // パスキーの検証（署名の確認）はプレゼンテーション層で済ませ、その結果を反映する
    Task<SignInOutcome> SignInWithPasskeyAsync(UuidV7 accountId, byte[] credentialId, uint signCount, bool isBackedUp);

    Task<SignInOutcome> SignInWithExternalLoginAsync(string loginProvider, string providerKey, string? rememberedAccountId);

    Task<EmailCode> GenerateEmailTwoFactorCodeAsync(UuidV7 accountId);
}

public sealed record EmailCode(string EmailAddress, string Code);
