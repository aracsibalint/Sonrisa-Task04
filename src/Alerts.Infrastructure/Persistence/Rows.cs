namespace Alerts.Infrastructure.Persistence;

// Explicit storage records: domain values are validated at this boundary, not EF-owned domain objects.
public sealed class ChannelDestinationRow
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Channel { get; set; } = "";
    public long ConfigurationVersion { get; set; }
    public string DestinationIdentity { get; set; } = "";
    public string? Address { get; set; }
    public string? SecretReference { get; set; }
    public bool IsAvailable { get; set; }
    public long? RevokedAtUtcMs { get; set; }
    public long CreatedAtUtcMs { get; set; }
}

public sealed class SourceStateRow
{
    public string Id { get; set; } = "";
    public string LogicalName { get; set; } = "";
    public long ConfigurationVersion { get; set; }
    public string Kind { get; set; } = "";
    public string Category { get; set; } = "";
    public string Provenance { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ConfigurationReference { get; set; } = "";
    public long IdentityVersion { get; set; }
    public long? ScheduleVersion { get; set; }
    public long? PollIntervalMs { get; set; }
    public bool Enabled { get; set; }
    public long? ActivatedAtUtcMs { get; set; }
    public string Health { get; set; } = "";
    public long? BaselineCompletedAtUtcMs { get; set; }
    public string? ETag { get; set; }
    public string? LastModified { get; set; }
    public long? LastAttemptAtUtcMs { get; set; }
    public long? LastFetchSucceededAtUtcMs { get; set; }
    public long? LastValidObservationAtUtcMs { get; set; }
    public long? NextPollAtUtcMs { get; set; }
    public long ConsecutiveFailures { get; set; }
    public long ConsecutiveSchemaFailures { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorSummary { get; set; }
    public long LastPollRejectedItems { get; set; }
    public long LastPollConflictingItems { get; set; }
    public long Version { get; set; }
    public long CreatedAtUtcMs { get; set; }
    public long UpdatedAtUtcMs { get; set; }
}

public sealed class SeenSourceItemRow
{
    public string SourceId { get; set; } = "";
    public string ExternalKey { get; set; } = "";
    public string Disposition { get; set; } = "";
    public long FirstSeenAtUtcMs { get; set; }
    public long? PublishedAtUtcMs { get; set; }
    public string? EventId { get; set; }
    public byte[]? PayloadFingerprint { get; set; }
}

public sealed class SourceFailureRow
{
    public string Id { get; set; } = "";
    public string SourceId { get; set; } = "";
    public long OccurredAtUtcMs { get; set; }
    public string Stage { get; set; } = "";
    public string Code { get; set; } = "";
    public string Summary { get; set; } = "";
    public long? HttpStatus { get; set; }
    public long RejectedItemCount { get; set; }
}

public sealed class EventRow
{
    public string Id { get; set; } = "";
    public string SourceId { get; set; } = "";
    public string ExternalKey { get; set; } = "";
    public string Category { get; set; } = "";
    public string Provenance { get; set; } = "";
    public string? SubmittedByUserId { get; set; }
    public long SchemaVersion { get; set; }
    public long FingerprintVersion { get; set; }
    public byte[] PayloadFingerprint { get; set; } = [];
    public long OccurredAtUtcMs { get; set; }
    public long ReceivedAtUtcMs { get; set; }
    public long AcceptedAtUtcMs { get; set; }
    public long PredicateVersion { get; set; }
    public long EvaluatedRuleCount { get; set; }
    public long MatchedRuleCount { get; set; }
    public long IntentCount { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? ArticleUrl { get; set; }
    public bool? SummaryTruncated { get; set; }
    public string? Instrument { get; set; }
    public string? QuoteCurrency { get; set; }
    public string? ComparisonKind { get; set; }
    public long? WindowStartUtcMs { get; set; }
    public long? WindowEndUtcMs { get; set; }
    public long? ScheduledSlotUtcMs { get; set; }
    public string? OpenPriceText { get; set; }
    public string? LastPriceText { get; set; }
    public string? SignedMovementPercentText { get; set; }
    public string? ProviderMovementPercentText { get; set; }
    public string? MagnitudeText { get; set; }
    public string? MagnitudeScale { get; set; }
    public string? Place { get; set; }
    public bool? Simulated { get; set; }
}

public sealed class RuleRow
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string? Keyword { get; set; }
    public string? Instrument { get; set; }
    public string? ComparisonKind { get; set; }
    public string? ThresholdPercentText { get; set; }
    public string? MinimumMagnitudeText { get; set; }
    public string? MagnitudeScale { get; set; }
    public bool EmailSelected { get; set; }
    public bool SlackSelected { get; set; }
    public bool Enabled { get; set; }
    public long Version { get; set; }
    public long CreatedAtUtcMs { get; set; }
    public long UpdatedAtUtcMs { get; set; }
    public long? DeletedAtUtcMs { get; set; }
}

public sealed class NotificationIntentRow
{
    public string Id { get; set; } = "";
    public string EventId { get; set; } = "";
    public string RuleId { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public long RuleVersion { get; set; }
    public string RuleNameSnapshot { get; set; } = "";
    public string Category { get; set; } = "";
    public string FilterSnapshotJson { get; set; } = "";
    public long PredicateVersion { get; set; }
    public long MatchedAtUtcMs { get; set; }
    public string Channel { get; set; } = "";
    public string DestinationId { get; set; } = "";
    public long DestinationConfigurationVersion { get; set; }
    public string DestinationIdentitySnapshot { get; set; } = "";
    public string? RecipientAddressSnapshot { get; set; }
    public string? SecretReferenceSnapshot { get; set; }
    public string? SubjectSnapshot { get; set; }
    public string BodySnapshot { get; set; } = "";
    public long PayloadSchemaVersion { get; set; }
    public long CreatedAtUtcMs { get; set; }
    public string Status { get; set; } = "";
    public long AttemptCount { get; set; }
    public long? NextAttemptAtUtcMs { get; set; }
    public string? ClaimToken { get; set; }
    public long? LeaseExpiresAtUtcMs { get; set; }
    public long? DeliveredAtUtcMs { get; set; }
    public long? FailedAtUtcMs { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorSummary { get; set; }
    public long Version { get; set; }
}

public sealed class DeliveryAttemptRow
{
    public string Id { get; set; } = "";
    public string IntentId { get; set; } = "";
    public long Ordinal { get; set; }
    public string ClaimToken { get; set; } = "";
    public long StartedAtUtcMs { get; set; }
    public long LeaseExpiresAtUtcMs { get; set; }
    public long? FinishedAtUtcMs { get; set; }
    public string Outcome { get; set; } = "";
    public string? ErrorCode { get; set; }
    public string? ErrorSummary { get; set; }
    public long? HttpStatus { get; set; }
    public long? SmtpStatus { get; set; }
    public long? RetryAfterMs { get; set; }
}
