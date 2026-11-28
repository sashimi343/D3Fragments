using D3Fragments;
using D3Fragments.Identity.EFCore;

using IdentityIntegration.Core.Accounts;

using Microsoft.AspNetCore.Identity;

namespace IdentityIntegration.Infrastructure.Identity;

// Implements the Core layer interfaces with ASP.NET Core Identity. Called inside Commands, and UnitOfWorkBehavior saves the changes.
// SignInManager's Verify* methods are extension methods of D3Fragments.Identity.EFCore that only do the database side without touching cookies (DFR0017)
internal sealed class IdentityAccountService(UserManager<UserAccount> users, SignInManager<UserAccount> signIn) : IAccountAuthenticator, IAccountManager
{
    private const int RecoveryCodeCount = 10;

    public async Task<SignInOutcome> SignInWithPasswordAsync(string userName, string password, string? rememberedAccountId)
    {
        if (await users.FindByNameAsync(userName) is not { } account)
        {
            return SignInOutcome.Failed;
        }
        SignInResult result = await signIn.VerifyPasswordAsync(account, password, lockoutOnFailure: true, IsRemembered(account, rememberedAccountId));
        return ToOutcome(result, account);
    }

    public async Task<SignInOutcome> SignInWithTwoFactorCodeAsync(UuidV7 accountId, TwoFactorMethod method, string code)
    {
        UserAccount account = await GetAsync(accountId);
        string provider = method == TwoFactorMethod.Authenticator ? users.Options.Tokens.AuthenticatorTokenProvider : TokenOptions.DefaultEmailProvider;
        SignInResult result = await signIn.VerifyTwoFactorCodeAsync(account, provider, Normalize(code));
        return ToOutcome(result, account);
    }

    public async Task<SignInOutcome> SignInWithRecoveryCodeAsync(UuidV7 accountId, string recoveryCode)
    {
        UserAccount account = await GetAsync(accountId);
        SignInResult result = await signIn.VerifyRecoveryCodeAsync(account, recoveryCode.Replace(" ", string.Empty, StringComparison.Ordinal));
        return ToOutcome(result, account);
    }

    public async Task<SignInOutcome> SignInWithPasskeyAsync(UuidV7 accountId, byte[] credentialId, uint signCount, bool isBackedUp)
    {
        UserAccount account = await GetAsync(accountId);
        SignInResult result = await signIn.VerifyPasskeyAsync(account, credentialId, signCount, isBackedUp);
        return ToOutcome(result, account);
    }

    public async Task<SignInOutcome> SignInWithExternalLoginAsync(string loginProvider, string providerKey, string? rememberedAccountId)
    {
        if (await users.FindByLoginAsync(loginProvider, providerKey) is not { } account)
        {
            return new SignInOutcome(SignInStatus.NotRegistered);
        }
        SignInResult result = await signIn.VerifyExternalLoginAsync(account, IsRemembered(account, rememberedAccountId));
        return ToOutcome(result, account);
    }

    public async Task<EmailCode> GenerateEmailTwoFactorCodeAsync(UuidV7 accountId)
    {
        UserAccount account = await GetAsync(accountId);
        string code = await users.GenerateTwoFactorTokenAsync(account, TokenOptions.DefaultEmailProvider);
        return new EmailCode(account.Email!, code);
    }

    public async Task<Result<RegisteredAccount>> RegisterAsync(string userName, string emailAddress, string password)
    {
        var account = new UserAccount { UserName = userName, Email = emailAddress };
        Result created = (await users.CreateAsync(account, password)).ToResult();
        if (created.IsFailure)
        {
            return Result<RegisteredAccount>.Failure(created.Errors);
        }
        string token = await users.GenerateEmailConfirmationTokenAsync(account);
        return new RegisteredAccount(account.Id, token);
    }

    public async Task<Result> ConfirmEmailAsync(UuidV7 accountId, string token)
    {
        if (await users.FindByIdAsync(accountId.ToString()) is not { } account)
        {
            return Result.Failure(Error.NotFound("The account was not found."));
        }
        return (await users.ConfirmEmailAsync(account, token)).ToResult(_ => Error.Validation("The confirmation link is invalid or has expired."));
    }

