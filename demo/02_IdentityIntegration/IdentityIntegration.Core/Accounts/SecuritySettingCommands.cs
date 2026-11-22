using D3Fragments;
using D3Fragments.Application;

namespace IdentityIntegration.Core.Accounts;

// 二要素認証とパスキーの設定。security stamp が変わる操作の後は、プレゼンテーション層で Cookie を更新する（RefreshSignInAsync）

public sealed record PrepareAuthenticatorKeyCommand(UuidV7 AccountId) : ICommand<string>;

public sealed class PrepareAuthenticatorKeyCommandHandler(IAccountManager accounts) : IRequestHandler<PrepareAuthenticatorKeyCommand, string>
{
    public Task<string> HandleAsync(PrepareAuthenticatorKeyCommand request, CancellationToken cancellationToken) =>
        accounts.PrepareAuthenticatorKeyAsync(request.AccountId);
}

public sealed record EnableAuthenticatorCommand(UuidV7 AccountId, string Code) : ICommand<Result<IReadOnlyList<string>>>;

public sealed class EnableAuthenticatorCommandHandler(IAccountManager accounts) : IRequestHandler<EnableAuthenticatorCommand, Result<IReadOnlyList<string>>>
{
    public Task<Result<IReadOnlyList<string>>> HandleAsync(EnableAuthenticatorCommand request, CancellationToken cancellationToken) =>
        accounts.EnableAuthenticatorAsync(request.AccountId, request.Code);
}

public sealed record EnableEmailTwoFactorCommand(UuidV7 AccountId) : ICommand<IReadOnlyList<string>>;

public sealed class EnableEmailTwoFactorCommandHandler(IAccountManager accounts) : IRequestHandler<EnableEmailTwoFactorCommand, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> HandleAsync(EnableEmailTwoFactorCommand request, CancellationToken cancellationToken) =>
        accounts.EnableEmailTwoFactorAsync(request.AccountId);
}

public sealed record DisableTwoFactorCommand(UuidV7 AccountId) : ICommand<Result>;

public sealed class DisableTwoFactorCommandHandler(IAccountManager accounts) : IRequestHandler<DisableTwoFactorCommand, Result>
{
    public async Task<Result> HandleAsync(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        await accounts.DisableTwoFactorAsync(request.AccountId);
        return Result.Success();
    }
}

public sealed record RegenerateRecoveryCodesCommand(UuidV7 AccountId) : ICommand<IReadOnlyList<string>>;

public sealed class RegenerateRecoveryCodesCommandHandler(IAccountManager accounts) : IRequestHandler<RegenerateRecoveryCodesCommand, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> HandleAsync(RegenerateRecoveryCodesCommand request, CancellationToken cancellationToken) =>
        accounts.RegenerateRecoveryCodesAsync(request.AccountId);
}

// パスキーの検証（署名・オリジンの確認）はプレゼンテーション層で済ませ、検証済みの内容を保存する
public sealed record AddPasskeyCommand(UuidV7 AccountId, PasskeyCredential Passkey) : ICommand<Result>;

public sealed class AddPasskeyCommandHandler(IAccountManager accounts) : IRequestHandler<AddPasskeyCommand, Result>
{
    public Task<Result> HandleAsync(AddPasskeyCommand request, CancellationToken cancellationToken) =>
        accounts.AddPasskeyAsync(request.AccountId, request.Passkey);
}

public sealed record RemovePasskeyCommand(UuidV7 AccountId, byte[] CredentialId) : ICommand<Result>;

public sealed class RemovePasskeyCommandHandler(IAccountManager accounts) : IRequestHandler<RemovePasskeyCommand, Result>
{
    public async Task<Result> HandleAsync(RemovePasskeyCommand request, CancellationToken cancellationToken)
    {
        await accounts.RemovePasskeyAsync(request.AccountId, request.CredentialId);
        return Result.Success();
    }
}
