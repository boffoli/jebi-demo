using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;

namespace Jebi.Web.Auth;

internal sealed class DemoSmtpEmailSender(DemoAuthOptions options, ILogger<DemoSmtpEmailSender> logger)
    : IDemoEmailSender
{
    public async Task SendAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(options.SmtpHost))
        {
            logger.LogWarning(
                "[DemoAuth] SMTP not configured. Email not sent via SMTP. To={To} Subject={Subject}. " +
                "Using log fallback in Development.\n{Body}",
                to,
                subject,
                body);
            return;
        }

        try
        {
            using var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
            {
                EnableSsl = options.SmtpPort is 465 or 587,
                Credentials = string.IsNullOrWhiteSpace(options.SmtpUser)
                    ? null
                    : new NetworkCredential(options.SmtpUser, options.SmtpPassword)
            };
            using var message = new MailMessage(options.SmtpFrom, to, subject, body);
            await client.SendMailAsync(message);
            logger.LogInformation(
                "[DemoAuth] SMTP email sent. Host={Host}:{Port} To={To} Subject={Subject}",
                options.SmtpHost,
                options.SmtpPort,
                to,
                subject);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[DemoAuth] SMTP send failed. Host={Host}:{Port} To={To} Subject={Subject}",
                options.SmtpHost,
                options.SmtpPort,
                to,
                subject);
            throw;
        }
    }
}