    public async Task<Result<UuidV7>> RegisterWithExternalLoginAsync(string userName, string emailAddress, string loginProvider, string providerKey, string providerDisplayName)
    {
        // The email address is treated as already confirmed by the external identity provider
        var account = new UserAccount { UserName = userName, Email = emailAddress, EmailConfirmed = true };
        Result created = (await users.CreateAsync(account)).ToResult();
        if (created.IsFailure)
        {
            return Result<UuidV7>.Failure(created.Errors);
        }
        // A failure after creating the user would make the UnitOfWork save only the user if returned as a Result, so throw an exception to discard the changes
        Result linked = (await users.AddLoginAsync(account, new UserLoginInfo(loginProvider, providerKey, providerDisplayName))).ToResult();
        return linked.IsFailure ? throw new DomainException(linked.Errors[0]) : account.Id;
    }

    public async Task<string> PrepareAuthenticatorKeyAsync(UuidV7 accountId)
    {
        UserAccount account = await GetAsync(accountId);
        if (string.IsNullOrEmpty(await users.GetAuthenticatorKeyAsync(account)))
        {
            await users.ResetAuthenticatorKeyAsync(account);
        }
        return (await users.GetAuthenticatorKeyAsync(account))!;
    }

    public async Task<Result<IReadOnlyList<string>>> EnableAuthenticatorAsync(UuidV7 accountId, string code)
    {
        UserAccount account = await GetAsync(accountId);
        if (!await users.VerifyTwoFactorTokenAsync(account, users.Options.Tokens.AuthenticatorTokenProvider, Normalize(code)))
        {
            return Result<IReadOnlyList<string>>.Failure(Error.Validation("The verification code is invalid.", target: "Code"));
        }
        return Result<IReadOnlyList<string>>.Success(await EnableTwoFactorAsync(account));
    }

    public async Task<IReadOnlyList<string>> EnableEmailTwoFactorAsync(UuidV7 accountId) =>
        await EnableTwoFactorAsync(await GetAsync(accountId));

    public async Task DisableTwoFactorAsync(UuidV7 accountId) =>
        await users.SetTwoFactorEnabledAsync(await GetAsync(accountId), false);

    public async Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(UuidV7 accountId) =>
        [.. await users.GenerateNewTwoFactorRecoveryCodesAsync(await GetAsync(accountId), RecoveryCodeCount) ?? []];

    public async Task<Result> AddPasskeyAsync(UuidV7 accountId, PasskeyCredential passkey)
    {
        UserAccount account = await GetAsync(accountId);
        var info = new UserPasskeyInfo(
            passkey.CredentialId,
            passkey.PublicKey,
            passkey.CreatedAt,
            passkey.SignCount,
            passkey.Transports,
            passkey.IsUserVerified,
            passkey.IsBackupEligible,
            passkey.IsBackedUp,
            passkey.AttestationObject,
            passkey.ClientDataJson)
        {
            Name = passkey.Name,
        };
        return (await users.AddOrUpdatePasskeyAsync(account, info)).ToResult();
    }

    public async Task RemovePasskeyAsync(UuidV7 accountId, byte[] credentialId) =>
        await users.RemovePasskeyAsync(await GetAsync(accountId), credentialId);

    // Enables two-factor authentication and creates recovery codes if there are none yet
    private async Task<IReadOnlyList<string>> EnableTwoFactorAsync(UserAccount account)
    {
        await users.SetTwoFactorEnabledAsync(account, true);
        return await users.CountRecoveryCodesAsync(account) == 0
            ? [.. await users.GenerateNewTwoFactorRecoveryCodesAsync(account, RecoveryCodeCount) ?? []]
            : [];
    }

    private async Task<UserAccount> GetAsync(UuidV7 accountId) =>
        await users.FindByIdAsync(accountId.ToString()) ?? throw new DomainException(Error.NotFound("The account was not found."));

    private static bool IsRemembered(UserAccount account, string? rememberedAccountId) => rememberedAccountId == account.Id.ToString();

    // Removes spaces and hyphens entered to match how the authenticator app displays the code
    private static string Normalize(string code) => code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

    private static SignInOutcome ToOutcome(SignInResult result, UserAccount account)
    {
        SignInStatus status = result switch
        {
            { Succeeded: true } => SignInStatus.Succeeded,
            { RequiresTwoFactor: true } => SignInStatus.RequiresTwoFactor,
            { IsLockedOut: true } => SignInStatus.LockedOut,
            { IsNotAllowed: true } => SignInStatus.NotAllowed,
            _ => SignInStatus.Failed,
        };
        return new SignInOutcome(status, account.Id);
    }
}
