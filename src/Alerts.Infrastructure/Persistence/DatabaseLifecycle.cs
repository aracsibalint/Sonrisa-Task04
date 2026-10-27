using Alerts.Application;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Alerts.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Data.Common;
using Alerts.Application.Rules;

namespace Alerts.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    // Host owns/disposes the lifetime owner; it must be acquired before constructing writer services.
    public static IServiceCollection AddAlertsPersistence(this IServiceCollection services, WindowsDatabaseOwner owner)
    {
        owner.RequireActive();
        services.AddSingleton(owner);
        services.AddSingleton<IMutationGate, MutationGate>();
        services.AddSingleton<IFreeSpaceProbe, VolumeFreeSpaceProbe>();
        services.AddSingleton(sp => new StorageAdmission(owner.DatabasePath, sp.GetRequiredService<IFreeSpaceProbe>()));
        services.AddScoped(sp => new AlertsDbContext(DatabaseLifecycle.Options(owner.DatabasePath)));
        services.AddScoped<PersistenceUnitOfWork>();
        services.AddSingleton<IPasswordHasher<IdentityUser>, PasswordHasher<IdentityUser>>();
        services.AddSingleton<IIdentityLogin, IdentityLoginService>();
        services.AddSingleton<IIdentityProvisioningFault, NoIdentityProvisioningFault>();
        services.AddSingleton<IdentityProvisioner>();
        services.AddSingleton<IRulePersistence, RulePersistence>();
        services.AddSingleton<RuleUseCases>();
        return services;
    }
}

// The outer Application operation already owns admission. Scoped participants share this context;
// they do NOT acquire the gate. No HTTP, SMTP, password hashing or other external I/O in the callback.
public sealed class PersistenceUnitOfWork(AlertsDbContext context, WindowsDatabaseOwner owner, StorageAdmission storage)
{
    public async Task<T> ExecuteAsync<T>(MutationAdmission admission, MutationPurpose purpose,
        Func<AlertsDbContext, CancellationToken, Task<T>> databaseOnlyOperation, CancellationToken cancellationToken = default)
    {
        admission.RequireActive();
        owner.RequireActive();
        storage.Check(purpose);
        context.Admit(admission, owner, storage, purpose);
        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var result = await databaseOnlyOperation(context, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            storage.Check(purpose);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            // A failure here is an uncertain acknowledgement, not a rolled-back commit.
            // Latch readiness/mutations off; durable idempotency resolves a later retry.
            await DatabaseLifecycle.CheckpointAsync(owner, storage, purpose, cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (StorageUnavailableException) { throw; }
        catch (SqliteException e) when (SqliteStorageFailures.IsStorageAvailability(e))
        { storage.MarkFaulted(); throw new StorageUnavailableException(); }
        catch (DbUpdateException e) when (e.InnerException is SqliteException sqlite && SqliteStorageFailures.IsStorageAvailability(sqlite))
        { storage.MarkFaulted(); throw new StorageUnavailableException(); }
        // Disposal rolls back on cancellation or any exception; do not reuse this scope after failure.
    }
}

public static class DatabaseLifecycle
{
    public static DbContextOptions<AlertsDbContext> Options(string path) => new DbContextOptionsBuilder<AlertsDbContext>()
        .UseSqlite(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true, DefaultTimeout = 1, Pooling = false
        }.ToString()).Options;

    public static async Task MigrateAsync(WindowsDatabaseOwner owner, StorageAdmission storage, IMutationGate gate,
        CancellationToken cancellationToken = default)
    {
        await gate.ExecuteAsync(async (admission, ct) =>
        {
            admission.RequireActive();
            owner.RequireActive();
            storage.Check(MutationPurpose.Normal);
            await using var context = new AlertsDbContext(Options(owner.DatabasePath));
            try
            {
                var expected = context.Database.GetMigrations().ToArray();
                var applied = (await context.Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false)).ToArray();
                if (!expected.Take(applied.Length).SequenceEqual(applied))
                    throw new InvalidOperationException("Unknown migration history; no automatic repair is supported.");
                if (applied.Length == 0 && await context.Database.SqlQueryRaw<long>(
                    "SELECT count(*) AS Value FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT IN ('__EFMigrationsHistory','__EFMigrationsLock')")
                    .SingleAsync(ct).ConfigureAwait(false) != 0)
                    throw new InvalidOperationException("Nonempty unversioned database; explicit operator review is required.");
                await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", ct).ConfigureAwait(false);
                await context.Database.MigrateAsync(ct).ConfigureAwait(false);
                await CheckpointAsync(owner, storage, MutationPurpose.Normal, ct).ConfigureAwait(false);
            }
            catch (SqliteException) { storage.MarkFaulted(); throw; }
            return true;
        }, cancellationToken).ConfigureAwait(false);
    }

    public static async Task RequireCurrentSchemaAsync(WindowsDatabaseOwner owner, CancellationToken cancellationToken = default)
    {
        owner.RequireActive();
        await using var context = new AlertsDbContext(Options(owner.DatabasePath));
        var expected = context.Database.GetMigrations().ToArray();
        var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false)).ToArray();
        if (expected.Length == 0 || !expected.SequenceEqual(applied))
            throw new InvalidOperationException("Schema missing or incompatible; stop the app and run the reviewed migration command.");
        // Streaming projections validate old decimal/snapshot representations without loading history in memory.
        await foreach (var value in context.Set<EventRow>().AsNoTracking().Select(x => new
        { x.OpenPriceText, x.LastPriceText, x.SignedMovementPercentText, x.ProviderMovementPercentText, x.MagnitudeText })
            .AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false)) { _ = value; }
        await foreach (var value in context.Set<RuleRow>().AsNoTracking().Select(x => new { x.ThresholdPercentText, x.MinimumMagnitudeText })
            .AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false)) { _ = value; }
        await foreach (var value in context.Set<NotificationIntentRow>().AsNoTracking().Select(x => new { x.Category, x.FilterSnapshotJson })
            .AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
            FilterSnapshot.Validate(value.Category, value.FilterSnapshotJson);
    }

    // Invoke only while outer admission is held. Nonzero busy result is a storage fault, not success.
    internal static async Task CheckpointAsync(WindowsDatabaseOwner owner, StorageAdmission storage, MutationPurpose purpose, CancellationToken cancellationToken)
    {
        owner.RequireActive();
        storage.Check(purpose);
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = owner.DatabasePath, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true, DefaultTimeout = 1, Pooling = false }.ToString());
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false) || reader.GetInt32(0) != 0)
                throw new StorageUnavailableException();
        }
        catch { storage.MarkFaulted(); throw; }
    }
}

// Model/SQL generation only. Tooling never opens the configured application DB. Use --migrate
// on the API executable for disk migrations (ownership + space checks); not `ef database update`.
public sealed class AlertsDesignTimeFactory : IDesignTimeDbContextFactory<AlertsDbContext>
{
    public AlertsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<AlertsDbContext>()
        .UseSqlite("Data Source=:memory:;Foreign Keys=True;Default Timeout=1")
        .AddInterceptors(new ModelOnlyConnectionGuard()).Options);
}

internal sealed class ModelOnlyConnectionGuard : DbConnectionInterceptor
{
    private static NotSupportedException Refused() => new("Design-time context is model/SQL only. Use the ownership-guarded --migrate command; ef database update/drop is unsupported.");
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) => throw Refused();
    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) => throw Refused();
}
