using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Alerts.Application;
using Alerts.Infrastructure.Persistence;
using Alerts.Infrastructure.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Alerts.IntegrationTests.Identity;

public sealed class IdentityTests
{
    private const string UserId = "00000000-0000-0000-0000-000000000101";
    private const string AdminId = "00000000-0000-0000-0000-000000000103";
    private const string Password = "Synthetic-Only-42!Alpha";

    [Fact]
    public async Task Login_UserAndAdmin_ReturnsFrameworkOpaqueTokenAndSafeCurrentAccountViews()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using var factory = sandbox.Factory();
        using var client = factory.CreateClient();

        var userToken = await LoginAsync(client, "user-a@example.test", Password);
        Assert.Equal("Bearer", userToken.GetProperty("tokenType").GetString());
        Assert.Equal(900, userToken.GetProperty("expiresIn").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(userToken.GetProperty("accessToken").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(userToken.GetProperty("refreshToken").GetString()));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken.GetProperty("accessToken").GetString());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/me", TestContext.Current.CancellationToken);
        Assert.Equal(UserId, me.GetProperty("id").GetString());
        Assert.Equal("user-a@example.test", me.GetProperty("email").GetString());
        Assert.Equal(["User"], me.GetProperty("roles").EnumerateArray().Select(x => x.GetString()));

        var channels = await client.GetFromJsonAsync<JsonElement>("/api/v1/me/channels", TestContext.Current.CancellationToken);
        Assert.Equal(["email", "slack"], channels.GetProperty("channels").EnumerateArray().Select(x => x.GetProperty("channel").GetString()));
        Assert.All(channels.GetProperty("channels").EnumerateArray(), channel =>
        {
            Assert.False(channel.GetProperty("available").GetBoolean());
            Assert.Equal("NotProvisioned", channel.GetProperty("reason").GetString());
            Assert.Equal(3, channel.EnumerateObject().Count());
        });

        client.DefaultRequestHeaders.Authorization = null;
        var adminToken = await LoginAsync(client, "admin@example.test", Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken.GetProperty("accessToken").GetString());
        var admin = await client.GetFromJsonAsync<JsonElement>("/api/v1/me", TestContext.Current.CancellationToken);
        Assert.Equal(AdminId, admin.GetProperty("id").GetString());
        Assert.Equal(["Admin", "User"], admin.GetProperty("roles").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task Login_InvalidUnknownAndLocked_ReturnsSameGenericUnauthorizedAndPersistsLockout()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using var factory = sandbox.Factory();
        using var client = factory.CreateClient();

        var unknown = await FailedLoginAsync(client, "missing@example.test", "Wrong-Password-42!X");
        var wrong = await FailedLoginAsync(client, "user-a@example.test", "Wrong-Password-42!X");
        Assert.Equal(unknown, wrong);
        for (var i = 1; i < 5; i++) await FailedLoginAsync(client, "user-a@example.test", "Wrong-Password-42!X");
        var locked = await FailedLoginAsync(client, "user-a@example.test", Password);
        Assert.Equal(unknown, locked);
    }

    [Fact]
    public async Task ProvisioningAndKeyRing_RestartIsIdempotentAndExistingTokenSurvives()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        string token;
        await using (var first = sandbox.Factory())
        using (var client = first.CreateClient())
            token = (await LoginAsync(client, "user-a@example.test", Password)).GetProperty("accessToken").GetString()!;

