namespace Jebi.Web.Auth;

internal sealed class DemoAuthOptions
{
    public string CookieName { get; set; } = ".JebiDemo.Auth";
    public string DbPath { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpFrom { get; set; } = "demo@localhost";
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
}
