using Alerts.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Alerts.Infrastructure.Persistence;

public sealed class AlertsDbContext(DbContextOptions<AlertsDbContext> options) : IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
    private MutationAdmission? admission;
    private WindowsDatabaseOwner? owner;
    private StorageAdmission? storage;
    private MutationPurpose purpose;

    internal void Admit(MutationAdmission current, WindowsDatabaseOwner lifetimeOwner, StorageAdmission policy, MutationPurpose mutationPurpose)
    {
        if (admission is not null) throw new InvalidOperationException("Context already admitted.");
        current.RequireActive();
        admission = current;
        owner = lifetimeOwner;
        storage = policy;
        purpose = mutationPurpose;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        PersistenceModel.Configure(builder);
    }

    private void ValidateWrite()
    {
        if (admission is null || owner is null || storage is null)
            throw new InvalidOperationException("Writes require outer mutation admission and process ownership.");
        admission.RequireActive();
        owner.RequireActive();
        storage.Check(purpose);
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;
            if (purpose == MutationPurpose.IdentitySecurityBookkeeping
                && (entry.State != EntityState.Modified || entry.Entity is not IdentityUser
                    || entry.Properties.Any(p => p.IsModified && p.Metadata.Name is not ("AccessFailedCount" or "LockoutEnd" or "ConcurrencyStamp" or "SecurityStamp"))))
                throw new InvalidOperationException("Emergency Identity admission is restricted to essential security bookkeeping.");
            if (purpose == MutationPurpose.SendResultOrRecovery
                && (entry.State != EntityState.Modified || entry.Entity is not (NotificationIntentRow or DeliveryAttemptRow)))
                throw new InvalidOperationException("Emergency delivery admission cannot create work or grow unrelated history.");
            if (purpose == MutationPurpose.SendResultOrRecovery && entry.Entity is NotificationIntentRow)
                ValidateEmergencyIntentTransition(entry);
            if (entry.State is EntityState.Added or EntityState.Modified && entry.Entity is NotificationIntentRow intent)
                FilterSnapshot.Validate(intent.Category, intent.FilterSnapshotJson);
            if (entry.State == EntityState.Deleted
                && (purpose != MutationPurpose.IdentityProvisioning || entry.Entity is not IdentityUserRole<string>))
                throw new InvalidOperationException("Physical deletion is not supported.");
            if (entry.State != EntityState.Modified) continue;
            if (entry.Entity is EventRow or SeenSourceItemRow)
                throw new InvalidOperationException("Accepted event/seen evidence is immutable.");
            if (entry.Entity is NotificationIntentRow && entry.OriginalValues.GetValue<string>("Status") is "Delivered" or "Failed"
                || entry.Entity is DeliveryAttemptRow && entry.OriginalValues.GetValue<string>("Outcome") != "InFlight")
                throw new InvalidOperationException("Terminal delivery evidence cannot be changed.");
            foreach (var property in entry.Properties.Where(p => p.IsModified))
            {
                if (entry.Entity is NotificationIntentRow && !PersistenceModel.MutableIntentFields.Contains(property.Metadata.Name)
                    || entry.Entity is ChannelDestinationRow && property.Metadata.Name is not ("IsAvailable" or "RevokedAtUtcMs")
                    || entry.Entity is DeliveryAttemptRow && property.Metadata.Name is not ("FinishedAtUtcMs" or "Outcome" or "ErrorCode" or "ErrorSummary" or "HttpStatus" or "SmtpStatus" or "RetryAfterMs")
                    || entry.Entity is RuleRow && property.Metadata.Name == "Category")
                    throw new InvalidOperationException("Immutable persistence evidence cannot be updated.");
            }
        }
    }

    private static void ValidateEmergencyIntentTransition(EntityEntry entry)
    {
        var intent = (NotificationIntentRow)entry.Entity;
        var original = entry.OriginalValues;
        if (original.GetValue<string>("Status") != "InFlight"
            || original.GetValue<string?>("ClaimToken") is null || original.GetValue<long?>("LeaseExpiresAtUtcMs") is null
            || original.GetValue<long>("AttemptCount") is < 1 or > 5
            || intent.AttemptCount != original.GetValue<long>("AttemptCount")
            || intent.Status is not ("Delivered" or "Failed" or "RetryScheduled")
            || intent.ClaimToken is not null || intent.LeaseExpiresAtUtcMs is not null
            || intent.Version != checked(original.GetValue<long>("Version") + 1))
            throw new InvalidOperationException("Emergency delivery admission can only finish an existing claim without increasing its ordinal.");
    }

    private async Task ValidateEmergencyDeliveryPairsAsync(CancellationToken cancellationToken)
    {
        if (purpose != MutationPurpose.SendResultOrRecovery) return;
        var intents = ChangeTracker.Entries<NotificationIntentRow>().Where(e => e.State == EntityState.Modified).ToArray();
        var attempts = ChangeTracker.Entries<DeliveryAttemptRow>().Where(e => e.State == EntityState.Modified).ToArray();
        if (intents.Length != attempts.Length)
            throw new InvalidOperationException("Emergency completion requires both the claimed intent and its existing attempt.");

        foreach (var intentEntry in intents)
        {
            var intent = intentEntry.Entity;
            var paired = attempts.Where(e => e.Entity.IntentId == intent.Id).ToArray();
            if (paired.Length != 1)
                throw new InvalidOperationException("Emergency completion requires exactly one matching existing attempt.");
            var attemptEntry = paired[0];
            var attempt = attemptEntry.Entity;
            var originalToken = intentEntry.OriginalValues.GetValue<string>("ClaimToken");
            var originalLease = intentEntry.OriginalValues.GetValue<long?>("LeaseExpiresAtUtcMs");
            var compatible = intent.Status switch
            {
                "Delivered" => attempt.Outcome == "Succeeded",
                "RetryScheduled" => intent.AttemptCount < 5 && (attempt.Outcome is "TransientFailure" or "OutcomeUnknown"),
                "Failed" => attempt.Outcome == "PermanentFailure"
                    || intent.AttemptCount == 5 && (attempt.Outcome is "TransientFailure" or "OutcomeUnknown"),
                _ => false
            };
            if (attemptEntry.OriginalValues.GetValue<string>("Outcome") != "InFlight"
                || attemptEntry.OriginalValues.GetValue<long?>("FinishedAtUtcMs") is not null
                || attempt.ClaimToken != originalToken || attempt.Ordinal != intent.AttemptCount
                || attempt.LeaseExpiresAtUtcMs != originalLease
                || attempt.FinishedAtUtcMs is null || attempt.FinishedAtUtcMs < attempt.StartedAtUtcMs || !compatible)
                throw new InvalidOperationException("Emergency completion does not agree with the original claim and attempt outcome.");

            // Validate persisted claims as well as EF originals: detached/stale snapshots must not
            // fabricate emergency eligibility. The outer gate and transaction remain owned once.
            var persistedIntent = await Set<NotificationIntentRow>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == intent.Id, cancellationToken).ConfigureAwait(false);
            var persistedAttempt = await Set<DeliveryAttemptRow>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == attempt.Id, cancellationToken).ConfigureAwait(false);
            if (persistedIntent is null || persistedAttempt is null
                || persistedIntent.Status != "InFlight" || persistedIntent.ClaimToken != originalToken
                || persistedIntent.LeaseExpiresAtUtcMs != originalLease || persistedIntent.AttemptCount != intent.AttemptCount
                || persistedIntent.Version != intentEntry.OriginalValues.GetValue<long>("Version")
                || persistedAttempt.Outcome != "InFlight" || persistedAttempt.FinishedAtUtcMs is not null
                || persistedAttempt.IntentId != intent.Id || persistedAttempt.ClaimToken != originalToken
                || persistedAttempt.Ordinal != intent.AttemptCount || persistedAttempt.LeaseExpiresAtUtcMs != originalLease
                || persistedAttempt.StartedAtUtcMs != attempt.StartedAtUtcMs)
                throw new InvalidOperationException("Persisted claim evidence does not authorize this emergency completion.");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new NotSupportedException("Use the admitted asynchronous unit of work for all writes.");
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateWrite();
        try
        {
            await ValidateEmergencyDeliveryPairsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var entry in ChangeTracker.Entries<NotificationIntentRow>().Where(e => e.State == EntityState.Added))
            {
                var i = entry.Entity;
                var ev = Set<EventRow>().Local.SingleOrDefault(x => x.Id == i.EventId)
                    ?? await Set<EventRow>().SingleAsync(x => x.Id == i.EventId, cancellationToken).ConfigureAwait(false);
                var rule = Set<RuleRow>().Local.SingleOrDefault(x => x.Id == i.RuleId)
                    ?? await Set<RuleRow>().SingleAsync(x => x.Id == i.RuleId, cancellationToken).ConfigureAwait(false);
                var destination = Set<ChannelDestinationRow>().Local.SingleOrDefault(x => x.Id == i.DestinationId)
                    ?? await Set<ChannelDestinationRow>().SingleAsync(x => x.Id == i.DestinationId, cancellationToken).ConfigureAwait(false);
                FilterSnapshot.RequireRuleEvidence(rule, i.FilterSnapshotJson);
                if (i.Category != ev.Category || i.Category != rule.Category || i.RuleVersion != rule.Version || i.RuleNameSnapshot != rule.Name
                    || i.DestinationConfigurationVersion != destination.ConfigurationVersion || i.DestinationIdentitySnapshot != destination.DestinationIdentity
                    || i.RecipientAddressSnapshot != destination.Address || i.SecretReferenceSnapshot != destination.SecretReference)
                    throw new InvalidOperationException("Intent snapshots do not agree with referenced immutable evidence.");
            }
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
        }
        catch (Microsoft.Data.Sqlite.SqliteException e) when (SqliteStorageFailures.IsStorageAvailability(e)) { storage!.MarkFaulted(); throw; }
        catch (DbUpdateException e) when (e.InnerException is Microsoft.Data.Sqlite.SqliteException sqlite && SqliteStorageFailures.IsStorageAvailability(sqlite))
        { storage!.MarkFaulted(); throw; }
    }
}
