using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Jebi.Common.Auth;
using Jebi.Samples.Crm;
using Jebi.Samples.Inventory;
using Jebi.Samples.Sales;
using Serilog;
using Jebi.Web.Auth;
using Jebi.Web.Services;

// 1) Bootstrap host and file-system prerequisites.
var builder = WebApplication.CreateBuilder(args);
var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "Logs");
Directory.CreateDirectory(logDirectory);

// 2) Configure structured logging (console + rolling file).
builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(logDirectory, "jebi-web-package-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            fileSizeLimitBytes: 10_000_000,
            rollOnFileSizeLimit: true,
            shared: true);
});

// 3) Ensure local folders used by sample databases exist.
var root = builder.Environment.ContentRootPath;
foreach (var dir in new[]
{
    Path.Combine(root, "Database"),
    Path.Combine(root, "Samples", "Crm"),
    Path.Combine(root, "Samples", "Inventory"),
    Path.Combine(root, "Samples", "Sales"),
})
{
    Directory.CreateDirectory(dir);
}

// 4) Register shared UI services.
builder.Services.AddSingleton<ErrorNotifier>();

// 5) Register demo target DbContexts.
builder.Services.AddDbContext<CrmTargetDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CrmDemo")));
builder.Services.AddDbContext<InventoryTargetDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("InventoryDemo")));
builder.Services.AddDbContext<SalesTargetDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SalesDemo")));

// 6) Read auth credentials from configuration providers (appsettings, user-secrets, env vars, etc.).
var clientId = builder.Configuration["Jebi:Auth:ClientId"]
    ?? throw new InvalidOperationException("Missing required configuration setting 'Jebi:Auth:ClientId'.");
var clientSecret = builder.Configuration["Jebi:Auth:ClientSecret"]
    ?? throw new InvalidOperationException("Missing required configuration setting 'Jebi:Auth:ClientSecret'.");
var jebiAuthOptions = new JebiAuthOptions
{
    ClientId = clientId,
    ClientSecret = clientSecret,
    AcpBaseUrl = builder.Configuration["Jebi:Auth:AcpBaseUrl"] ?? string.Empty
};

// 7) Read local execution database connection used by Jebi ingestion runtime.
var jebiExecutionDbConn = builder.Configuration.GetConnectionString("JebiIngestionDb")
    ?? throw new InvalidOperationException("Missing required connection string 'JebiIngestionDb'.");

// 8) Register Jebi REST integration (remote catalog/plan + local execute).
builder.Services.AddJebiRest(
    jebiExecutionDbConn,
    jebiAuthOptions,
    ensureTargetDatabases: true,
    targetDbContexts: [typeof(CrmTargetDbContext), typeof(InventoryTargetDbContext), typeof(SalesTargetDbContext)]);

// 9) Register web UI stack and demo auth endpoints.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDemoAuth(builder.Configuration);

var app = builder.Build();
app.Logger.LogInformation(
    "Starting Jebi.Web.Package in {Environment}. Jebi REST auth/discovery endpoints are package-managed.",
    app.Environment.EnvironmentName);

// 10) Configure exception handling and security headers for non-dev environments.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 11) Configure HTTP middleware pipeline.
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
var sharedUiWwwroot = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "Jebi.Web.SharedUi", "wwwroot"));
if (Directory.Exists(sharedUiWwwroot))
{
    // Serve shared static assets from Jebi.Web.SharedUi when available.
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(sharedUiWwwroot)
    });
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 12) Map endpoints for Blazor host and demo auth flows.
app.MapBlazorHub();
app.MapDemoAuthEndpoints();
app.MapFallbackToPage("/_Host");

// 13) Run app and always flush logs on shutdown.
try
{
    await app.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
