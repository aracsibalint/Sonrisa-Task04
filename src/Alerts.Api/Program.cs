using Alerts.Api;
using Alerts.Application;
using Alerts.Infrastructure.Persistence;

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
        schemaReady = true;
    }
    catch { lifetimeOwner.Dispose(); throw; }
}
var app = builder.Build();
using var databaseLifetime = lifetimeOwner;

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
app.Run();

// Public entry point for WebApplicationFactory; no business API is implemented yet.
public partial class Program;
