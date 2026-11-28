using D3Fragments;

namespace IdentityIntegration.Core.Accounts;

public enum TwoFactorMethod
{
    Authenticator,
    Email,
}

// Sign-in verification (the database side). Implemented in the Infrastructure layer (ASP.NET Core Identity)
public interface IAccountAuthenticator
{
    // rememberedAccountId: the account ID recorded in the "remember this browser" cookie
    Task<SignInOutcome> SignInWithPasswordAsync(string userName, string password, string? rememberedAccountId);

    Task<SignInOutcome> SignInWithTwoFactorCodeAsync(UuidV7 accountId, TwoFactorMethod method, string code);

    Task<SignInOutcome> SignInWithRecoveryCodeAsync(UuidV7 accountId, string recoveryCode);

    // Passkey verification (checking the signature) is done in the presentation layer, and its result is applied here
    Task<SignInOutcome> SignInWithPasskeyAsync(UuidV7 accountId, byte[] credentialId, uint signCount, bool isBackedUp);

    Task<SignInOutcome> SignInWithExternalLoginAsync(string loginProvider, string providerKey, string? rememberedAccountId);

    Task<EmailCode> GenerateEmailTwoFactorCodeAsync(UuidV7 accountId);
}

public sealed record EmailCode(string EmailAddress, string Code);
