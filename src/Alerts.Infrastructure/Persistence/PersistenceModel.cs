using System.Globalization;
using Alerts.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Alerts.Infrastructure.Persistence;

internal static class PersistenceModel
{
    internal static readonly HashSet<string> MutableIntentFields = ["Status", "AttemptCount", "NextAttemptAtUtcMs", "ClaimToken", "LeaseExpiresAtUtcMs", "DeliveredAtUtcMs", "FailedAtUtcMs", "LastErrorCode", "LastErrorSummary", "Version"];

    internal static void Configure(ModelBuilder b)
    {
        // .NET 10 exposes passkey DbSets; the approved login-only schema has no passkeys.
        b.Ignore<IdentityUserPasskey<string>>();
        b.Ignore<IdentityPasskeyData>();
        b.Entity<ChannelDestinationRow>().ToTable("ChannelDestination");
        b.Entity<SourceStateRow>().ToTable("SourceState");
        b.Entity<SeenSourceItemRow>().ToTable("SeenSourceItem").HasKey(x => new { x.SourceId, x.ExternalKey });
        b.Entity<SourceFailureRow>().ToTable("SourceFailure");
        b.Entity<EventRow>().ToTable("Event");
        b.Entity<RuleRow>().ToTable("Rule");
        b.Entity<NotificationIntentRow>().ToTable("NotificationIntent");
        b.Entity<DeliveryAttemptRow>().ToTable("DeliveryAttempt");

        var destination = b.Entity<ChannelDestinationRow>();
        destination.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.OwnerId);
        destination.HasAlternateKey(x => new { x.Id, x.OwnerId, x.Channel });
        destination.HasIndex(x => new { x.OwnerId, x.Channel, x.ConfigurationVersion }).IsUnique();
        destination.HasIndex(x => new { x.OwnerId, x.Channel }).IsUnique().HasFilter("IsAvailable=1");
        Check(b, typeof(ChannelDestinationRow), "Channel", "Channel IN ('Email','Slack')");
        Check(b, typeof(ChannelDestinationRow), "Address", "(Channel='Email' AND Address IS NOT NULL AND SecretReference IS NULL) OR (Channel='Slack' AND SecretReference IS NOT NULL AND Address IS NULL)");
        Check(b, typeof(ChannelDestinationRow), "Availability", "IsAvailable=0 OR RevokedAtUtcMs IS NULL");

        var source = b.Entity<SourceStateRow>();
        source.HasIndex(x => new { x.LogicalName, x.ConfigurationVersion }).IsUnique();
        source.HasAlternateKey(x => new { x.Id, x.Category, x.Provenance });
        source.HasIndex(x => new { x.Enabled, x.NextPollAtUtcMs });
        Check(b, typeof(SourceStateRow), "Kind", "(Kind='Rss' AND Category='News' AND Provenance='Live') OR (Kind='Market' AND Category='Market' AND Provenance='Live') OR (Kind='EarthquakeWebhook' AND Category='Earthquake' AND Provenance='Simulated') OR (Kind='Manual' AND Category IN ('News','Market','Earthquake') AND Provenance='Manual')");
        Check(b, typeof(SourceStateRow), "Health", "Health IN ('Uninitialized','Healthy','Degraded','Blocked','Disabled')");
        Check(b, typeof(SourceStateRow), "Baseline", "Kind='Rss' OR (BaselineCompletedAtUtcMs IS NULL AND ETag IS NULL AND LastModified IS NULL)");
        Check(b, typeof(SourceStateRow), "Schedule", "(Kind IN ('Rss','Market') AND ScheduleVersion IS NOT NULL AND PollIntervalMs IS NOT NULL AND PollIntervalMs>0) OR (Kind NOT IN ('Rss','Market') AND ScheduleVersion IS NULL AND PollIntervalMs IS NULL AND NextPollAtUtcMs IS NULL)");
        Check(b, typeof(SourceStateRow), "Activation", "Enabled=0 OR ActivatedAtUtcMs IS NOT NULL");

