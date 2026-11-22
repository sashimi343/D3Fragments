using D3Fragments;

namespace IdentityIntegration.Core.Accounts;

// アカウントの登録と、認証の設定の変更。実装は Infrastructure 層（ASP.NET Core Identity）
public interface IAccountManager
{
    Task<Result<RegisteredAccount>> RegisterAsync(string userName, string emailAddress, string password);

    Task<Result> ConfirmEmailAsync(UuidV7 accountId, string token);

    Task<Result<UuidV7>> RegisterWithExternalLoginAsync(string userName, string emailAddress, string loginProvider, string providerKey, string providerDisplayName);

    // 認証アプリに登録するキー。まだなければ作る
    Task<string> PrepareAuthenticatorKeyAsync(UuidV7 accountId);

    // 二要素認証を有効にする。回復コードがなければ作って返す
    Task<Result<IReadOnlyList<string>>> EnableAuthenticatorAsync(UuidV7 accountId, string code);

    Task<IReadOnlyList<string>> EnableEmailTwoFactorAsync(UuidV7 accountId);

    Task DisableTwoFactorAsync(UuidV7 accountId);

    Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(UuidV7 accountId);

    Task<Result> AddPasskeyAsync(UuidV7 accountId, PasskeyCredential passkey);

    Task RemovePasskeyAsync(UuidV7 accountId, byte[] credentialId);
}

public sealed record RegisteredAccount(UuidV7 AccountId, string EmailConfirmationToken);

// 登録するパスキー（Identity の UserPasskeyInfo と同じ内容）
public sealed record PasskeyCredential(
    byte[] CredentialId,
    byte[] PublicKey,
    DateTimeOffset CreatedAt,
    uint SignCount,
    string[]? Transports,
    bool IsUserVerified,
    bool IsBackupEligible,
    bool IsBackedUp,
    byte[] AttestationObject,
    byte[] ClientDataJson,
    string? Name);
