using D3Fragments;
using D3Fragments.Application;

namespace IdentityIntegration.Core.Accounts;

// サインインの Command は DB 側の処理（アクセス失敗回数・ロックアウト・回復コードの消費・パスキーの署名回数）だけを行う。
// Cookie の発行は、Commit 後にプレゼンテーション層が行う

public sealed record SignInWithPasswordCommand(string UserName, string Password, string? RememberedAccountId) : ICommand<SignInOutcome>;

public sealed class SignInWithPasswordCommandHandler(IAccountAuthenticator authenticator) : IRequestHandler<SignInWithPasswordCommand, SignInOutcome>
{
    public Task<SignInOutcome> HandleAsync(SignInWithPasswordCommand request, CancellationToken cancellationToken) =>
        authenticator.SignInWithPasswordAsync(request.UserName, request.Password, request.RememberedAccountId);
}

public sealed record SignInWithTwoFactorCodeCommand(UuidV7 AccountId, TwoFactorMethod Method, string Code) : ICommand<SignInOutcome>;

public sealed class SignInWithTwoFactorCodeCommandHandler(IAccountAuthenticator authenticator) : IRequestHandler<SignInWithTwoFactorCodeCommand, SignInOutcome>
{
    public Task<SignInOutcome> HandleAsync(SignInWithTwoFactorCodeCommand request, CancellationToken cancellationToken) =>
        authenticator.SignInWithTwoFactorCodeAsync(request.AccountId, request.Method, request.Code);
}

public sealed record SignInWithRecoveryCodeCommand(UuidV7 AccountId, string RecoveryCode) : ICommand<SignInOutcome>;

public sealed class SignInWithRecoveryCodeCommandHandler(IAccountAuthenticator authenticator) : IRequestHandler<SignInWithRecoveryCodeCommand, SignInOutcome>
{
    public Task<SignInOutcome> HandleAsync(SignInWithRecoveryCodeCommand request, CancellationToken cancellationToken) =>
        authenticator.SignInWithRecoveryCodeAsync(request.AccountId, request.RecoveryCode);
}

public sealed record SignInWithPasskeyCommand(UuidV7 AccountId, byte[] CredentialId, uint SignCount, bool IsBackedUp) : ICommand<SignInOutcome>;

public sealed class SignInWithPasskeyCommandHandler(IAccountAuthenticator authenticator) : IRequestHandler<SignInWithPasskeyCommand, SignInOutcome>
{
    public Task<SignInOutcome> HandleAsync(SignInWithPasskeyCommand request, CancellationToken cancellationToken) =>
        authenticator.SignInWithPasskeyAsync(request.AccountId, request.CredentialId, request.SignCount, request.IsBackedUp);
}

public sealed record SignInWithExternalLoginCommand(string LoginProvider, string ProviderKey, string? RememberedAccountId) : ICommand<SignInOutcome>;

public sealed class SignInWithExternalLoginCommandHandler(IAccountAuthenticator authenticator) : IRequestHandler<SignInWithExternalLoginCommand, SignInOutcome>
{
    public Task<SignInOutcome> HandleAsync(SignInWithExternalLoginCommand request, CancellationToken cancellationToken) =>
        authenticator.SignInWithExternalLoginAsync(request.LoginProvider, request.ProviderKey, request.RememberedAccountId);
}

// ponytail: 送信回数の制限はしない。公開するアプリでは、ASP.NET Core のレート制限などでアプリが制限する
public sealed record SendTwoFactorEmailCodeCommand(UuidV7 AccountId) : ICommand<Result>;

public sealed class SendTwoFactorEmailCodeCommandHandler(IAccountAuthenticator authenticator, IAccountEmailSender sender)
    : IRequestHandler<SendTwoFactorEmailCodeCommand, Result>
{
    public async Task<Result> HandleAsync(SendTwoFactorEmailCodeCommand request, CancellationToken cancellationToken)
    {
        EmailCode code = await authenticator.GenerateEmailTwoFactorCodeAsync(request.AccountId);
        await sender.SendTwoFactorCodeAsync(code.EmailAddress, code.Code, cancellationToken);
        return Result.Success();
    }
}
