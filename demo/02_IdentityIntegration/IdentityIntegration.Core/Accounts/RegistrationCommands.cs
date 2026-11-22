using D3Fragments;
using D3Fragments.Application;
using D3Fragments.Extensions;

using IdentityIntegration.Core.Members;

namespace IdentityIntegration.Core.Accounts;

// Identity のユーザー（認証）と会員（業務上の利用者）を、同じ UnitOfWork で登録する。どちらかだけが保存されることはない
public sealed record RegisterAccountCommand(string UserName, string EmailAddress, string Password, string Nickname) : ICommand<Result<RegisteredAccount>>;

public sealed class RegisterAccountCommandHandler(IAccountManager accounts, IMemberRepository members, TimeProvider timeProvider)
    : IRequestHandler<RegisterAccountCommand, Result<RegisteredAccount>>
{
    public async Task<Result<RegisteredAccount>> HandleAsync(RegisterAccountCommand request, CancellationToken cancellationToken)
    {
        // 失敗を Result で返しても UnitOfWork は保存するため、Identity のユーザーを作る前に会員の入力を検証する
        Result<Nickname> nickname = Nickname.TryCreate(request.Nickname);
        if (nickname.IsFailure)
        {
            return Result<RegisteredAccount>.Failure(nickname.Errors);
        }

        // Identity の検証（ユーザー名の重複・パスワードの規則など）で失敗した場合、ユーザーは追加されない
        Result<RegisteredAccount> account = await accounts.RegisterAsync(request.UserName, request.EmailAddress, request.Password);
        if (account.IsSuccess)
        {
            members.Add(Member.TryRegister(account.Value!.AccountId, request.Nickname, timeProvider.GetLocalToday()).GetValueOrThrow());
        }
        return account;
    }
}

// 確認メールは登録の Commit 後に送る（登録が保存されなかったのにメールだけ届くことがないように）
public sealed record SendConfirmationEmailCommand(string EmailAddress, string ConfirmationLink) : ICommand<Result>;

public sealed class SendConfirmationEmailCommandHandler(IAccountEmailSender sender) : IRequestHandler<SendConfirmationEmailCommand, Result>
{
    public async Task<Result> HandleAsync(SendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        await sender.SendConfirmationLinkAsync(request.EmailAddress, request.ConfirmationLink, cancellationToken);
        return Result.Success();
    }
}

public sealed record ConfirmEmailCommand(UuidV7 AccountId, string Token) : ICommand<Result>;

public sealed class ConfirmEmailCommandHandler(IAccountManager accounts) : IRequestHandler<ConfirmEmailCommand, Result>
{
    public Task<Result> HandleAsync(ConfirmEmailCommand request, CancellationToken cancellationToken) =>
        accounts.ConfirmEmailAsync(request.AccountId, request.Token);
}

public sealed record RegisterWithExternalLoginCommand(
    string UserName, string EmailAddress, string Nickname, string LoginProvider, string ProviderKey, string ProviderDisplayName)
    : ICommand<Result<UuidV7>>;

public sealed class RegisterWithExternalLoginCommandHandler(IAccountManager accounts, IMemberRepository members, TimeProvider timeProvider)
    : IRequestHandler<RegisterWithExternalLoginCommand, Result<UuidV7>>
{
    public async Task<Result<UuidV7>> HandleAsync(RegisterWithExternalLoginCommand request, CancellationToken cancellationToken)
    {
        Result<Nickname> nickname = Nickname.TryCreate(request.Nickname);
        if (nickname.IsFailure)
        {
            return Result<UuidV7>.Failure(nickname.Errors);
        }

        Result<UuidV7> accountId = await accounts.RegisterWithExternalLoginAsync(
            request.UserName, request.EmailAddress, request.LoginProvider, request.ProviderKey, request.ProviderDisplayName);
        if (accountId.IsSuccess)
        {
            members.Add(Member.TryRegister(accountId.Value, request.Nickname, timeProvider.GetLocalToday()).GetValueOrThrow());
        }
        return accountId;
    }
}
