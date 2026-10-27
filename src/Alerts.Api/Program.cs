using Alerts.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
var app = builder.Build();

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
app.Run();

// Public entry point for WebApplicationFactory; no business API is implemented yet.
public partial class Program;
