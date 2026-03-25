using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jebi.Web.Auth;

internal sealed class DemoAuthService(DemoAuthDb db, IDemoEmailSender emailSender) : IDemoAuthService
{
    private const int Iterations = 310_000;
    private const int KeyLength = 32;

    public async Task<DemoAuthResult> RegisterAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return DemoAuthResult.Fail("Invalid email.");
        if (password.Length < 6)
            return DemoAuthResult.Fail("Password too short (minimum 6 characters).");

        await db.EnsureCreatedAsync();

        if (await db.FindByEmailAsync(email) is not null)
            return DemoAuthResult.Fail("Email already registered.");

        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var hash = HashPassword(password, salt);
        var user = new DemoAuthUser { Id = Guid.NewGuid(), Email = email, PasswordHash = hash, Salt = salt };
        await db.CreateAsync(user);
        return DemoAuthResult.Ok(BuildPrincipal(user));
    }

    public async Task<DemoAuthResult> LoginAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            return DemoAuthResult.Fail("Email is required.");

        await db.EnsureCreatedAsync();

        var user = await db.FindByEmailAsync(email);
        if (user is null || !VerifyPassword(password, user.Salt, user.PasswordHash))
            return DemoAuthResult.Fail("Invalid credentials.");

        return DemoAuthResult.Ok(BuildPrincipal(user));
    }

    public async Task ForgotPasswordAsync(string email, string baseUrl)
    {
        await db.EnsureCreatedAsync();
        var user = await db.FindByEmailAsync(email);
        if (user is null) return; // don't reveal existence

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        user.ResetToken = token;
        user.ResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
        await db.UpdateAsync(user);

        var link = $"{baseUrl}/auth/reset-password?token={Uri.EscapeDataString(token)}";
        await emailSender.SendAsync(
            email,
            "Reset password Jebi Demo",
            $"Click here to reset your password:\n{link}\n\nThis link expires in 1 hour.");
    }

    public async Task<DemoAuthResult> ResetPasswordAsync(string token, string newPassword)
    {
        await db.EnsureCreatedAsync();
        var user = await db.FindByResetTokenAsync(token);
        if (user is null || user.ResetTokenExpiresAt < DateTime.UtcNow)
            return DemoAuthResult.Fail("Invalid or expired token.");
        if (newPassword.Length < 6)
            return DemoAuthResult.Fail("Password too short (minimum 6 characters).");

        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        user.PasswordHash = HashPassword(newPassword, salt);
        user.Salt = salt;
        user.ResetToken = null;
        user.ResetTokenExpiresAt = null;
        await db.UpdateAsync(user);
        return DemoAuthResult.Ok(BuildPrincipal(user));
    }

    private static string HashPassword(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeyLength);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string salt, string expectedHash)
    {
        var actual = HashPassword(password, salt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(actual),
            Convert.FromBase64String(expectedHash));
    }

    private static ClaimsPrincipal BuildPrincipal(DemoAuthUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