        var ev = b.Entity<EventRow>();
        ev.HasIndex(x => new { x.SourceId, x.ExternalKey }).IsUnique();
        ev.HasAlternateKey(x => new { x.Id, x.SourceId, x.ExternalKey });
        ev.HasOne<SourceStateRow>().WithMany().HasForeignKey(x => new { x.SourceId, x.Category, x.Provenance }).HasPrincipalKey(x => new { x.Id, x.Category, x.Provenance });
        ev.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.SubmittedByUserId);
        ev.HasIndex(x => new { x.AcceptedAtUtcMs, x.Id }).IsDescending();
        ev.HasIndex(x => new { x.SourceId, x.AcceptedAtUtcMs, x.Id }).IsDescending(false, true, true);
        ev.HasIndex(x => new { x.Category, x.AcceptedAtUtcMs, x.Id }).IsDescending(false, true, true);
        Check(b, typeof(EventRow), "Manual", "(Provenance='Manual' AND SubmittedByUserId IS NOT NULL) OR (Provenance IN ('Live','Simulated') AND SubmittedByUserId IS NULL)");
        Check(b, typeof(EventRow), "Counts", "EvaluatedRuleCount BETWEEN 0 AND 100 AND MatchedRuleCount BETWEEN 0 AND EvaluatedRuleCount AND IntentCount BETWEEN MatchedRuleCount AND 2*MatchedRuleCount AND IntentCount<=200");
        const string newsNull = "Title IS NULL AND Summary IS NULL AND ArticleUrl IS NULL AND SummaryTruncated IS NULL";
        const string marketNull = "Instrument IS NULL AND QuoteCurrency IS NULL AND ComparisonKind IS NULL AND WindowStartUtcMs IS NULL AND WindowEndUtcMs IS NULL AND ScheduledSlotUtcMs IS NULL AND OpenPriceText IS NULL AND LastPriceText IS NULL AND SignedMovementPercentText IS NULL AND ProviderMovementPercentText IS NULL";
        const string quakeNull = "MagnitudeText IS NULL AND MagnitudeScale IS NULL AND Place IS NULL AND Simulated IS NULL";
        Check(b, typeof(EventRow), "CategoryFields", $"(Category='News' AND Title IS NOT NULL AND SummaryTruncated IS NOT NULL AND {marketNull} AND {quakeNull}) OR (Category='Market' AND {newsNull} AND {quakeNull} AND Instrument IS NOT NULL AND Instrument='BTCUSDT' AND QuoteCurrency IS NOT NULL AND QuoteCurrency='USDT' AND ComparisonKind IS NOT NULL AND ComparisonKind='binance-rolling-24h' AND WindowStartUtcMs IS NOT NULL AND WindowEndUtcMs IS NOT NULL AND WindowEndUtcMs-WindowStartUtcMs=86400000 AND OccurredAtUtcMs=WindowEndUtcMs AND OpenPriceText IS NOT NULL AND LastPriceText IS NOT NULL AND SignedMovementPercentText IS NOT NULL AND ((Provenance='Live' AND ScheduledSlotUtcMs IS NOT NULL AND ScheduledSlotUtcMs%60000=0) OR (Provenance='Manual' AND ScheduledSlotUtcMs IS NULL))) OR (Category='Earthquake' AND {newsNull} AND {marketNull} AND MagnitudeText IS NOT NULL AND MagnitudeScale IS NOT NULL AND MagnitudeScale='Mw' AND Simulated IS NOT NULL AND Simulated=1)");

        var seen = b.Entity<SeenSourceItemRow>();
        seen.HasOne<SourceStateRow>().WithMany().HasForeignKey(x => x.SourceId);
        seen.HasOne<EventRow>().WithMany().HasForeignKey(x => new { x.EventId, x.SourceId, x.ExternalKey }).HasPrincipalKey(x => new { x.Id, x.SourceId, x.ExternalKey });
        seen.HasIndex(x => x.EventId).IsUnique().HasFilter("EventId IS NOT NULL");
        seen.HasIndex(x => new { x.SourceId, x.FirstSeenAtUtcMs });
        Check(b, typeof(SeenSourceItemRow), "Disposition", "(Disposition='Accepted' AND EventId IS NOT NULL AND PayloadFingerprint IS NOT NULL) OR (Disposition IN ('BaselineSuppressed','OldSuppressed') AND EventId IS NULL)");

        b.Entity<SourceFailureRow>().HasOne<SourceStateRow>().WithMany().HasForeignKey(x => x.SourceId);
        b.Entity<SourceFailureRow>().HasIndex(x => new { x.SourceId, x.OccurredAtUtcMs, x.Id }).IsDescending(false, true, true);
        Check(b, typeof(SourceFailureRow), "Stage", "Stage IN ('Fetch','Parse','Normalize','Accept','Schedule')");

        var rule = b.Entity<RuleRow>();
        rule.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.OwnerId);
        rule.HasAlternateKey(x => new { x.Id, x.OwnerId });
        rule.HasIndex(x => new { x.OwnerId, x.DeletedAtUtcMs, x.CreatedAtUtcMs, x.Id }).IsDescending(false, false, true, true);
        rule.HasIndex(x => new { x.Category, x.Enabled, x.DeletedAtUtcMs });
        Check(b, typeof(RuleRow), "Selection", "(EmailSelected=1 OR SlackSelected=1) AND (DeletedAtUtcMs IS NULL OR Enabled=0)");
        Check(b, typeof(RuleRow), "Filter", "(Category='News' AND Keyword IS NOT NULL AND Instrument IS NULL AND ComparisonKind IS NULL AND ThresholdPercentText IS NULL AND MinimumMagnitudeText IS NULL AND MagnitudeScale IS NULL) OR (Category='Market' AND Keyword IS NULL AND Instrument IS NOT NULL AND Instrument='BTCUSDT' AND ComparisonKind IS NOT NULL AND ComparisonKind='binance-rolling-24h' AND ThresholdPercentText IS NOT NULL AND MinimumMagnitudeText IS NULL AND MagnitudeScale IS NULL) OR (Category='Earthquake' AND Keyword IS NULL AND Instrument IS NULL AND ComparisonKind IS NULL AND ThresholdPercentText IS NULL AND MinimumMagnitudeText IS NOT NULL AND MagnitudeScale IS NOT NULL AND MagnitudeScale='Mw')");

        var intent = b.Entity<NotificationIntentRow>();
        intent.HasOne<EventRow>().WithMany().HasForeignKey(x => x.EventId);
        intent.HasOne<RuleRow>().WithMany().HasForeignKey(x => new { x.RuleId, x.OwnerId }).HasPrincipalKey(x => new { x.Id, x.OwnerId });
        intent.HasOne<IdentityUser>().WithMany().HasForeignKey(x => x.OwnerId);
        intent.HasOne<ChannelDestinationRow>().WithMany().HasForeignKey(x => new { x.DestinationId, x.OwnerId, x.Channel }).HasPrincipalKey(x => new { x.Id, x.OwnerId, x.Channel });
        intent.HasIndex(x => new { x.EventId, x.RuleId, x.Channel }).IsUnique();
        intent.HasIndex(x => new { x.Status, x.NextAttemptAtUtcMs, x.CreatedAtUtcMs, x.Id });
        intent.HasIndex(x => new { x.Status, x.LeaseExpiresAtUtcMs });
        intent.HasIndex(x => new { x.CreatedAtUtcMs, x.Id }).IsDescending();
        intent.HasIndex(x => new { x.EventId, x.Id });
        intent.HasIndex(x => new { x.OwnerId, x.CreatedAtUtcMs, x.Id }).IsDescending(false, true, true);
        Check(b, typeof(NotificationIntentRow), "Category", "Category IN ('News','Market','Earthquake')");
        Check(b, typeof(NotificationIntentRow), "Json", "CASE WHEN json_valid(FilterSnapshotJson) THEN json_type(FilterSnapshotJson)='object' ELSE 0 END");
        Check(b, typeof(NotificationIntentRow), "Channel", "(Channel='Email' AND RecipientAddressSnapshot IS NOT NULL AND SubjectSnapshot IS NOT NULL AND SecretReferenceSnapshot IS NULL) OR (Channel='Slack' AND SecretReferenceSnapshot IS NOT NULL AND RecipientAddressSnapshot IS NULL AND SubjectSnapshot IS NULL)");
        Check(b, typeof(NotificationIntentRow), "State", "(Status='Pending' AND AttemptCount=0 AND NextAttemptAtUtcMs IS NOT NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND DeliveredAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='InFlight' AND AttemptCount BETWEEN 1 AND 5 AND NextAttemptAtUtcMs IS NULL AND ClaimToken IS NOT NULL AND LeaseExpiresAtUtcMs IS NOT NULL AND DeliveredAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='Delivered' AND AttemptCount BETWEEN 1 AND 5 AND DeliveredAtUtcMs IS NOT NULL AND NextAttemptAtUtcMs IS NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='RetryScheduled' AND AttemptCount BETWEEN 1 AND 4 AND NextAttemptAtUtcMs IS NOT NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND DeliveredAtUtcMs IS NULL AND FailedAtUtcMs IS NULL) OR (Status='Failed' AND FailedAtUtcMs IS NOT NULL AND LastErrorCode IS NOT NULL AND NextAttemptAtUtcMs IS NULL AND ClaimToken IS NULL AND LeaseExpiresAtUtcMs IS NULL AND DeliveredAtUtcMs IS NULL AND (AttemptCount BETWEEN 1 AND 5 OR (AttemptCount=0 AND LastErrorCode='PermanentProvisioningFailure')))");

        var attempt = b.Entity<DeliveryAttemptRow>();
        attempt.HasOne<NotificationIntentRow>().WithMany().HasForeignKey(x => x.IntentId);
        attempt.HasIndex(x => new { x.IntentId, x.Ordinal }).IsUnique();
        attempt.HasIndex(x => x.ClaimToken).IsUnique();
        attempt.HasIndex(x => x.IntentId).IsUnique().HasFilter("Outcome='InFlight'");
        Check(b, typeof(DeliveryAttemptRow), "State", "Ordinal BETWEEN 1 AND 5 AND LeaseExpiresAtUtcMs>StartedAtUtcMs AND ((Outcome='InFlight' AND FinishedAtUtcMs IS NULL) OR (Outcome IN ('Succeeded','TransientFailure','PermanentFailure','OutcomeUnknown') AND FinishedAtUtcMs IS NOT NULL AND FinishedAtUtcMs>=StartedAtUtcMs))");
        Check(b, typeof(DeliveryAttemptRow), "Retry", "RetryAfterMs IS NULL OR RetryAfterMs>=0");

        // Identity uses the same explicit storage contract, including restrictive relationships.
        b.Entity<IdentityUser>().HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("EmailIndex");
        b.Entity<IdentityUser>().Property(x => x.LockoutEnd).HasConversion(new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? MillisecondUtc.From(v.Value).UnixMilliseconds : null,
            v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null));
        foreach (var name in new[] { "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "PasswordHash", "SecurityStamp", "ConcurrencyStamp" })
            b.Entity<IdentityUser>().Property<string>(name).IsRequired();
        foreach (var name in new[] { "Name", "NormalizedName", "ConcurrencyStamp" })
            b.Entity<IdentityRole>().Property<string>(name).IsRequired();
        b.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "00000000-0000-0000-0000-000000000001", Name = "User", NormalizedName = "USER", ConcurrencyStamp = "role-user-v1" },
            new IdentityRole { Id = "00000000-0000-0000-0000-000000000002", Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "role-admin-v1" });
        Check(b, typeof(IdentityUser), "FailedCount", "AccessFailedCount>=0");

        foreach (var entity in b.Model.GetEntityTypes().ToArray())
        {
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.Restrict;
            foreach (var p in entity.GetProperties().ToArray()) ConfigureProperty(b, entity, p);
        }
    }

    private static void ConfigureProperty(ModelBuilder b, IMutableEntityType entity, IMutableProperty p)
    {
        var eb = b.Entity(entity.ClrType);
        var name = p.Name;
        var q = $"\"{name}\"";
        if (p.ClrType == typeof(string))
        {
            var length = Length(entity.ClrType, name);
            eb.Property(name).HasMaxLength(length).UseCollation("BINARY");
            Check(b, entity.ClrType, name + "Length", $"{q} IS NULL OR length({q}) BETWEEN 1 AND {length}");
            if (name.EndsWith("Id", StringComparison.Ordinal) || name == "Id" || name == "ClaimToken")
                Check(b, entity.ClrType, name + "Guid", $"{q} IS NULL OR (length({q})=36 AND substr({q},9,1)='-' AND substr({q},14,1)='-' AND substr({q},19,1)='-' AND substr({q},24,1)='-' AND length(replace({q},'-',''))=32 AND replace({q},'-','') NOT GLOB '*[^0-9a-f]*')");
            if (name.EndsWith("Text", StringComparison.Ordinal))
            {
                var kind = name is "OpenPriceText" or "LastPriceText" ? "Price" : name == "ThresholdPercentText" ? "PercentThreshold" : name is "MagnitudeText" or "MinimumMagnitudeText" ? "Magnitude" : "Derived";
                eb.Property<string>(name).HasConversion(new ValueConverter<string, string>(v => DecimalText(v, kind), v => DecimalText(v, kind)));
            }
        }
        if (name.EndsWith("UtcMs", StringComparison.Ordinal) || name == "LockoutEnd")
            Check(b, entity.ClrType, name + "Range", $"{q} IS NULL OR {q} BETWEEN -62135596800000 AND 253402300799999");
        if (p.ClrType == typeof(bool) || p.ClrType == typeof(bool?))
            Check(b, entity.ClrType, name + "Bool", $"{q} IS NULL OR {q} IN (0,1)");
        if (name.EndsWith("Version", StringComparison.Ordinal))
        {
            Check(b, entity.ClrType, name + "Positive", $"{q} IS NULL OR {q}>=1");
            if (name == "Version") eb.Property(name).IsConcurrencyToken();
        }
        if (name is "ConsecutiveFailures" or "ConsecutiveSchemaFailures" or "LastPollRejectedItems" or "LastPollConflictingItems" or "RejectedItemCount")
            Check(b, entity.ClrType, name + "Counter", $"{q}>=0");
        if (name is "HttpStatus" or "SmtpStatus") Check(b, entity.ClrType, name + "Range", $"{q} IS NULL OR {q} BETWEEN 100 AND 599");
        if (p.ClrType == typeof(byte[])) Check(b, entity.ClrType, name + "Hash", $"{q} IS NULL OR length({q})=32");
    }

    private static int Length(Type type, string name) => name switch
    {
        "Id" or "OwnerId" or "UserId" or "RoleId" or "SourceId" or "EventId" or "RuleId" or "DestinationId" or "IntentId" or "SubmittedByUserId" or "ClaimToken" => 36,
        "ExternalKey" => 12288,
        "LogicalName" or "DisplayName" or "RuleNameSnapshot" or "Keyword" => 100,
        "Name" => type == typeof(RuleRow) ? 100 : type == typeof(IdentityUserToken<string>) ? 128 : 256,
        "Channel" => 5, "Category" => 10, "Provenance" => 12, "Kind" or "Disposition" or "Outcome" or "Instrument" => 20,
        "Health" or "Stage" => 16, "Status" => 14, "MagnitudeScale" => 4, "QuoteCurrency" => 10, "ComparisonKind" => 32,
        "ConfigurationReference" or "DestinationIdentity" or "DestinationIdentitySnapshot" or "LastModified" or "LoginProvider" or "ProviderKey" => 128,
        "Address" or "RecipientAddressSnapshot" => 320,
        "SecretReference" or "SecretReferenceSnapshot" => 256,
        "PhoneNumber" or "Code" or "ErrorCode" or "LastErrorCode" => 64,
        "Title" or "ErrorSummary" or "LastErrorSummary" => 500,
        "Summary" => type == typeof(EventRow) ? 4000 : 500,
        "ETag" => 1024,
        "ArticleUrl" or "PasswordHash" or "ClaimValue" or "FilterSnapshotJson" => 2048,
        "BodySnapshot" => 16000, "SubjectSnapshot" or "Place" => 200, "Value" => 4096,
        _ when name.EndsWith("Text", StringComparison.Ordinal) => 64,
        _ => 256
    };

    private static void Check(ModelBuilder b, Type type, string suffix, string sql)
        => b.Entity(type).ToTable(t => t.HasCheckConstraint($"CK_{b.Model.FindEntityType(type)!.GetTableName()}_{suffix}", sql));

    private static string DecimalText(string text, string kind)
    {
        if (kind != "Derived")
        {
            if (CanonicalDecimal.Parse(text, Enum.Parse<DecimalKind>(kind)).Text != text)
                throw new InvalidOperationException("Noncanonical decimal persistence value.");
            return text;
        }
        if (text.Length is < 1 or > 64 || !decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            || (value == 0 ? "0" : value.ToString("0.############################", CultureInfo.InvariantCulture)) != text)
            throw new InvalidOperationException("Invalid exact canonical derived decimal.");
        return text;
    }
}
