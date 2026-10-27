using Alerts.Api;
using Alerts.Application;
using Alerts.Infrastructure.Persistence;
using Alerts.Infrastructure.Identity;
using Alerts.Api.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

if (args.Length > 0 && args[0] == "--migrate")
{
    if (args.Length != 2) throw new ArgumentException("Usage: --migrate <absolute-local-NTFS-database-path>");
    var storage = new StorageAdmission(args[1], new VolumeFreeSpaceProbe());
    storage.Check(MutationPurpose.Normal);
    using var owner = WindowsDatabaseOwner.Acquire(args[1], allowCreate: true);
    using var gate = new MutationGate();
    await DatabaseLifecycle.MigrateAsync(owner, storage, gate);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
    options.SerializerOptions.MaxDepth = 16;
});
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 14;
    options.Password.RequiredUniqueChars = 4;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
});
builder.Services.AddAuthentication(IdentityConstants.BearerScheme)
    .AddBearerToken(IdentityConstants.BearerScheme, options => options.BearerTokenExpiration = TimeSpan.FromMinutes(15));
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Owner", policy => policy.RequireRole("User", "Admin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        }));
});
WindowsDatabaseOwner? lifetimeOwner = null;
var schemaReady = false;
var databasePath = builder.Configuration["Persistence:DatabasePath"];
if (!string.IsNullOrEmpty(databasePath))
{
    lifetimeOwner = WindowsDatabaseOwner.Acquire(databasePath);
    try
    {
        await DatabaseLifecycle.RequireCurrentSchemaAsync(lifetimeOwner);
        builder.Services.AddAlertsPersistence(lifetimeOwner);
        var keyDirectoryText = builder.Configuration["DataProtection:KeyDirectory"];
        var applicationName = builder.Configuration["DataProtection:ApplicationName"];
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The approved Data Protection profile requires Windows current-user DPAPI.");
        if (string.IsNullOrWhiteSpace(keyDirectoryText) || !Path.IsPathFullyQualified(keyDirectoryText)
            || string.IsNullOrWhiteSpace(applicationName))
            throw new InvalidOperationException("Persistent Data Protection key directory and application name are required with persistence.");
        var keyDirectory = new DirectoryInfo(Path.GetFullPath(keyDirectoryText));
        if (string.Equals(keyDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(lifetimeOwner.DatabasePath)?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Data Protection keys must be stored outside the database directory.");
        Directory.CreateDirectory(keyDirectory.FullName);
        builder.Services.AddDataProtection().SetApplicationName(applicationName)
            .PersistKeysToFileSystem(keyDirectory).ProtectKeysWithDpapi(protectToLocalMachine: false);
        schemaReady = true;
    }
    catch { lifetimeOwner.Dispose(); throw; }
}
var app = builder.Build();
using var databaseLifetime = lifetimeOwner;

if (schemaReady)
{
    var provisioning = app.Configuration.GetSection("Identity:Provisioning").Get<IdentityProvisioningOptions>()
        ?? new IdentityProvisioningOptions();
    await app.Services.GetRequiredService<IdentityProvisioner>().ProvisionAsync(provisioning.Accounts);
}

// Kestrel endpoint configuration can override URLs; it is not supported in this sandbox.
if (app.Configuration.GetSection("Kestrel:Endpoints").GetChildren().Any())
{
    throw new InvalidOperationException("Kestrel endpoint overrides are not supported in the local sandbox.");
}

var urls = (app.Configuration["urls"] ?? "http://127.0.0.1:5080")
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
if (urls.Length == 0 || urls.Any(url => !SandboxBindingPolicy.IsAllowed(url)))
{
    throw new InvalidOperationException("The local sandbox requires literal loopback HTTP bindings.");
}

app.Urls.Clear();
foreach (var url in urls)
{
    app.Urls.Add(url);
}
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1"))
        context.Response.Headers.CacheControl = "no-store";
    await next(context).ConfigureAwait(false);
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", () =>
{
    if (!schemaReady) return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    try
    {
        lifetimeOwner!.RequireActive();
        app.Services.GetRequiredService<StorageAdmission>().Check(MutationPurpose.SendResultOrRecovery);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or ObjectDisposedException)
    { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
});
app.MapAlertsIdentity(schemaReady);
app.Run();

// Public entry point for WebApplicationFactory.
public partial class Program;