        await using var second = sandbox.Factory("Different-Synthetic-42!Beta");
        using var restartedClient = second.CreateClient();
        restartedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await restartedClient.GetAsync("/api/v1/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        restartedClient.DefaultRequestHeaders.Authorization = null;
        _ = await LoginAsync(restartedClient, "user-a@example.test", Password);
        Assert.Contains(Directory.EnumerateFiles(sandbox.Keys), path => File.ReadAllText(path).Contains("encryptedSecret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConcurrentFailedLogins_SerializeBookkeepingAndReachLockoutWithoutDeadlock()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using var factory = sandbox.Factory();
        using var client = factory.CreateClient();

        var failures = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            client.PostAsJsonAsync("/api/v1/auth/login", new { email = "user-a@example.test", password = "Wrong-Password-42!X" },
                TestContext.Current.CancellationToken)));
        Assert.All(failures, response => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode));
        foreach (var response in failures) response.Dispose();
        await FailedLoginAsync(client, "user-a@example.test", Password);
    }

    [Fact]
    public async Task RouteInventory_DoesNotExposeFrameworkIdentityManagementOrRefreshRoutes()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using var factory = sandbox.Factory();
        using var client = factory.CreateClient();
        await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var routes = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Select(x => x.RoutePattern.RawText).ToArray();
        Assert.DoesNotContain(routes, x => x is not null && (x.Contains("register") || x.Contains("reset")
            || x.Contains("refresh") || x.Contains("logout") || x.Contains("manage")));
        Assert.Equal(5, routes.Length);
    }

    [Fact]
    public async Task LoginRateLimit_EleventhAttemptFromSameIpIsRejectedWithoutQueueing()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using var factory = sandbox.Factory();
        using var client = factory.CreateClient();

        for (var attempt = 0; attempt < 10; attempt++)
            await FailedLoginAsync(client, $"missing-{attempt}@example.test", "Wrong-Password-42!X");
        using var limited = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "missing-final@example.test", password = "Wrong-Password-42!X" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }

    [Fact]
    public async Task Login_OldFrameworkHash_RehashesInsideAdmittedCommitBeforeIssuingToken()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using var factory = sandbox.Factory();
        using var client = factory.CreateClient();
        await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        var oldHasher = new PasswordHasher<IdentityUser>(Options.Create(new PasswordHasherOptions { IterationCount = 10_000 }));
        var oldHash = oldHasher.HashPassword(new IdentityUser { Id = UserId }, Password);
        var gate = factory.Services.GetRequiredService<IMutationGate>();
        await gate.ExecuteAsync(async (admission, ct) =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<PersistenceUnitOfWork>().ExecuteAsync(admission, MutationPurpose.Normal,
                async (db, token) =>
                {
                    var user = await db.Users.SingleAsync(x => x.Id == UserId, token);
                    user.PasswordHash = oldHash;
                    user.SecurityStamp = Guid.NewGuid().ToString("D");
                    user.ConcurrencyStamp = Guid.NewGuid().ToString("D");
                    return true;
                }, ct);
        }, TestContext.Current.CancellationToken);

        _ = await LoginAsync(client, "user-a@example.test", Password);
        await using var readScope = factory.Services.CreateAsyncScope();
        var storedHash = await readScope.ServiceProvider.GetRequiredService<AlertsDbContext>().Users.AsNoTracking()
            .Where(x => x.Id == UserId).Select(x => x.PasswordHash).SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotEqual(oldHash, storedHash);
    }

    [Fact]
    public async Task Login_SqliteWriterBusy_ReturnsSafeStorageUnavailableWithoutToken()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using var factory = sandbox.Factory();
        using var client = factory.CreateClient();
        await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        await using var blocker = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = sandbox.Database, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 1 }.ToString());
        await blocker.OpenAsync(TestContext.Current.CancellationToken);
        await using (var begin = blocker.CreateCommand())
        {
            begin.CommandText = "BEGIN IMMEDIATE;";
            await begin.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        using var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "user-a@example.test", password = Password }, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("StorageUnavailable", body.GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("exception", out _));
        Assert.False(body.TryGetProperty("accessToken", out _));
        Assert.True(factory.Services.GetRequiredService<StorageAdmission>().IsFaulted);
        await using var rollback = blocker.CreateCommand();
        rollback.CommandText = "ROLLBACK;";
        await rollback.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Provisioning_AdminDowngrade_ReconcilesFreshClaimsExactly()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using (var baseline = sandbox.Factory()) using (baseline.CreateClient()) { }
        await using var downgraded = sandbox.Factory(adminRole: "User", includeBackupAdmin: true);
        using var client = downgraded.CreateClient();
        var token = await LoginAsync(client, "admin@example.test", Password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("accessToken").GetString());
        var me = await client.GetFromJsonAsync<JsonElement>("/api/v1/me", TestContext.Current.CancellationToken);
        Assert.Equal(["User"], me.GetProperty("roles").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task Provisioning_AbsentAccountIsDisabledAndLaterReAddUsesOriginalPassword()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using (var baseline = sandbox.Factory()) using (baseline.CreateClient()) { }
        await using (var absent = sandbox.Factory(includeUser: false))
        using (var client = absent.CreateClient())
            await FailedLoginAsync(client, "user-a@example.test", Password);

        await using var readded = sandbox.Factory(password: "Different-Synthetic-42!Beta");
        using var readdedClient = readded.CreateClient();
        _ = await LoginAsync(readdedClient, "user-a@example.test", Password);
    }

    [Fact]
    public async Task Provisioning_EmptyConfigurationFailsBeforeDisablingExistingAccounts()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using (var baseline = sandbox.Factory()) using (baseline.CreateClient()) { }
        await using (var empty = sandbox.Factory(empty: true))
            Assert.Throws<InvalidOperationException>(() => empty.CreateClient());
        var user = await sandbox.ReadUserAsync(UserId);
        Assert.True(user.LockoutEnabled);
    }

    [Fact]
    public async Task Provisioning_PreCommitFaultRollsBackRoleAndDisableReconciliation()
    {
        await using var sandbox = await Sandbox.CreateAsync();
        await using (var baseline = sandbox.Factory()) using (baseline.CreateClient()) { }
        await using (var failing = sandbox.Factory(includeUser: false, fault: true))
            Assert.Throws<SyntheticProvisioningFaultException>(() => failing.CreateClient());

        var user = await sandbox.ReadUserAsync(UserId);
        Assert.True(user.LockoutEnabled);
        await using var db = new AlertsDbContext(DatabaseLifecycle.Options(sandbox.Database));
        Assert.Equal([StableIdentityIds.UserRoleId], await db.UserRoles.AsNoTracking().Where(x => x.UserId == UserId)
            .Select(x => x.RoleId).ToArrayAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<JsonElement> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    private static async Task<string> FailedLoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private sealed class Sandbox : IAsyncDisposable
    {
        private readonly string root;
        private readonly string database;
        private readonly string keys;
        public string Keys => keys;
        public string Database => database;

        private Sandbox(string root)
        {
            this.root = root;
            var databaseDirectory = Directory.CreateDirectory(Path.Combine(root, "database"));
            keys = Directory.CreateDirectory(Path.Combine(root, "keys")).FullName;
            database = Path.Combine(databaseDirectory.FullName, "alerts.db");
        }

        public static async Task<Sandbox> CreateAsync()
        {
            var sandbox = new Sandbox(Path.Combine(Path.GetTempPath(), "opencode", "be004-" + Guid.NewGuid().ToString("N")));
            var storage = new StorageAdmission(sandbox.database, new VolumeFreeSpaceProbe());
            using var owner = WindowsDatabaseOwner.Acquire(sandbox.database, allowCreate: true);
            using var gate = new MutationGate();
            await DatabaseLifecycle.MigrateAsync(owner, storage, gate, TestContext.Current.CancellationToken);
            return sandbox;
        }

        public WebApplicationFactory<Program> Factory(string password = Password, bool includeUser = true,
            string adminRole = "Admin", bool includeBackupAdmin = false, bool empty = false, bool fault = false)
            => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach (var setting in Settings(password, includeUser, adminRole, includeBackupAdmin, empty))
                builder.UseSetting(setting.Key, setting.Value);
            if (fault) builder.ConfigureServices(services =>
            {
                services.RemoveAll<IIdentityProvisioningFault>();
                services.AddSingleton<IIdentityProvisioningFault, ThrowingProvisioningFault>();
            });
        });

        private Dictionary<string, string?> Settings(string password, bool includeUser, string adminRole,
            bool includeBackupAdmin, bool empty)
        {
            var settings = new Dictionary<string, string?>
            {
                ["Persistence:DatabasePath"] = database,
                ["DataProtection:KeyDirectory"] = keys,
                ["DataProtection:ApplicationName"] = "Alerts.BE004.Tests"
            };
            if (empty) return settings;
            var index = 0;
            if (includeUser) AddAccount(settings, index++, UserId, "user-a@example.test", password, "User");
            AddAccount(settings, index++, AdminId, "admin@example.test", password, adminRole);
            if (includeBackupAdmin)
                AddAccount(settings, index, "00000000-0000-0000-0000-000000000104", "backup-admin@example.test", password, "Admin");
            return settings;
        }

        private static void AddAccount(Dictionary<string, string?> settings, int index, string id, string email, string password, string role)
        {
            var prefix = $"Identity:Provisioning:Accounts:{index}:";
            settings[prefix + "Id"] = id;
            settings[prefix + "Email"] = email;
            settings[prefix + "Password"] = password;
            settings[prefix + "Role"] = role;
        }

        public async Task<IdentityUser> ReadUserAsync(string id)
        {
            await using var db = new AlertsDbContext(DatabaseLifecycle.Options(database));
            return await db.Users.AsNoTracking().SingleAsync(x => x.Id == id, TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            for (var attempt = 0; attempt < 20 && Directory.Exists(root); attempt++)
            {
                try { Directory.Delete(root, recursive: true); }
                catch (IOException) when (attempt < 19) { await Task.Delay(25); }
            }
        }
    }

    private sealed class ThrowingProvisioningFault : IIdentityProvisioningFault
    {
        public void BeforeCommit() => throw new SyntheticProvisioningFaultException();
    }

    private sealed class SyntheticProvisioningFaultException : Exception;
}
