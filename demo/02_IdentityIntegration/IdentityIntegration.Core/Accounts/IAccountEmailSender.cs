namespace IdentityIntegration.Core.Accounts;

public interface IAccountEmailSender
{
    Task SendConfirmationLinkAsync(string emailAddress, string link, CancellationToken cancellationToken);

    Task SendTwoFactorCodeAsync(string emailAddress, string code, CancellationToken cancellationToken);
}
