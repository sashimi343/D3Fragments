using D3Fragments;

namespace IdentityIntegration.Core.Accounts;

public enum SignInStatus
{
    Succeeded,
    RequiresTwoFactor,
    LockedOut,
    NotAllowed,
    Failed,
    // No account is linked to the external login
    NotRegistered,
}

// The result of a sign-in. Failures are also returned as a result, not an exception (if a Command ends with an exception, the UnitOfWork also discards the recorded access failure count)
public sealed record SignInOutcome(SignInStatus Status, UuidV7? AccountId = null)
{
    public static SignInOutcome Failed { get; } = new(SignInStatus.Failed);
}
