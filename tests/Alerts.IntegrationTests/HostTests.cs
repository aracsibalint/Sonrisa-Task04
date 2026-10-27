using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Alerts.IntegrationTests;

public sealed class HostTests
{
    [Fact]
    public async Task Liveness_DefaultHost_ReturnsAlive()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(TestContext.Current.CancellationToken);
        Assert.Equal("alive", body!["status"]);
    }

    [Fact]
    public async Task UnknownRoute_DefaultHost_ReturnsProblemDetailsWithoutInternals()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/rules", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.False(body.TryGetProperty("exception", out _));
        Assert.False(body.TryGetProperty("stackTrace", out _));
    }

    [Fact]
    public async Task Endpoints_DefaultHost_ExposesOnlyHealth_AndUnconfiguredPersistenceIsNotReady()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        await client.GetAsync("/health/live", TestContext.Current.CancellationToken);

        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;

        Assert.Equal(new[] { "/health/live", "/health/ready" }, endpoints.OfType<RouteEndpoint>().Select(e => e.RoutePattern.RawText).Order());
        using var readiness = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
    }

    [Theory]
    [InlineData("urls", "http://0.0.0.0:5080")]
    [InlineData("Kestrel:Endpoints:Public:Url", "http://0.0.0.0:5080")]
    public async Task Startup_UnsafeBindingOverride_RejectsConfiguration(string key, string value)
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { [key] = value })));

        var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("sandbox", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
