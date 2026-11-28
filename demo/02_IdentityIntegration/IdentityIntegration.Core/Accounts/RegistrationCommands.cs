using D3Fragments;
using D3Fragments.Application;
using D3Fragments.Extensions;

using IdentityIntegration.Core.Members;

namespace IdentityIntegration.Core.Accounts;

// Registers the Identity user (authentication) and the member (the business user) in the same UnitOfWork. One is never saved without the other
public sealed record RegisterAccountCommand(string UserName, string EmailAddress, string Password, string Nickname) : ICommand<Result<RegisteredAccount>>;

public sealed class RegisterAccountCommandHandler(IAccountManager accounts, IMemberRepository members, TimeProvider timeProvider)
    : IRequestHandler<RegisterAccountCommand, Result<RegisteredAccount>>
{
    public async Task<Result<RegisteredAccount>> HandleAsync(RegisterAccountCommand request, CancellationToken cancellationToken)
    {
        // The UnitOfWork saves changes even when a failure is returned as a Result, so validate the member input before creating the Identity user
        Result<Nickname> nickname = Nickname.TryCreate(request.Nickname);
        if (nickname.IsFailure)
        {
            return Result<RegisteredAccount>.Failure(nickname.Errors);
        }

        // If Identity validation (duplicate user name, password rules, etc.) fails, the user is not added
        Result<RegisteredAccount> account = await accounts.RegisterAsync(request.UserName, request.EmailAddress, request.Password);
        if (account.IsSuccess)
        {
            members.Add(Member.TryRegister(account.Value!.AccountId, request.Nickname, timeProvider.GetLocalToday()).GetValueOrThrow());
        }
        return account;
    }
}

// The confirmation email is sent after the registration is committed (so that an email never arrives for a registration that was not saved)
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
