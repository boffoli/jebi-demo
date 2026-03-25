using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jebi.Web.Auth;

public static class DemoAuthExtensions
{
    public static IServiceCollection AddDemoAuth(this IServiceCollection services, IConfiguration config)
    {
        var opts = config.GetSection("DemoAuth").Get<DemoAuthOptions>() ?? new DemoAuthOptions();
        services.AddSingleton(opts);
        services.AddSingleton<DemoAuthDb>();
        services.AddScoped<IDemoAuthService, DemoAuthService>();
        services.AddScoped<IDemoEmailSender, DemoSmtpEmailSender>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.Cookie.Name = opts.CookieName;
                o.LoginPath = "/auth/login";
                o.LogoutPath = "/auth/logout";
                o.AccessDeniedPath = "/auth/login";
                o.ExpireTimeSpan = TimeSpan.FromDays(7);
                o.SlidingExpiration = true;
            });
        services.AddAuthorization();
        return services;
    }

    public static WebApplication MapDemoAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/register", async (HttpContext ctx, IDemoAuthService svc) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var email = form["email"].ToString().Trim();
            var password = form["password"].ToString();
            var result = await svc.RegisterAsync(email, password);
            if (!result.Succeeded)
                return Results.Redirect($"/auth/register?error={Uri.EscapeDataString(result.Error!)}");
            await ctx.SignInAsync(result.Principal!);
            return Results.Redirect("/");
        }).DisableAntiforgery();

        app.MapPost("/auth/login", async (HttpContext ctx, IDemoAuthService svc) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var email = form["email"].ToString().Trim();
            var password = form["password"].ToString();
            var returnUrl = form["returnUrl"].ToString();
            var result = await svc.LoginAsync(email, password);
            if (!result.Succeeded)
                return Results.Redirect($"/auth/login?error={Uri.EscapeDataString(result.Error!)}");
            await ctx.SignInAsync(result.Principal!);
            var redirect = !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/')
                ? returnUrl : "/";
            return Results.Redirect(redirect);
        }).DisableAntiforgery();

        app.MapGet("/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync();
            return Results.Redirect("/auth/login");
        });

        app.MapPost("/auth/forgot-password", async (HttpContext ctx, IDemoAuthService svc) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var email = form["email"].ToString().Trim();
            await svc.ForgotPasswordAsync(email, $"{ctx.Request.Scheme}://{ctx.Request.Host}");
            return Results.Redirect("/auth/forgot-password?success=1");
        }).DisableAntiforgery();

        app.MapPost("/auth/reset-password", async (HttpContext ctx, IDemoAuthService svc) =>
        {
            var form = await ctx.Request.ReadFormAsync();
            var token = form["token"].ToString();
            var password = form["password"].ToString();
            var result = await svc.ResetPasswordAsync(token, password);
            if (!result.Succeeded)
                return Results.Redirect(
                    $"/auth/reset-password?token={Uri.EscapeDataString(token)}&error={Uri.EscapeDataString(result.Error!)}");
            await ctx.SignInAsync(result.Principal!);
            return Results.Redirect("/");
        }).DisableAntiforgery();

        app.MapGet("/auth/me", (HttpContext ctx) =>
            ctx.User.Identity?.IsAuthenticated == true
                ? Results.Ok(new { email = ctx.User.FindFirstValue(ClaimTypes.Email) })
                : Results.Unauthorized());

        return app;
    }
}
