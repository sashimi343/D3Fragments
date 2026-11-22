using System.Net;
using System.Net.Mail;

using IdentityIntegration.Core.Accounts;

namespace IdentityIntegration.Infrastructure.Email;

// Mailpit（Aspire が起動する SMTP サーバー）へ送る。送ったメールは Mailpit の Web 画面で確認する
internal sealed class SmtpAccountEmailSender(SmtpEndpoint endpoint) : IAccountEmailSender
{
    private const string From = "no-reply@identity-integration.example";

    public Task SendConfirmationLinkAsync(string emailAddress, string link, CancellationToken cancellationToken) =>
        SendAsync(emailAddress, "メールアドレスの確認", $"""
            <p>次のリンクを開いて、メールアドレスを確認してください。</p>
            <p><a href="{WebUtility.HtmlEncode(link)}">メールアドレスを確認する</a></p>
            """, cancellationToken);

    public Task SendTwoFactorCodeAsync(string emailAddress, string code, CancellationToken cancellationToken) =>
        SendAsync(emailAddress, "ログイン用の確認コード", $"""
            <p>ログイン用の確認コードは <strong>{WebUtility.HtmlEncode(code)}</strong> です。</p>
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
    // Aspire が渡す接続文字列（例: Endpoint=smtp://localhost:1025）から読み取る
    public static SmtpEndpoint Parse(string connectionString)
    {
        string endpoint = connectionString.Split(';')
            .Select(part => part.Trim())
            .First(part => part.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))["Endpoint=".Length..];
        var uri = new Uri(endpoint);
        return new SmtpEndpoint(uri.Host, uri.Port);
    }
}
