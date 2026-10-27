using System.Diagnostics;
using Xunit;
using System.Runtime.InteropServices;
using Alerts.Application;
using Alerts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Alerts.IntegrationTests.Persistence;

public sealed class PersistenceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UnitOfWork_SchemaAndConstraintErrors_AreNotStorage503OrLatched()
    {
        await using var db = await Sandbox.CreateAsync();
        var schemaError = await Assert.ThrowsAsync<SqliteException>(() => db.WriteAsync(async (c, ct) =>
        {
            await c.Database.ExecuteSqlRawAsync("SELECT * FROM TableThatDoesNotExist;", ct);
            return true;
        }));
        Assert.Equal(1, schemaError.SqliteErrorCode & 0xff);
        Assert.False(db.Storage.IsFaulted);

        var constraint = await Assert.ThrowsAsync<DbUpdateException>(() => db.WriteAsync((c, _) =>
        {
            c.Roles.Add(new IdentityRole
            {
                Id = "00000000-0000-0000-0000-000000000001",
                Name = "Duplicate",
                NormalizedName = "DUPLICATE",
                ConcurrencyStamp = "duplicate"
            });
            return Task.FromResult(true);
        }));
        var constraintSqlite = Assert.IsType<SqliteException>(constraint.InnerException);
        Assert.Equal(19, constraintSqlite.SqliteErrorCode & 0xff);
        Assert.False(db.Storage.IsFaulted);
    }
    [Fact]
    public async Task Migrations_ReapplyReopen_RealConstraintsAndIdentityRelationships()
    {
        await using var db = await Sandbox.CreateAsync();
        await DatabaseLifecycle.MigrateAsync(db.Owner, db.Storage, db.Gate, Ct);
        await DatabaseLifecycle.RequireCurrentSchemaAsync(db.Owner, Ct);
        await db.WriteAsync(async (c, ct) =>
        {
            c.Users.Add(User());
            c.UserRoles.Add(new IdentityUserRole<string> { UserId = UserId, RoleId = "00000000-0000-0000-0000-000000000001" });
            c.Add(Source());
            await c.SaveChangesAsync(ct);
            return true;
        });
        await using var read = db.Context();
        Assert.Equal(2, await read.Roles.CountAsync(Ct));
        Assert.Single(await read.UserRoles.ToListAsync(Ct));
        Assert.Equal(123L, (await read.Users.SingleAsync(Ct)).LockoutEnd!.Value.ToUnixTimeMilliseconds());
        Assert.Equal(1L, await Scalar(db, "PRAGMA foreign_keys;"));
        Assert.Equal(0L, await Scalar(db, "SELECT count(*) FROM sqlite_master WHERE name IN ('Match','RuleMatch','AspNetUserPasskeys');"));
        Assert.Equal("wal", await TextScalar(db, "PRAGMA journal_mode;"));
        await AssertSqlRejected(db, $"INSERT INTO AspNetUserRoles(UserId,RoleId) VALUES('{Guid.NewGuid():D}','00000000-0000-0000-0000-000000000001');");
        await AssertSqlRejected(db, $"DELETE FROM AspNetUsers WHERE Id='{UserId}';");
    }

    [Fact]
    public async Task EventUniquenessForeignKeysCategoryFieldsAndSeen_AreEnforced()
    {
        await using var db = await Sandbox.CreateAsync();
        await Seed(db);
        var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => db.WriteAsync(async (c, ct) =>
        {
            var copy = await c.Set<EventRow>().AsNoTracking().SingleAsync(ct); copy.Id = NewId(); c.Add(copy); return true;
        }));
        Assert.Contains("Event.SourceId, Event.ExternalKey", duplicate.InnerException!.Message);
        await AssertSqlRejected(db, "UPDATE Event SET Title=NULL;");
        await AssertSqlRejected(db, "UPDATE Event SET MagnitudeText='4';");
        await AssertSqlRejected(db, "UPDATE Event SET EvaluatedRuleCount=101;");
        await AssertSqlRejected(db, $"UPDATE Event SET SourceId='{Guid.NewGuid():D}';");
        await db.WriteAsync((c, _) =>
        {
            c.Add(new SeenSourceItemRow { SourceId = SourceId, ExternalKey = "key", Disposition = "Accepted", FirstSeenAtUtcMs = 1, EventId = EventId, PayloadFingerprint = new byte[32] });
            return Task.FromResult(true);
        });
        await AssertSqlRejected(db, "UPDATE SeenSourceItem SET ExternalKey='wrong';");
        await AssertSqlRejected(db, "UPDATE SeenSourceItem SET Disposition='OldSuppressed';");
        await AssertSqlRejected(db, "UPDATE SourceState SET Enabled=1,ActivatedAtUtcMs=NULL;");
        await AssertSqlRejected(db, "UPDATE SourceState SET Kind='Market';");
    }

    [Fact]
    public async Task IntentChannelsSnapshotsStatesAndAttemptConstraints_AreRealSQLite()
    {
        await using var db = await Sandbox.CreateAsync();
        await Seed(db, intents: true);
        await using var read = db.Context();
        Assert.Equal(2, await read.Set<NotificationIntentRow>().CountAsync(Ct));
        await AssertSqlRejected(db, "UPDATE NotificationIntent SET FilterSnapshotJson='[]';");
        await AssertSqlRejected(db, "UPDATE NotificationIntent SET RuleVersion=0;");
        await AssertSqlRejected(db, "UPDATE NotificationIntent SET Status='InFlight';");
        await AssertSqlRejected(db, "UPDATE NotificationIntent SET Status='Failed',NextAttemptAtUtcMs=NULL,FailedAtUtcMs=1,LastErrorCode='Other';");
        await AssertSqlRejected(db, "UPDATE NotificationIntent SET Channel='Other';");
        await AssertSqlRejected(db, "DELETE FROM Rule;");
        var duplicate = await Assert.ThrowsAsync<DbUpdateException>(() => db.WriteAsync(async (c, ct) =>
        {
            var copy = await c.Set<NotificationIntentRow>().AsNoTracking().FirstAsync(ct); copy.Id = NewId(); c.Add(copy); return true;
        }));
        Assert.Contains("NotificationIntent.EventId, NotificationIntent.RuleId, NotificationIntent.Channel", duplicate.InnerException!.Message);
        await db.WriteAsync(async (c, ct) =>
        {
            var intent = await c.Set<NotificationIntentRow>().SingleAsync(x => x.Channel == "Email", ct);
            intent.Status = "InFlight"; intent.AttemptCount = 1; intent.NextAttemptAtUtcMs = null;
            intent.ClaimToken = Claim; intent.LeaseExpiresAtUtcMs = 120000; intent.Version++;
            c.Add(new DeliveryAttemptRow { Id = NewId(), IntentId = intent.Id, Ordinal = 1, ClaimToken = Claim, StartedAtUtcMs = 1, LeaseExpiresAtUtcMs = 120000, Outcome = "InFlight" });
            return true;
        });
        await AssertSqlRejected(db, "UPDATE DeliveryAttempt SET Outcome='Succeeded';");
        await AssertSqlRejected(db, "UPDATE DeliveryAttempt SET Ordinal=6;");
        await AssertSqlRejected(db, "UPDATE DeliveryAttempt SET LeaseExpiresAtUtcMs=StartedAtUtcMs;");
        await db.WriteAsync(async (c, ct) =>
        {
            var attempt = await c.Set<DeliveryAttemptRow>().SingleAsync(ct);
            attempt.Outcome = "OutcomeUnknown"; attempt.FinishedAtUtcMs = 120001;
            var intent = await c.Set<NotificationIntentRow>().SingleAsync(x => x.Channel == "Email", ct);
            intent.Status = "RetryScheduled"; intent.ClaimToken = null; intent.LeaseExpiresAtUtcMs = null;
            intent.NextAttemptAtUtcMs = 150001; intent.Version++;
            return true;
        });
        Assert.Equal("OutcomeUnknown", await TextScalar(db, "SELECT Outcome FROM DeliveryAttempt;"));
    }

    [Fact]
    public async Task DecimalTimeAndDueQuery_RoundTripExactly_AndUseNumericIndex()
    {
        await using var db = await Sandbox.CreateAsync();
        await Seed(db, intents: true);
        await db.WriteAsync((c, _) =>
        {
            c.Add(new RuleRow { Id = NewId(), OwnerId = UserId, Name = "price", Category = "Market", Instrument = "BTCUSDT", ComparisonKind = "binance-rolling-24h", ThresholdPercentText = "0.00000001", EmailSelected = true, Version = 1, CreatedAtUtcMs = -62135596800000, UpdatedAtUtcMs = 253402300799999 });
            return Task.FromResult(true);
        });
        await using var read = db.Context();
        var price = await read.Set<RuleRow>().SingleAsync(x => x.Category == "Market", Ct);
        Assert.Equal("0.00000001", price.ThresholdPercentText);
        Assert.Equal(253402300799999, price.UpdatedAtUtcMs);
        var due = read.Set<NotificationIntentRow>().AsNoTracking().Where(x => (x.Status == "Pending" || x.Status == "RetryScheduled") && x.NextAttemptAtUtcMs <= 10)
            .OrderBy(x => x.NextAttemptAtUtcMs).ThenBy(x => x.CreatedAtUtcMs).ThenBy(x => x.Id).Take(10);
        Assert.Equal(2, await due.CountAsync(Ct));
        Assert.Contains("NextAttemptAtUtcMs", due.ToQueryString());
        Assert.Contains("INDEX", await TextScalar(db, "EXPLAIN QUERY PLAN SELECT Id FROM NotificationIntent WHERE Status='Pending' AND NextAttemptAtUtcMs<=10 ORDER BY NextAttemptAtUtcMs,CreatedAtUtcMs,Id LIMIT 10;", 3));
        await AssertSqlRejected(db, "UPDATE Rule SET CreatedAtUtcMs=253402300800000;");
        await Assert.ThrowsAnyAsync<Exception>(() => db.WriteAsync(async (c, ct) =>
        {
            var r = await c.Set<RuleRow>().SingleAsync(x => x.Category == "Market", ct);
            r.ThresholdPercentText = "0.000000001";
            return true;
        }));
    }

    [Fact]
    public async Task ExceptionAndCancellation_RollBack_EvenAfterSave_AndNextWriterCommits()
    {
        await using var db = await Sandbox.CreateAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.WriteAsync<bool>(async (c, ct) =>
        {
            c.Add(Source()); await c.SaveChangesAsync(ct); throw new InvalidOperationException("injected before commit");
        }));
        await using (var read = db.Context()) Assert.Empty(await read.Set<SourceStateRow>().ToListAsync(Ct));
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => db.WriteAsync(async (c, ct) =>
        {
            c.Add(Source()); await c.SaveChangesAsync(ct); cancellation.Cancel(); ct.ThrowIfCancellationRequested(); return true;
        }, cancellation.Token));
        await db.WriteAsync((c, _) => { c.Add(Source()); return Task.FromResult(true); });
        await using var reopened = db.Context();
        Assert.Single(await reopened.Set<SourceStateRow>().ToListAsync(Ct));
    }

    [Fact]
    public async Task ImmutableSnapshotsAndConcurrentRuleVersion_AreGuarded()
    {
        await using var db = await Sandbox.CreateAsync(); await Seed(db, intents: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.WriteAsync(async (c, ct) =>
        {
            (await c.Set<NotificationIntentRow>().FirstAsync(ct)).BodySnapshot = "changed";
            return true;
        }));
        await db.WriteAsync(async (c, ct) =>
        {
            var rule = await c.Set<RuleRow>().SingleAsync(ct); rule.Enabled = false; rule.DeletedAtUtcMs = 5; rule.Version++;
            return true;
        });
        await using var read = db.Context();
        Assert.All(await read.Set<NotificationIntentRow>().ToListAsync(Ct), i => Assert.Equal(1, i.RuleVersion));
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db.WriteAsync((c, _) =>
        {
            var stale = Rule(); c.Attach(stale); stale.Name = "stale"; stale.Version = 2;
            return Task.FromResult(true);
        }));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"schemaVersion\":1,\"keyword\":\"moon\",\"keyword\":\"moon\"}")]
    [InlineData("{\"schemaVersion\":2,\"keyword\":\"moon\"}")]
    public async Task FilterSnapshot_InvalidSchema_IsRejectedByApplicationBoundary(string json)
    {
        await using var db = await Sandbox.CreateAsync(); await Seed(db, intents: true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.WriteAsync(async (c, ct) =>
        {
            var copy = await c.Set<NotificationIntentRow>().AsNoTracking().FirstAsync(ct);
            copy.Id = NewId(); copy.FilterSnapshotJson = json; c.Add(copy); return true;
        }));
    }

    [Fact]
    public async Task MaximumExternalKey_AndDistinctSourceIdentity_RoundTrip_WithoutTruncation()
    {
        await using var db = await Sandbox.CreateAsync(); await Seed(db);
        var maximum = "v1|" + new string('a', 12285);
        await db.WriteAsync(async (c, ct) =>
        {
            var second = Source(); second.Id = NewId(); second.LogicalName = "second"; c.Add(second);
            var copy = await c.Set<EventRow>().AsNoTracking().SingleAsync(ct); copy.Id = NewId(); copy.SourceId = second.Id; c.Add(copy);
            var large = new EventRow { Id = NewId(), SourceId = SourceId, ExternalKey = maximum, Category = "News", Provenance = "Live", SchemaVersion = 1, FingerprintVersion = 1, PayloadFingerprint = new byte[32], PredicateVersion = 1, Title = "large", SummaryTruncated = false };
            c.Add(large); return true;
        });
        await using var read = db.Context();
        Assert.Equal(maximum, (await read.Set<EventRow>().SingleAsync(x => x.ExternalKey == maximum, Ct)).ExternalKey);
        Assert.Equal(2, await read.Set<EventRow>().CountAsync(x => x.ExternalKey == "key", Ct));
    }

    [Fact]
    public async Task DiskRecheckBeforeCommit_RollsBack_WithoutFillingDisk()
    {
        await using var db = await Sandbox.CreateAsync();
        await Assert.ThrowsAsync<StorageUnavailableException>(() => db.WriteAsync(async (c, ct) =>
        {
            c.Add(Source()); await c.SaveChangesAsync(ct); db.Space.Bytes = 0; return true;
        }));
        db.Space.Bytes = long.MaxValue;
        await using var read = db.Context(); Assert.Empty(await read.Set<SourceStateRow>().ToListAsync(Ct));
        db.Storage.MarkFaulted();
        await Assert.ThrowsAsync<StorageUnavailableException>(() => db.WriteAsync((c, _) => Task.FromResult(true)));
    }

    [Fact]
    public async Task EmergencyAdmission_AllowsOnlyBoundedSecurityOrCompletion_NotOrdinaryGrowth()
    {
        await using var db = await Sandbox.CreateAsync(); await Seed(db);
        db.Space.Bytes = 269484032;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.WriteEmergencyAsync((c, _) =>
        {
            c.Add(Source()); return Task.FromResult(true);
        }, MutationPurpose.IdentitySecurityBookkeeping));
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.WriteEmergencyAsync((c, _) =>
        {
            c.Add(Source()); return Task.FromResult(true);
        }, MutationPurpose.SendResultOrRecovery));
        await db.WriteEmergencyAsync(async (c, ct) =>
        {
            (await c.Users.SingleAsync(ct)).AccessFailedCount++; return true;
        }, MutationPurpose.IdentitySecurityBookkeeping);
        await using var read = db.Context(); Assert.Equal(1, (await read.Users.SingleAsync(Ct)).AccessFailedCount);
    }

    [Fact]
    public async Task MissingSchema_IsNotAutomaticallyMigrated_AndUnadmittedWriteFails()
    {
        await using var db = await Sandbox.CreateAsync(migrate: false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseLifecycle.RequireCurrentSchemaAsync(db.Owner, Ct));
        await using var context = db.Context();
        context.Add(Source());
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(Ct));
        Assert.Equal(0L, await Scalar(db, "SELECT count(*) FROM sqlite_master WHERE name='SourceState';"));
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("RetryScheduled")]
    public async Task EmergencyDelivery_FreshClaimsAreRejected_AndOriginalWorkSurvives(string originalStatus)
    {
        await using var db = await Sandbox.CreateAsync();
        if (originalStatus == "Pending") await Seed(db, intents: true);
        else
        {
            await SeedClaim(db);
            await db.WriteAsync((c, ct) => FinishClaim(c, ct, "RetryScheduled", "TransientFailure"));
        }
        db.Space.Bytes = 269484032;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.WriteEmergencyAsync(async (c, ct) =>
        {
            var intent = await c.Set<NotificationIntentRow>().SingleAsync(x => x.Channel == "Email", ct);
            intent.Status = "InFlight"; intent.AttemptCount++; intent.NextAttemptAtUtcMs = null;
            intent.ClaimToken = NewId(); intent.LeaseExpiresAtUtcMs = 240000; intent.Version++;
            // Reproduces the reviewed bypass: no new attempt row, individually valid intent CHECKs.
            return true;
        }, MutationPurpose.SendResultOrRecovery));
        await using var read = db.Context();
        var persisted = await read.Set<NotificationIntentRow>().SingleAsync(x => x.Channel == "Email", Ct);
        Assert.Equal(originalStatus, persisted.Status);
        Assert.Equal(originalStatus == "Pending" ? 0 : 1, persisted.AttemptCount);
        Assert.Null(persisted.ClaimToken);
        Assert.Equal(originalStatus == "Pending" ? 0 : 1, await read.Set<DeliveryAttemptRow>().CountAsync(Ct));
    }

    [Theory]
    [InlineData("Delivered", "Succeeded", 1)]
    [InlineData("Failed", "PermanentFailure", 1)]
    [InlineData("RetryScheduled", "TransientFailure", 1)]
    [InlineData("RetryScheduled", "OutcomeUnknown", 1)]
    [InlineData("Failed", "OutcomeUnknown", 5)]
    [InlineData("Failed", "TransientFailure", 5)]
    public async Task EmergencyDelivery_ExistingClaimCompletesOrRecovers_WithoutNewOrdinal(string status, string outcome, int ordinal)
    {
        await using var db = await Sandbox.CreateAsync(); await SeedClaim(db, ordinal);
        db.Space.Bytes = 269484032;
        await db.WriteEmergencyAsync((c, ct) => FinishClaim(c, ct, status, outcome), MutationPurpose.SendResultOrRecovery);
        await using var read = db.Context();
        var intent = await read.Set<NotificationIntentRow>().SingleAsync(x => x.Channel == "Email", Ct);
        var attempt = await read.Set<DeliveryAttemptRow>().SingleAsync(x => x.Ordinal == ordinal, Ct);
        Assert.Equal(status, intent.Status); Assert.Equal(ordinal, intent.AttemptCount);
        Assert.Null(intent.ClaimToken); Assert.Null(intent.LeaseExpiresAtUtcMs);
        Assert.Equal(outcome, attempt.Outcome); Assert.Equal(120001, attempt.FinishedAtUtcMs);
        Assert.Equal(Claim, attempt.ClaimToken);
        Assert.Equal(ordinal, await read.Set<DeliveryAttemptRow>().CountAsync(Ct));
    }

    [Theory]
    [InlineData("IncrementOrdinal")]
    [InlineData("RotateClaim")]
    [InlineData("MissingAttempt")]
    [InlineData("MissingIntent")]
    [InlineData("WrongOutcome")]
    [InlineData("PrematureUnknownFailure")]
    public async Task EmergencyDelivery_InconsistentOrUnpairedCompletion_IsRejectedAtomically(string fault)
    {
        await using var db = await Sandbox.CreateAsync(); await SeedClaim(db);
        db.Space.Bytes = 269484032;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.WriteEmergencyAsync(async (c, ct) =>
        {
            await FinishClaim(c, ct, fault == "PrematureUnknownFailure" ? "Failed" : "Delivered",
                fault == "WrongOutcome" ? "TransientFailure" : fault == "PrematureUnknownFailure" ? "OutcomeUnknown" : "Succeeded");
            var intent = c.Set<NotificationIntentRow>().Local.Single(x => x.Channel == "Email");
            var attempt = c.Set<DeliveryAttemptRow>().Local.Single();
            if (fault == "IncrementOrdinal") intent.AttemptCount++;
            if (fault == "RotateClaim") { intent.Status = "InFlight"; intent.DeliveredAtUtcMs = null; intent.ClaimToken = NewId(); intent.LeaseExpiresAtUtcMs = 240000; }
            if (fault == "MissingAttempt") c.Entry(attempt).State = EntityState.Unchanged;
            if (fault == "MissingIntent") c.Entry(intent).State = EntityState.Unchanged;
            return true;
        }, MutationPurpose.SendResultOrRecovery));
        await using var read = db.Context();
        var persisted = await read.Set<NotificationIntentRow>().SingleAsync(x => x.Channel == "Email", Ct);
        var persistedAttempt = await read.Set<DeliveryAttemptRow>().SingleAsync(Ct);
        Assert.Equal("InFlight", persisted.Status); Assert.Equal(1, persisted.AttemptCount);
        Assert.Equal(Claim, persisted.ClaimToken); Assert.Equal("InFlight", persistedAttempt.Outcome);
        Assert.Null(persistedAttempt.FinishedAtUtcMs);
    }

    [Fact]
    public async Task EmergencyDelivery_FabricatedTrackedOriginals_CannotAuthorizeUnpersistedClaim()
    {
        await using var db = await Sandbox.CreateAsync(); await Seed(db, intents: true);
        db.Space.Bytes = 269484032;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.WriteEmergencyAsync(async (c, ct) =>
        {
            var fake = await c.Set<NotificationIntentRow>().AsNoTracking().SingleAsync(x => x.Channel == "Email", ct);
            fake.Status = "InFlight"; fake.AttemptCount = 1; fake.NextAttemptAtUtcMs = null;
            fake.ClaimToken = Claim; fake.LeaseExpiresAtUtcMs = 120000;
            c.Attach(fake);
            c.Attach(new DeliveryAttemptRow { Id = NewId(), IntentId = fake.Id, Ordinal = 1, ClaimToken = Claim,
                StartedAtUtcMs = 1, LeaseExpiresAtUtcMs = 120000, Outcome = "InFlight" });
            return await FinishClaim(c, ct, "Delivered", "Succeeded", trackedOnly: true);
        }, MutationPurpose.SendResultOrRecovery));
        await using var read = db.Context();
        Assert.Equal("Pending", (await read.Set<NotificationIntentRow>().SingleAsync(x => x.Channel == "Email", Ct)).Status);
        Assert.Empty(await read.Set<DeliveryAttemptRow>().ToListAsync(Ct));
    }

    [Fact]
    public async Task Ownership_RejectsSecondProcess_AndCrashReleasesWithoutDeletingSidecar()
    {
        await using var db = await Sandbox.CreateAsync();
        using (var second = StartMigration(db.Path))
        {
            await second.WaitForExitAsync(Ct);
            Assert.NotEqual(0, second.ExitCode);
        }
        var sidecar = db.Path + ".writer.lock";
        Assert.True(File.Exists(sidecar));
        Assert.Throws<IOException>(() => WindowsDatabaseOwner.Acquire(db.Path));
        db.Owner.Dispose();
        var info = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add(typeof(Program).Assembly.Location);
        info.ArgumentList.Add("--Persistence:DatabasePath"); info.ArgumentList.Add(db.Path);
        info.ArgumentList.Add("--DataProtection:KeyDirectory"); info.ArgumentList.Add(Path.Combine(Path.GetDirectoryName(db.Path)!, "keys"));
        info.ArgumentList.Add("--DataProtection:ApplicationName"); info.ArgumentList.Add("Alerts.Persistence.OwnerProbe");
        info.ArgumentList.Add("--Identity:Provisioning:Accounts:0:Id"); info.ArgumentList.Add("00000000-0000-0000-0000-000000000199");
        info.ArgumentList.Add("--Identity:Provisioning:Accounts:0:Email"); info.ArgumentList.Add("owner-probe@example.test");
        info.ArgumentList.Add("--Identity:Provisioning:Accounts:0:Password"); info.ArgumentList.Add("Synthetic-Owner-42!Probe");
        info.ArgumentList.Add("--Identity:Provisioning:Accounts:0:Role"); info.ArgumentList.Add("Admin");
        info.ArgumentList.Add("--urls"); info.ArgumentList.Add("http://127.0.0.1:0");
        using (var child = Process.Start(info)!)
        {
            try
            {
                using var limit = CancellationTokenSource.CreateLinkedTokenSource(Ct);
                limit.CancelAfter(TimeSpan.FromSeconds(10));
                string? line;
                do { line = await child.StandardOutput.ReadLineAsync(limit.Token); }
                while (line is not null && !line.Contains("Application started", StringComparison.Ordinal));
                Assert.NotNull(line);
                Assert.Throws<IOException>(() => WindowsDatabaseOwner.Acquire(db.Path));
            }
            finally
            {
                if (!child.HasExited) child.Kill(entireProcessTree: true);
                await child.WaitForExitAsync(Ct);
            }
        }
        using var recovered = WindowsDatabaseOwner.Acquire(db.Path);
        Assert.True(File.Exists(sidecar));
    }

    [Fact]
    public async Task WalCheckpointBusy_FailsClosed_WithoutDeletingData()
    {
        await using var db = await Sandbox.CreateAsync(); await Seed(db);
        await using var reader = db.Context();
        await reader.Database.OpenConnectionAsync(Ct);
        await using var snapshot = ((SqliteConnection)reader.Database.GetDbConnection()).BeginTransaction(deferred: true);
        await reader.Database.UseTransactionAsync(snapshot, Ct);
        Assert.Single(await reader.Set<SourceStateRow>().AsNoTracking().ToListAsync(Ct));
        await Assert.ThrowsAsync<StorageUnavailableException>(() => db.WriteAsync((c, _) =>
        {
            c.Add(new SourceFailureRow { Id = NewId(), SourceId = SourceId, Stage = "Fetch", Code = "Synthetic", Summary = "Synthetic" });
            return Task.FromResult(true);
        }));
        Assert.True(db.Storage.IsFaulted);
        Assert.Single(await reader.Set<SourceStateRow>().AsNoTracking().ToListAsync(Ct));
        await snapshot.RollbackAsync(Ct);
        await using var reopened = db.Context();
        // Commit preceded the failed checkpoint: caller got no success, but data must not be lost.
        Assert.Single(await reopened.Set<SourceFailureRow>().AsNoTracking().ToListAsync(Ct));
    }

    [Fact]
    public async Task ModelOnlyTooling_RefusesAnyConnection_EvenWithOverride()
    {
        await using var db = await Sandbox.CreateAsync(migrate: false);
        await using var context = new AlertsDesignTimeFactory().CreateDbContext([]);
        context.Database.SetConnectionString($"Data Source={db.Path};Foreign Keys=True");
        await Assert.ThrowsAsync<NotSupportedException>(() => context.Database.MigrateAsync(Ct));
        Assert.Equal(0L, await Scalar(db, "SELECT count(*) FROM sqlite_master WHERE name='SourceState';"));
    }

    [Fact]
    public async Task Ownership_RejectsHardlinksReparseAndUnsupportedPaths()
    {
        await using var db = await Sandbox.CreateAsync();
        Assert.Throws<IOException>(() => WindowsDatabaseOwner.Acquire(db.Path + ":alternate"));
        Assert.Throws<IOException>(() => WindowsDatabaseOwner.Acquire("\\\\localhost\\c$\\sandbox.db"));
        Assert.Throws<IOException>(() => WindowsDatabaseOwner.Acquire(System.IO.Path.GetDirectoryName(db.Path) + "\\.\\alerts.db"));
        db.Owner.Dispose();
        var alias = db.Path + ".alias";
        Assert.True(CreateHardLink(alias, db.Path, IntPtr.Zero));
        try
        {
            Assert.Throws<IOException>(() => WindowsDatabaseOwner.Acquire(alias));
            Assert.Throws<IOException>(() => WindowsDatabaseOwner.Acquire(db.Path));
        }
        finally { File.Delete(alias); }
        var junction = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(db.Path)!, "junction");
        using var command = Process.Start(new ProcessStartInfo("cmd.exe") { Arguments = $"/c mklink /J \"{junction}\" \"{System.IO.Path.GetDirectoryName(db.Path)}\"", CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true });
        await command!.WaitForExitAsync(Ct); Assert.Equal(0, command.ExitCode);
        try { Assert.Throws<IOException>(() => WindowsDatabaseOwner.Acquire(System.IO.Path.Combine(junction, "alerts.db"))); }
        finally { Directory.Delete(junction); }
    }

    private static Process StartMigration(string path)
    {
        var assembly = typeof(Program).Assembly.Location;
        var info = new ProcessStartInfo("dotnet") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true, RedirectStandardOutput = true };
        info.ArgumentList.Add(assembly); info.ArgumentList.Add("--migrate"); info.ArgumentList.Add(path);
        return Process.Start(info)!;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string newName, string existingName, IntPtr securityAttributes);

    private const string UserId = "10000000-0000-0000-0000-000000000001";
    private const string SourceId = "20000000-0000-0000-0000-000000000001";
    private const string EventId = "30000000-0000-0000-0000-000000000001";
    private const string RuleId = "40000000-0000-0000-0000-000000000001";
    private const string Claim = "50000000-0000-0000-0000-000000000001";
    private static string NewId() => Guid.NewGuid().ToString("D");
    private static IdentityUser User() => new() { Id = UserId, UserName = "synthetic@example.test", NormalizedUserName = "SYNTHETIC@EXAMPLE.TEST", Email = "synthetic@example.test", NormalizedEmail = "SYNTHETIC@EXAMPLE.TEST", PasswordHash = "synthetic-not-a-login", SecurityStamp = "security", ConcurrencyStamp = "concurrency", LockoutEnd = DateTimeOffset.FromUnixTimeMilliseconds(123) };
    private static SourceStateRow Source() => new() { Id = SourceId, LogicalName = "synthetic-rss", ConfigurationVersion = 1, Kind = "Rss", Category = "News", Provenance = "Live", DisplayName = "Synthetic", ConfigurationReference = "synthetic-source", IdentityVersion = 1, ScheduleVersion = 1, PollIntervalMs = 3600000, Health = "Disabled", Version = 1 };
    private static RuleRow Rule() => new() { Id = RuleId, OwnerId = UserId, Name = "moon", Category = "News", Keyword = "moon", EmailSelected = true, SlackSelected = true, Version = 1 };
    private static async Task SeedClaim(Sandbox db, int ordinal = 1)
    {
        await Seed(db, intents: true);
        await db.WriteAsync(async (c, ct) =>
        {
            var intent = await c.Set<NotificationIntentRow>().SingleAsync(x => x.Channel == "Email", ct);
            intent.Status = "InFlight"; intent.AttemptCount = ordinal; intent.NextAttemptAtUtcMs = null;
            intent.ClaimToken = Claim; intent.LeaseExpiresAtUtcMs = 120000; intent.Version++;
            for (var n = 1; n <= ordinal; n++)
                c.Add(new DeliveryAttemptRow { Id = NewId(), IntentId = intent.Id, Ordinal = n, ClaimToken = n == ordinal ? Claim : NewId(),
                    StartedAtUtcMs = 1, LeaseExpiresAtUtcMs = 120000, Outcome = n == ordinal ? "InFlight" : "TransientFailure", FinishedAtUtcMs = n == ordinal ? null : 2 });
            return true;
        });
    }

    private static async Task<bool> FinishClaim(AlertsDbContext context, CancellationToken ct, string status, string outcome, bool trackedOnly = false)
    {
        var intent = trackedOnly ? context.Set<NotificationIntentRow>().Local.Single(x => x.Channel == "Email")
            : await context.Set<NotificationIntentRow>().SingleAsync(x => x.Channel == "Email", ct);
        var attempt = trackedOnly ? context.Set<DeliveryAttemptRow>().Local.Single()
            : await context.Set<DeliveryAttemptRow>().SingleAsync(x => x.Outcome == "InFlight", ct);
        intent.Status = status; intent.ClaimToken = null; intent.LeaseExpiresAtUtcMs = null; intent.Version++;
        intent.NextAttemptAtUtcMs = status == "RetryScheduled" ? 150001 : null;
        intent.DeliveredAtUtcMs = status == "Delivered" ? 120001 : null;
        intent.FailedAtUtcMs = status == "Failed" ? 120001 : null;
        intent.LastErrorCode = status == "Failed" ? "SyntheticFailure" : null;
        attempt.Outcome = outcome; attempt.FinishedAtUtcMs = 120001;
        return true;
    }
    private static async Task Seed(Sandbox db, bool intents = false)
    {
        await db.WriteAsync((c, _) =>
        {
            c.Users.Add(User()); c.Add(Source()); c.Add(Rule());
            c.Add(new EventRow { Id = EventId, SourceId = SourceId, ExternalKey = "key", Category = "News", Provenance = "Live", SchemaVersion = 1, FingerprintVersion = 1, PredicateVersion = 1, PayloadFingerprint = new byte[32], Title = "moon", SummaryTruncated = false, EvaluatedRuleCount = 1, MatchedRuleCount = intents ? 1 : 0, IntentCount = intents ? 2 : 0 });
            if (intents)
                foreach (var channel in new[] { "Email", "Slack" })
                {
                    var destination = new ChannelDestinationRow { Id = NewId(), OwnerId = UserId, Channel = channel, ConfigurationVersion = 1, DestinationIdentity = "synthetic-target", Address = channel == "Email" ? "synthetic@example.test" : null, SecretReference = channel == "Slack" ? "synthetic-secret-ref" : null, IsAvailable = true };
                    c.Add(destination);
                    c.Add(new NotificationIntentRow { Id = NewId(), EventId = EventId, RuleId = RuleId, OwnerId = UserId, RuleVersion = 1, RuleNameSnapshot = "moon", Category = "News", FilterSnapshotJson = "{\"schemaVersion\":1,\"keyword\":\"moon\"}", PredicateVersion = 1, Channel = channel, DestinationId = destination.Id, DestinationConfigurationVersion = 1, DestinationIdentitySnapshot = "synthetic-target", RecipientAddressSnapshot = destination.Address, SecretReferenceSnapshot = destination.SecretReference, SubjectSnapshot = channel == "Email" ? "Synthetic" : null, BodySnapshot = "Synthetic moon", PayloadSchemaVersion = 1, Status = "Pending", NextAttemptAtUtcMs = 10, Version = 1 });
                }
            return Task.FromResult(true);
        });
    }

    private static async Task AssertSqlRejected(Sandbox db, string sql)
    {
        await db.Gate.ExecuteAsync(async (a, ct) =>
        {
            a.RequireActive(); db.Owner.RequireActive(); db.Storage.Check(MutationPurpose.Normal);
            await using var c = db.Context();
            var error = await Assert.ThrowsAsync<SqliteException>(() => c.Database.ExecuteSqlRawAsync(sql, ct));
            Assert.Equal(19, error.SqliteErrorCode);
            return true;
        });
    }
    private static async Task<long> Scalar(Sandbox db, string sql) => long.Parse(await TextScalar(db, sql), System.Globalization.CultureInfo.InvariantCulture);
    private static async Task<string> TextScalar(Sandbox db, string sql, int ordinal = 0)
    {
        await using var c = db.Context(); await c.Database.OpenConnectionAsync();
        await using var command = c.Database.GetDbConnection().CreateCommand(); command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(); Assert.True(await reader.ReadAsync());
        return Convert.ToString(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture)!;
    }

    private sealed class Space : IFreeSpaceProbe { public long Bytes = long.MaxValue; public long AvailableBytes(string _) => Bytes; }
    private sealed class Sandbox : IAsyncDisposable
    {
        public required string Path { get; init; }
        public required WindowsDatabaseOwner Owner { get; init; }
        public Space Space { get; } = new();
        public required StorageAdmission Storage { get; set; }
        public MutationGate Gate { get; } = new();
        public AlertsDbContext Context() => new(DatabaseLifecycle.Options(Path));
        public static async Task<Sandbox> CreateAsync(bool migrate = true)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Persistence integration tests require Windows local NTFS.");
            var tempRoot = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
            var drive = new DriveInfo(System.IO.Path.GetPathRoot(tempRoot)!);
            if (drive.DriveType != DriveType.Fixed || !string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
                throw new PlatformNotSupportedException("Persistence integration tests require a fixed local NTFS temp directory.");
            var directory = System.IO.Path.Combine(tempRoot, "opencode", "be003-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = System.IO.Path.Combine(directory, "alerts.db");
            var db = new Sandbox { Path = path, Owner = WindowsDatabaseOwner.Acquire(path, true), Storage = null! };
            db.Storage = new StorageAdmission(path, db.Space);
            try { if (migrate) await DatabaseLifecycle.MigrateAsync(db.Owner, db.Storage, db.Gate); return db; }
            catch { await db.DisposeAsync(); throw; }
        }
        public Task<T> WriteAsync<T>(Func<AlertsDbContext, CancellationToken, Task<T>> action) => WriteAsync(action, Ct);
        public Task<T> WriteEmergencyAsync<T>(Func<AlertsDbContext, CancellationToken, Task<T>> action, MutationPurpose purpose) => WriteAsync(action, Ct, purpose);
        public async Task<T> WriteAsync<T>(Func<AlertsDbContext, CancellationToken, Task<T>> action, CancellationToken ct, MutationPurpose purpose = MutationPurpose.Normal)
            => await Gate.ExecuteAsync(async (a, token) =>
            {
                await using var context = Context();
                return await new PersistenceUnitOfWork(context, Owner, Storage).ExecuteAsync(a, purpose, action, token);
            }, ct);
        public ValueTask DisposeAsync()
        {
            Owner.Dispose(); Gate.Dispose();
            Directory.Delete(System.IO.Path.GetDirectoryName(Path)!, recursive: true);
            return ValueTask.CompletedTask;
        }
    }
}
