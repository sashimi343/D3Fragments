using System.Net;
using System.Net.Mail;

using IdentityIntegration.Core.Accounts;

namespace IdentityIntegration.Infrastructure.Email;

// Sends to Mailpit (the SMTP server started by Aspire). Sent emails can be checked in the Mailpit web UI
internal sealed class SmtpAccountEmailSender(SmtpEndpoint endpoint) : IAccountEmailSender
{
    private const string From = "no-reply@identity-integration.example";

    public Task SendConfirmationLinkAsync(string emailAddress, string link, CancellationToken cancellationToken) =>
        SendAsync(emailAddress, "Confirm your email address", $"""
            <p>Open the following link to confirm your email address.</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">Confirm email address</a></p>
            """, cancellationToken);

    public Task SendTwoFactorCodeAsync(string emailAddress, string code, CancellationToken cancellationToken) =>
        SendAsync(emailAddress, "Your sign-in verification code", $"""
            <p>Your sign-in verification code is <strong>{WebUtility.HtmlEncode(code)}</strong>.</p>
            """, cancellationToken);

    private async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient(endpoint.Host, endpoint.Port);
        using var message = new MailMessage(From, to, subject, body) { IsBodyHtml = true };
        await client.SendMailAsync(message, cancellationToken);
    }
}

public sealed record SmtpEndpoint(string Host, int Port)
{
    // Reads from the connection string passed by Aspire (e.g. Endpoint=smtp://localhost:1025)
    public static SmtpEndpoint Parse(string connectionString)
    {
        string endpoint = connectionString.Split(';')
            .Select(part => part.Trim())
            .First(part => part.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))["Endpoint=".Length..];
        var uri = new Uri(endpoint);
        return new SmtpEndpoint(uri.Host, uri.Port);
    }
}
