using D3Fragments;

namespace IdentityIntegration.Core.Accounts;

public enum SignInStatus
{
    Succeeded,
    RequiresTwoFactor,
    LockedOut,
    NotAllowed,
    Failed,
    // 外部ログインに紐づくアカウントがない
    NotRegistered,
}

// サインインの結果。失敗も例外ではなく結果で返す（Command が例外で終わると、UnitOfWork がアクセス失敗回数の記録まで破棄するため）
public sealed record SignInOutcome(SignInStatus Status, UuidV7? AccountId = null)
{
    public static SignInOutcome Failed { get; } = new(SignInStatus.Failed);
}
