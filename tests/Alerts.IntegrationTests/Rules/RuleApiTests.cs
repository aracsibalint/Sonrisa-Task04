using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Alerts.Application;
using Alerts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Alerts.IntegrationTests.Rules;

public sealed class RuleApiTests
{
    private const string UserA = "00000000-0000-0000-0000-000000000101";
    private const string UserB = "00000000-0000-0000-0000-000000000102";
    private const string Admin = "00000000-0000-0000-0000-000000000103";
    private const string Password = "Synthetic-Only-42!Alpha";

    [Fact]
    public async Task Lifecycle_UsesActualLogin_HidesOtherOwnersAndEnforcesVersionsAndRevocation()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using var factory = sandbox.Factory();
        using var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/rules", TestContext.Current.CancellationToken)).StatusCode);

        using var a = factory.CreateClient(); using var b = factory.CreateClient(); using var admin = factory.CreateClient();
        await Authenticate(a, "user-a@example.test"); await Authenticate(b, "user-b@example.test"); await Authenticate(admin, "admin@example.test");
        await SeedChannels(factory, UserA, email: true, slack: true);
        await AddDisabledEmailVersion(factory, UserA);
        var initiallyAvailable = await a.GetFromJsonAsync<JsonElement>("/api/v1/me/channels", TestContext.Current.CancellationToken);
        Assert.True(initiallyAvailable.GetProperty("channels")[0].GetProperty("available").GetBoolean());

