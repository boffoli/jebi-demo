namespace Jebi.Web.Auth;

internal interface IDemoEmailSender
{
    Task SendAsync(string to, string subject, string body);
}
