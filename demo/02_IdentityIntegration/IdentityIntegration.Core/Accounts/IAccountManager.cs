using D3Fragments;

namespace IdentityIntegration.Core.Accounts;

// Account registration and changes to authentication settings. Implemented in the Infrastructure layer (ASP.NET Core Identity)
public interface IAccountManager
{
    Task<Result<RegisteredAccount>> RegisterAsync(string userName, string emailAddress, string password);

    Task<Result> ConfirmEmailAsync(UuidV7 accountId, string token);

    Task<Result<UuidV7>> RegisterWithExternalLoginAsync(string userName, string emailAddress, string loginProvider, string providerKey, string providerDisplayName);

    // The key to register in the authenticator app. Created if it does not exist yet
    Task<string> PrepareAuthenticatorKeyAsync(UuidV7 accountId);

    // Enables two-factor authentication. Creates and returns recovery codes if there are none
    Task<Result<IReadOnlyList<string>>> EnableAuthenticatorAsync(UuidV7 accountId, string code);

    Task<IReadOnlyList<string>> EnableEmailTwoFactorAsync(UuidV7 accountId);

    Task DisableTwoFactorAsync(UuidV7 accountId);

    Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(UuidV7 accountId);

    Task<Result> AddPasskeyAsync(UuidV7 accountId, PasskeyCredential passkey);

    Task RemovePasskeyAsync(UuidV7 accountId, byte[] credentialId);
}

public sealed record RegisteredAccount(UuidV7 AccountId, string EmailConfirmationToken);

// A passkey to register (the same content as Identity's UserPasskeyInfo)
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
