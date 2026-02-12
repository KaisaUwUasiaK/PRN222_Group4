using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace PRN222_Group4.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var host = _configuration["Mail:Host"];
        var port = _configuration.GetValue<int>("Mail:Port", 587);
        var senderEmail = _configuration["Mail:SenderEmail"];
        var senderName = _configuration["Mail:SenderName"] ?? "ComicVerse";
        var password = _configuration["Mail:Password"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Mail settings (Host, SenderEmail, Password) are not configured.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(senderEmail, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