        using var created = await a.PostAsJsonAsync("/api/v1/rules", News("moon"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("\"1\"", created.Headers.ETag?.Tag);
        var body = await created.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var id = body.GetProperty("id").GetString()!;
        Assert.Equal($"/api/v1/rules/{id}", created.Headers.Location?.ToString());
        Assert.False(body.TryGetProperty("ownerId", out _));

        foreach (var other in new[] { b, admin })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v1/rules/{id}", TestContext.Current.CancellationToken)).StatusCode);
            using var guessed = await other.PutAsJsonAsync($"/api/v1/rules/{id}", News("moon", version: 1), TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, guessed.StatusCode);
            using var guessedDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/rules/{id}");
            guessedDelete.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
            Assert.Equal(HttpStatusCode.NotFound, (await other.SendAsync(guessedDelete, TestContext.Current.CancellationToken)).StatusCode);
        }

        using var identical = await a.PutAsJsonAsync($"/api/v1/rules/{id}", News("moon", version: 1), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, identical.StatusCode);
        Assert.Equal("\"2\"", identical.Headers.ETag?.Tag);
        Assert.Equal(HttpStatusCode.PreconditionFailed,
            (await a.PutAsJsonAsync($"/api/v1/rules/{id}", News("moon", version: 1), TestContext.Current.CancellationToken)).StatusCode);
        using var categoryChange = await a.PutAsJsonAsync($"/api/v1/rules/{id}", new
        {
            name = "Market", category = "market", filter = new { instrument = "BTCUSDT", comparisonKind = "binance-rolling-24h", thresholdPercent = "5" },
            channels = new[] { "email" }, enabled = true, version = 2
        }, TestContext.Current.CancellationToken);
        Assert.Equal("ImmutableCategory", (await categoryChange.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString());

        var edits = await Task.WhenAll(
            a.PutAsJsonAsync($"/api/v1/rules/{id}", News("moon-a", version: 2), TestContext.Current.CancellationToken),
            a.PutAsJsonAsync($"/api/v1/rules/{id}", News("moon-b", version: 2), TestContext.Current.CancellationToken));
        Assert.Equal(1, edits.Count(x => x.StatusCode == HttpStatusCode.OK));
        Assert.Equal(1, edits.Count(x => x.StatusCode == HttpStatusCode.PreconditionFailed));
        foreach (var response in edits) response.Dispose();

        Assert.Equal((HttpStatusCode)428, (await a.DeleteAsync($"/api/v1/rules/{id}", TestContext.Current.CancellationToken)).StatusCode);
        using (var weak = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/rules/{id}"))
        {
            weak.Headers.TryAddWithoutValidation("If-Match", "W/\"3\"");
            Assert.Equal(HttpStatusCode.BadRequest, (await a.SendAsync(weak, TestContext.Current.CancellationToken)).StatusCode);
        }
        using (var stale = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/rules/{id}"))
        {
            stale.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
            Assert.Equal(HttpStatusCode.PreconditionFailed, (await a.SendAsync(stale, TestContext.Current.CancellationToken)).StatusCode);
        }

        await RevokeEmail(factory, UserA);
        var channels = await a.GetFromJsonAsync<JsonElement>("/api/v1/me/channels", TestContext.Current.CancellationToken);
        var email = channels.GetProperty("channels").EnumerateArray().First();
        Assert.Equal("Disabled", email.GetProperty("reason").GetString());
        using var revokedUpdate = await a.PutAsJsonAsync($"/api/v1/rules/{id}", News("moon", version: 3), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, revokedUpdate.StatusCode);
        Assert.Equal("ChannelUnavailable", (await revokedUpdate.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("code").GetString());

        using var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/rules/{id}");
        delete.Headers.TryAddWithoutValidation("If-Match", "\"3\"");
        Assert.Equal(HttpStatusCode.NoContent, (await a.SendAsync(delete, TestContext.Current.CancellationToken)).StatusCode);
        using var repeated = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/rules/{id}");
        repeated.Headers.TryAddWithoutValidation("If-Match", "\"4\"");
        Assert.Equal(HttpStatusCode.NotFound, (await a.SendAsync(repeated, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Create_StrictUnionAndConcurrentLastQuotaSlot_NeverExceedsOwnerOrGlobalLimit()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using var factory = sandbox.Factory();
        using var client = factory.CreateClient();
        await Authenticate(client, "user-a@example.test");
        await SeedChannels(factory, UserA, email: true, slack: false);

        using (var anonymous = factory.CreateClient())
        using (var unauthenticatedMalformed = await anonymous.PostAsync("/api/v1/rules",
            Json("{\"name\":"), TestContext.Current.CancellationToken))
            Assert.Equal(HttpStatusCode.Unauthorized, unauthenticatedMalformed.StatusCode);

        foreach (var invalid in new[]
        {
            "{\"name\":\"bad\",\"category\":\"news\",\"filter\":{\"keyword\":\"x\"},\"channels\":[\"email\"],\"enabled\":true,\"ownerId\":\"00000000-0000-0000-0000-000000000999\"}",
            "{\"name\":\"bad\",\"category\":\"news\",\"filter\":{\"keyword\":\"x\"},\"channels\":[\"email\"],\"enabled\":true,\"destinationId\":\"00000000-0000-0000-0000-000000000999\"}",
            "{\"name\":\"bad\",\"category\":\"news\",\"filter\":{\"keyword\":\"x\",\"keyword\":\"y\"},\"channels\":[\"email\"],\"enabled\":true}",
            "{\"name\":"
        })
        {
            using var invalidResponse = await client.PostAsync("/api/v1/rules", Json(invalid), TestContext.Current.CancellationToken);
            await AssertCanonicalProblem(invalidResponse, HttpStatusCode.BadRequest, "ValidationFailed", "/api/v1/rules");
        }
        using (var unsupported = await client.PostAsync("/api/v1/rules",
            new StringContent("{}", Encoding.UTF8, "text/plain"), TestContext.Current.CancellationToken))
            await AssertCanonicalProblem(unsupported, HttpStatusCode.UnsupportedMediaType, "UnsupportedMediaType", "/api/v1/rules");
        using (var oversized = await client.PostAsync("/api/v1/rules",
            Json(new string(' ', 65_537)), TestContext.Current.CancellationToken))
            await AssertCanonicalProblem(oversized, HttpStatusCode.RequestEntityTooLarge, "PayloadTooLarge", "/api/v1/rules");

        using var wrongUnion = await client.PostAsJsonAsync("/api/v1/rules", new
        {
            name = "bad", category = "news", filter = new { keyword = "x", thresholdPercent = "5" }, channels = new[] { "email" }, enabled = true
        }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, wrongUnion.StatusCode);
        using var numberDecimal = await client.PostAsync("/api/v1/rules", new StringContent(
            "{\"name\":\"bad\",\"category\":\"market\",\"filter\":{\"instrument\":\"BTCUSDT\",\"comparisonKind\":\"binance-rolling-24h\",\"thresholdPercent\":5},\"channels\":[\"email\"],\"enabled\":true}", Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, numberDecimal.StatusCode);

        for (var i = 0; i < 19; i++)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/rules", News($"word-{i}"), TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
        using (var paused = await client.PostAsJsonAsync("/api/v1/rules", News("paused", enabled: false), TestContext.Current.CancellationToken))
            Assert.Equal(HttpStatusCode.Created, paused.StatusCode);
        await SeedActiveRules(factory, UserB, 80);

        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/rules", News("last-a"), TestContext.Current.CancellationToken),
            client.PostAsJsonAsync("/api/v1/rules", News("last-b"), TestContext.Current.CancellationToken));
        Assert.Equal(1, attempts.Count(x => x.StatusCode == HttpStatusCode.Created));
        Assert.Equal(1, attempts.Count(x => x.StatusCode == HttpStatusCode.Conflict));
        foreach (var response in attempts) response.Dispose();
        var list = await client.GetFromJsonAsync<JsonElement>("/api/v1/rules?enabled=true&pageSize=100", TestContext.Current.CancellationToken);
        Assert.Equal(20, list.GetProperty("items").GetArrayLength());
        var firstPage = await client.GetFromJsonAsync<JsonElement>("/api/v1/rules?enabled=true&pageSize=1", TestContext.Current.CancellationToken);
        var rawCursor = firstPage.GetProperty("nextCursor").GetString()!;
        var cursor = Uri.EscapeDataString(rawCursor);
        var secondPage = await client.GetFromJsonAsync<JsonElement>($"/api/v1/rules?enabled=true&pageSize=1&cursor={cursor}", TestContext.Current.CancellationToken);
        Assert.NotEqual(firstPage.GetProperty("items")[0].GetProperty("id").GetString(), secondPage.GetProperty("items")[0].GetProperty("id").GetString());
        foreach (var invalidCursorUrl in new[]
        {
            "/api/v1/rules?enabled=true&pageSize=1&cursor=",
            "/api/v1/rules?enabled=true&pageSize=1&cursor=" + cursor + "&cursor=" + cursor,
            "/api/v1/rules?enabled=true&pageSize=1&cursor=" + Uri.EscapeDataString(rawCursor[..^1] + (rawCursor[^1] == 'A' ? "B" : "A")),
            "/api/v1/rules?enabled=true&pageSize=2&cursor=" + cursor,
            "/api/v1/rules?enabled=true&category=news&pageSize=1&cursor=" + cursor,
            "/api/v1/rules?enabled=true&pageSize=1&cursor=" + Uri.EscapeDataString("é"),
            "/api/v1/rules?enabled=true&pageSize=1&cursor=" + new string('A', 4097)
        })
        {
            using var invalidCursor = await client.GetAsync(invalidCursorUrl, TestContext.Current.CancellationToken);
            await AssertCanonicalProblem(invalidCursor, HttpStatusCode.BadRequest, "InvalidCursor", "/api/v1/rules");
        }
        foreach (var invalidQueryUrl in new[]
        {
            "/api/v1/rules?category=news&category=market",
            "/api/v1/rules?pageSize=1&pageSize=2"
        })
        {
            using var invalidQuery = await client.GetAsync(invalidQueryUrl, TestContext.Current.CancellationToken);
            await AssertCanonicalProblem(invalidQuery, HttpStatusCode.BadRequest, "ValidationFailed", "/api/v1/rules");
        }
        using var userB = factory.CreateClient();
        await Authenticate(userB, "user-b@example.test");
        using var foreignCursor = await userB.GetAsync("/api/v1/rules?enabled=true&pageSize=1&cursor=" + cursor, TestContext.Current.CancellationToken);
        await AssertCanonicalProblem(foreignCursor, HttpStatusCode.BadRequest, "InvalidCursor", "/api/v1/rules");
    }

    private static object News(string keyword, bool enabled = true, long? version = null) => version is null
        ? new { name = "Space updates", category = "news", filter = new { keyword }, channels = new[] { "email" }, enabled }
        : new { name = "Space updates", category = "news", filter = new { keyword }, channels = new[] { "email" }, enabled, version };

    private static StringContent Json(string value) => new(value, Encoding.UTF8, "application/json");

    private static async Task AssertCanonicalProblem(HttpResponseMessage response, HttpStatusCode status, string code, string instance)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(new[] { "code", "detail", "errors", "instance", "status", "title", "traceId", "type" },
            body.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(instance, body.GetProperty("instance").GetString());
        Assert.Equal($"urn:alerts:problem:{string.Concat(code.Select((c, i) => char.IsUpper(c) && i > 0 ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()))}", body.GetProperty("type").GetString());
        Assert.True(Guid.TryParseExact(body.GetProperty("traceId").GetString(), "D", out _));
        Assert.Equal(JsonValueKind.Null, body.GetProperty("errors").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("detail").GetString()));
    }

    private static async Task Authenticate(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
    }

    private static Task SeedChannels(WebApplicationFactory<Program> factory, string ownerId, bool email, bool slack) => Mutate(factory, (db, now) =>
    {
        if (email) db.Set<ChannelDestinationRow>().Add(new() { Id = Guid.NewGuid().ToString("D"), OwnerId = ownerId, Channel = "Email", ConfigurationVersion = 1, DestinationIdentity = "test-email", Address = "owner@example.test", IsAvailable = true, CreatedAtUtcMs = now });
        if (slack) db.Set<ChannelDestinationRow>().Add(new() { Id = Guid.NewGuid().ToString("D"), OwnerId = ownerId, Channel = "Slack", ConfigurationVersion = 1, DestinationIdentity = "test-slack", SecretReference = "secret:test", IsAvailable = true, CreatedAtUtcMs = now });
    });
    private static Task RevokeEmail(WebApplicationFactory<Program> factory, string ownerId) => Mutate(factory, (db, now) =>
    {
        var row = db.Set<ChannelDestinationRow>().Single(x => x.OwnerId == ownerId && x.Channel == "Email" && x.IsAvailable);
        row.IsAvailable = false; row.RevokedAtUtcMs = now;
    });
    private static Task AddDisabledEmailVersion(WebApplicationFactory<Program> factory, string ownerId) => Mutate(factory, (db, now) =>
    {
        db.Set<ChannelDestinationRow>().Add(new()
        {
            Id = Guid.NewGuid().ToString("D"), OwnerId = ownerId, Channel = "Email", ConfigurationVersion = 2,
            DestinationIdentity = "disabled-email", Address = "disabled@example.test", IsAvailable = false, CreatedAtUtcMs = now
        });
    });
    private static Task SeedActiveRules(WebApplicationFactory<Program> factory, string ownerId, int count) => Mutate(factory, (db, now) =>
    {
        for (var i = 0; i < count; i++) db.Set<RuleRow>().Add(new()
        {
            Id = Guid.NewGuid().ToString("D"), OwnerId = ownerId, Name = $"fixture-{i}", Category = "News", Keyword = $"fixture-{i}",
            EmailSelected = true, Enabled = true, Version = 1, CreatedAtUtcMs = now - i, UpdatedAtUtcMs = now - i
        });
    });
    private static Task Mutate(WebApplicationFactory<Program> factory, Action<AlertsDbContext, long> action) =>
        factory.Services.GetRequiredService<IMutationGate>().ExecuteAsync(async (admission, ct) =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<PersistenceUnitOfWork>().ExecuteAsync(admission, MutationPurpose.Normal, (db, _) =>
            { action(db, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); return Task.FromResult(true); }, ct);
        }, TestContext.Current.CancellationToken);

    private sealed class Sandbox : IAsyncDisposable
    {
        private readonly string root; private readonly string database; private readonly string keys;
        private Sandbox(string root)
        {
            this.root = root; database = Path.Combine(Directory.CreateDirectory(Path.Combine(root, "database")).FullName, "alerts.db");
            keys = Directory.CreateDirectory(Path.Combine(root, "keys")).FullName;
        }
        public static async Task<Sandbox> CreateAsync()
        {
            var value = new Sandbox(Path.Combine(Path.GetTempPath(), "opencode", "be005-" + Guid.NewGuid().ToString("N")));
            using var owner = WindowsDatabaseOwner.Acquire(value.database, true); using var gate = new MutationGate();
            await DatabaseLifecycle.MigrateAsync(owner, new StorageAdmission(value.database, new VolumeFreeSpaceProbe()), gate, TestContext.Current.CancellationToken);
            return value;
        }
        public WebApplicationFactory<Program> Factory() => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            var values = new Dictionary<string, string?> { ["Persistence:DatabasePath"] = database, ["DataProtection:KeyDirectory"] = keys, ["DataProtection:ApplicationName"] = "Alerts.BE005.Tests" };
            Add(values, 0, UserA, "user-a@example.test", "User"); Add(values, 1, UserB, "user-b@example.test", "User"); Add(values, 2, Admin, "admin@example.test", "Admin");
            foreach (var pair in values) builder.UseSetting(pair.Key, pair.Value);
        });
        private static void Add(Dictionary<string, string?> values, int index, string id, string email, string role)
        {
            var p = $"Identity:Provisioning:Accounts:{index}:"; values[p + "Id"] = id; values[p + "Email"] = email; values[p + "Password"] = Password; values[p + "Role"] = role;
        }
        public async ValueTask DisposeAsync()
        {
            for (var i = 0; i < 20 && Directory.Exists(root); i++) try { Directory.Delete(root, true); } catch (IOException) when (i < 19) { await Task.Delay(25); }
        }
    }
}
