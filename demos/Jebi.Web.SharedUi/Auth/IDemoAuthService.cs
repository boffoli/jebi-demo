using System.Security.Claims;

namespace Jebi.Web.Auth;

internal interface IDemoAuthService
{
    Task<DemoAuthResult> RegisterAsync(string email, string password);
    Task<DemoAuthResult> LoginAsync(string email, string password);
    Task ForgotPasswordAsync(string email, string baseUrl);
    Task<DemoAuthResult> ResetPasswordAsync(string token, string newPassword);
}

internal sealed class DemoAuthResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }
    public ClaimsPrincipal? Principal { get; init; }

    public static DemoAuthResult Ok(ClaimsPrincipal principal) =>
        new() { Succeeded = true, Principal = principal };

    public static DemoAuthResult Fail(string error) =>
        new() { Succeeded = false, Error = error };
}
