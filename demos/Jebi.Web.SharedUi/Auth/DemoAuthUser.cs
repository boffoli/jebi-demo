namespace Jebi.Web.Auth;

internal sealed class DemoAuthUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiresAt { get; set; }
}
