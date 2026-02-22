namespace Group4_ReadingComicWeb.Services.Implementations;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
}
