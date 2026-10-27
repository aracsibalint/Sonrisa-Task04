# Data model and evolution

Status: **Proposed — pending technical approval.** No schema or migration has been executed.

## Authority, review gates and representation

[Scope](../process/approved-scope.md) · [Architecture](../architecture/overview.md) · [Flows](../architecture/processing-flows.md) · [API](../api/backend-api-design.md)

FACT: .NET 10, EF Core and initial SQLite are approved scope. The following mappings, constraints and migration strategy are Level 2 proposals. Architect reconciliation is required: the received handoff specifies optimistic rule versions and `Pending/InFlight/Sent/Failed`; the parallel flows initially specify last-committed edits and `RetryScheduled/Delivered`. This specification proposes the handoff contract: retries are Pending with a future due time; Sent means receiver acceptance. Do not build contradictory variants. See the implementation plan's Open Questions.

### Column notation and shared constraints

- Every listed column is **NOT NULL unless suffixed `?`**. `TEXT(n)` means SQLite TEXT plus explicit `CHECK(length(column)<=n)`; nonempty required strings additionally have `length>=1`. Optional strings are null or nonempty. SQLite does not enforce varchar lengths automatically. Counters/versions/timestamps use INTEGER (`long` in .NET), booleans INTEGER with `CHECK IN (0,1)`, hashes BLOB(32) with `CHECK(length=32)`.
- IDs are application-generated GUIDs, lowercase canonical `D` strings, `TEXT(36)`, ordinal/BINARY collation; identity user/role IDs use the same convention. They are not sortable timestamps. All FK columns use the same mapping. All relationships below use **ON DELETE RESTRICT**, including Identity relationships (account removal is out of scope). Explicitly enable/test SQLite foreign keys on every connection. No domain cascade deletion.
- `UtcMs` columns use signed UTC Unix epoch milliseconds, `CHECK BETWEEN -62135596800000 AND 253402300799999` (DateTimeOffset representable range); scheduling uses numeric comparisons/indexes, not text dates. API timestamps are UTC ISO 8601 with `Z`, millisecond precision; reject precision beyond milliseconds rather than silently round producer identity content. Domain uses UTC DateTimeOffset; conversion at persistence boundary. Framework Identity lockout time is the exception explicitly converted below.
- All enum columns use TEXT plus the enumerated CHECK values below. Versions are INTEGER >=1, application-incremented and configured as EF concurrency tokens, never SQLite rowversion. Overflow fails safely. No REAL/double for prices, thresholds or magnitude.
- **Decimal strategy:** invariant canonical TEXT(64); reject exponent notation, whitespace, plus sign, NaN/infinity, nondecimal and overflow. Input decimal string grammar `-?(0|[1-9][0-9]*)(\.[0-9]+)?`; parse using restricted invariant decimal styles; normalize trailing fractional zeroes, trailing dot and negative zero (`0`). Validate input scale before normalization. Prices/percent thresholds allow <=8 fractional places, magnitude <=2; derived percent allows decimal's supported precision. DB CHECK enforces nonempty bounded text, not numeric range. Domain/EF converters enforce parse, canonical form, scale and numeric ranges on every write; startup/provisioning/import validation checks old rows. Do not claim SQLite text CHECKs enforce decimal numeric validity.
- Match a bounded materialized category-specific active-rule set using checked .NET decimal arithmetic. SQL filters only owner/category/enabled/deleted and identifiers; **never decimal range predicates, numeric sorting, SUM or CAST(REAL) on stored text**. With <=200 undeleted rules, deliberate in-memory decimal evaluation is the documented exception to query-side filtering. Display signed percent is `checked(100*(last-open)/open)`; authoritative predicate is `checked(100*abs(last-open)) >= checked(threshold*open)`.
- Bounds: prices `(0, 1000000000000]`; percent threshold `(0,1000000]`; magnitude/minimum `[-2,10]`; Mw only. Domain validates, DB stores canonical text. No arithmetic overflow is converted to a match/nonmatch silently.
- EF-created schema and migration-history tables must be reviewed separately; no business table contains secrets, raw upstream response bodies or complete Slack webhook URLs.

## Relationships and aggregate boundaries

`IdentityUser -> Rule -> RuleMatch <- Event <- SourceState`; `RuleMatch -> NotificationIntent -> DeliveryAttempt`; `IdentityUser -> ChannelDestination -> NotificationIntent`; `SourceState -> SeenSourceItem / SourceFailure`. One application DbContext includes Identity and these records, scoped per operation. Domain entities/value objects have no EF/Identity dependencies; Application orchestrates one unit of work. Match, event and delivery snapshot fields are immutable after commit, enforced by application mapping/update guards and tests (not claimed as database triggers).

## Identity and destination tables

Use the standard Identity EF schema with explicit mappings below; no external login or token-management routes. Unused framework tables remain for compatibility, not as permission to expose features.

| Table | Columns | Keys/indexes/checks |
|---|---|---|
| AspNetUsers | `Id TEXT(36)`, `UserName TEXT(256)`, `NormalizedUserName TEXT(256)`, `Email TEXT(256)`, `NormalizedEmail TEXT(256)`, `EmailConfirmed BOOL`, `PasswordHash TEXT(2048)`, `SecurityStamp TEXT(256)`, `ConcurrencyStamp TEXT(256)`, `PhoneNumber TEXT(64)?`, `PhoneNumberConfirmed BOOL`, `TwoFactorEnabled BOOL`, `LockoutEnd INTEGER?` (UtcMs), `LockoutEnabled BOOL`, `AccessFailedCount INTEGER` | PK Id; unique NormalizedUserName and unique NormalizedEmail (provisioned unique emails); AccessFailedCount >=0. ConcurrencyStamp is Identity's concurrency token. Map LockoutEnd DateTimeOffset with explicit epoch converter. UserName is provisioned email; no MFA/phone flow in MVP, those fields false/null. |
| AspNetRoles | `Id TEXT(36)`, `Name TEXT(256)`, `NormalizedName TEXT(256)`, `ConcurrencyStamp TEXT(256)` | PK Id; unique NormalizedName; seed exactly User/Admin; ConcurrencyStamp token. |
| AspNetUserRoles | `UserId TEXT(36)`, `RoleId TEXT(36)` | PK (UserId,RoleId), FK user/role; index RoleId. |
| AspNetUserClaims | `Id INTEGER`, `UserId TEXT(36)`, `ClaimType TEXT(256)?`, `ClaimValue TEXT(2048)?` | generated PK Id, FK user; index UserId. No secrets in claims. |
| AspNetRoleClaims | `Id INTEGER`, `RoleId TEXT(36)`, `ClaimType TEXT(256)?`, `ClaimValue TEXT(2048)?` | generated PK Id, FK role; index RoleId. |
| AspNetUserLogins | `LoginProvider TEXT(128)`, `ProviderKey TEXT(128)`, `ProviderDisplayName TEXT(256)?`, `UserId TEXT(36)` | PK (LoginProvider,ProviderKey), FK user; index UserId; unused. |
| AspNetUserTokens | `UserId TEXT(36)`, `LoginProvider TEXT(128)`, `Name TEXT(128)`, `Value TEXT(4096)?` | PK (UserId,LoginProvider,Name), FK user; unused; bearer sessions are not rows here. |
| ChannelDestination | `Id TEXT(36)`, `OwnerId TEXT(36)`, `Channel TEXT(5)`, `ConfigurationVersion INTEGER`, `DestinationIdentity TEXT(128)`, `Address TEXT(320)?`, `SecretReference TEXT(256)?`, `IsAvailable BOOL`, `RevokedAtUtcMs INTEGER?`, `CreatedAtUtcMs INTEGER` | PK Id; FK OwnerId -> users; unique (OwnerId,Channel,ConfigurationVersion); unique (Id,OwnerId,Channel); partial unique (OwnerId,Channel) WHERE IsAvailable=1; Channel IN ('Email','Slack'); version>=1. Email requires Address/nonempty, SecretReference null; Slack requires SecretReference/nonempty, Address null. Available implies RevokedAt null. |

Provisioned destinations are immutable versioned rows apart from availability/revocation; new recipient means new row/version. Email is an address snapshot, validated as one mailbox, <=320, no CR/LF; Slack DestinationIdentity is a nonsecret opaque configured target ID, **not URL/path/token**. SecretReference points to protected configuration, never contains the secret. SMTP service credential lives only in secret configuration. Credential rotation may preserve destination identity; moving a secret reference to another recipient is forbidden. Old references are retained or revoked explicitly; pending work cannot be redirected. Owner/channel availability is discovered safely via GET /me/channels.

Identity writes (failed-login count, lockout reset, rehash, stamps, provisioning) must participate in the same async mutation gate. Verify actual UserManager/SignInManager persistence paths rather than assuming login is read-only. Hash checks occur outside a transaction; recheck the security/concurrency stamp under gate before a mutation/sign-in decision. No network in the gated operation. Review the strengthened Identity nullability against actual .NET 10 generated mappings during BE-003/004.

## Source state, seen keys and failures

| Table | Columns |
|---|---|
| SourceState | `Id TEXT(36)`, `LogicalName TEXT(100)`, `ConfigurationVersion INTEGER`, `Kind TEXT(20)`, `Category TEXT(10)`, `Provenance TEXT(12)`, `DisplayName TEXT(100)`, `ConfigurationReference TEXT(128)`, `IdentityVersion INTEGER`, `ScheduleVersion INTEGER?`, `PollIntervalMs INTEGER?`, `Enabled BOOL`, `Health TEXT(16)`, `BaselineCompletedAtUtcMs INTEGER?`, `ETag TEXT(1024)?`, `LastModified TEXT(128)?`, `LastAttemptAtUtcMs INTEGER?`, `LastFetchSucceededAtUtcMs INTEGER?`, `LastValidObservationAtUtcMs INTEGER?`, `NextPollAtUtcMs INTEGER?`, `ConsecutiveFailures INTEGER`, `ConsecutiveSchemaFailures INTEGER`, `LastErrorCode TEXT(64)?`, `LastErrorSummary TEXT(500)?`, `LastPollRejectedItems INTEGER`, `LastPollConflictingItems INTEGER`, `Version INTEGER`, `CreatedAtUtcMs INTEGER`, `UpdatedAtUtcMs INTEGER` |
| SeenSourceItem | `SourceId TEXT(36)`, `ExternalKey TEXT(12288)`, `Disposition TEXT(20)`, `FirstSeenAtUtcMs INTEGER`, `PublishedAtUtcMs INTEGER?`, `EventId TEXT(36)?`, `PayloadFingerprint BLOB(32)?` |
| SourceFailure | `Id TEXT(36)`, `SourceId TEXT(36)`, `OccurredAtUtcMs INTEGER`, `Stage TEXT(16)`, `Code TEXT(64)`, `Summary TEXT(500)`, `HttpStatus INTEGER?`, `RejectedItemCount INTEGER` |

Constraints/indexes:

- SourceState PK Id; unique (LogicalName,ConfigurationVersion); unique (Id,Category,Provenance) for event FK. Kind IN ('Rss','Market','EarthquakeWebhook','Manual'); Category IN ('News','Market','Earthquake'); Provenance IN ('Live','Simulated','Manual'); Health IN ('Uninitialized','Healthy','Degraded','Blocked','Disabled'). Versions>=1; counters>=0; poll interval positive. CHECK kind/category/provenance combinations: Rss/News/Live, Market/Market/Live, EarthquakeWebhook/Earthquake/Simulated, Manual/any/Manual. Only Rss baseline/validators may be nonnull; polled kinds require schedule version and poll interval, others keep schedule/poll fields null. ConfigurationReference is nonsecret; no URL/secrets in API. Index (Enabled,NextPollAtUtcMs) for polling.
- Seen PK (SourceId,ExternalKey); FK SourceId; FK (EventId,SourceId,ExternalKey) -> Event's alternate key; unique partial EventId WHERE EventId IS NOT NULL. Disposition IN ('BaselineSuppressed','OldSuppressed','Accepted'); Accepted iff EventId nonnull; accepted fingerprint nonnull. Suppressed rows have null EventId. Used only by RSS; application enforces source kind and matching category. Index (SourceId,FirstSeenAtUtcMs). Invalid individual items are not marked seen.
- SourceFailure PK Id; FK SourceId; index (SourceId,OccurredAtUtcMs DESC,Id DESC); Stage IN ('Fetch','Parse','Normalize','Accept','Schedule'); HttpStatus null or 100..599; RejectedItemCount>=0. Save one bounded poll-level record per failure/rejected-item group, not unbounded raw response text. Summary comes from an allowlisted local error template, not upstream exception/body. No automatic retention purge.

Source health and next-poll state are durable; staleness is computed at read time from successful fetch/valid-observation clocks, not conflated with HTTP reachability. A new URL/identity policy means a new SourceState/configuration version and explicit fresh suppressed RSS baseline. Disabled prior rows remain for FK history.

### Collision-free identity and payload fingerprint

ExternalKey is an ASCII encoding: `v1|<base64url(UTF8(part1))>|...`; unpadded base64url never contains `|`. Parts are:

- RSS: identity version, type (`guid` or `link`), exact nonempty decoded GUID or normalized link. GUID <=2048 Unicode scalars /8192 UTF-8 bytes; link <=2048 scalars /8192 bytes. Reject oversize, do not truncate identity. Link normalization lowercases scheme/host, removes default port/fragment, preserves path/query; no redirects or query stripping.
- Manual: authenticated actor GUID, caller `externalId` (1..512 Unicode scalars, <=2048 UTF-8 bytes, no controls). Source is fixed manual category; two admins using the same ID do not collide. Actor is derived, never client-supplied.
- Webhook: `externalId` with same producer bound, source derived from credential. Credential-key rotation does not change source identity or reset dedup.
- Market: instrument, comparison kind, schedule version, scheduled-slot-start epoch milliseconds. Source already scopes configuration version. Decimal strings/prices and retry receipt times are not identity parts.

Validate encoded length <=12288 ASCII bytes/chars before persistence; retain complete values. SQLite BINARY collation and future PostgreSQL deterministic ordinal collation preserve case sensitivity; do not use culture-insensitive/case-folded identity indexes.

Fingerprint is SHA-256 over application-defined canonical JSON schema v1: fixed property ordering, UTF-8, normalized NFC news strings, canonical decimal strings, canonical millisecond UTC times, category-specific submitted business fields and attribution URL; excludes HTTP signature/timestamp, received/accepted time, actor (already key-scoped), local counts and derived display percent. Missing versus null optional fields canonicalize to null. Store FingerprintVersion on Event; centralize serializer/test golden bytes. Compare both fingerprint and normalized immutable field values when determining equality (a hash is not permission to overwrite). Controlled same key/different content ->409; poll same key/different content -> original unchanged plus sanitized diagnostic, no rematch.

## Events (single typed table, not arbitrary JSON)

| Group | Columns |
|---|---|
| Common | `Id TEXT(36)`, `SourceId TEXT(36)`, `ExternalKey TEXT(12288)`, `Category TEXT(10)`, `Provenance TEXT(12)`, `SubmittedByUserId TEXT(36)?`, `SchemaVersion INTEGER`, `FingerprintVersion INTEGER`, `PayloadFingerprint BLOB(32)`, `OccurredAtUtcMs INTEGER`, `ReceivedAtUtcMs INTEGER`, `AcceptedAtUtcMs INTEGER`, `PredicateVersion INTEGER`, `EvaluatedRuleCount INTEGER`, `MatchedRuleCount INTEGER`, `IntentCount INTEGER` |
| News only | `Title TEXT(500)?`, `Summary TEXT(4000)?`, `ArticleUrl TEXT(2048)?`, `SummaryTruncated BOOL?` |
| Market only | `Instrument TEXT(20)?`, `QuoteCurrency TEXT(10)?`, `ComparisonKind TEXT(32)?`, `WindowStartUtcMs INTEGER?`, `WindowEndUtcMs INTEGER?`, `ScheduledSlotUtcMs INTEGER?`, `OpenPriceText TEXT(64)?`, `LastPriceText TEXT(64)?`, `SignedMovementPercentText TEXT(64)?`, `ProviderMovementPercentText TEXT(64)?` |
| Earthquake only | `MagnitudeText TEXT(64)?`, `MagnitudeScale TEXT(4)?`, `Place TEXT(200)?`, `Simulated BOOL?` |

PK Id; unique (SourceId,ExternalKey); alternate unique (Id,SourceId,ExternalKey); FK (SourceId,Category,Provenance) -> SourceState; FK SubmittedByUserId -> user. CHECK category/provenance enums; SubmittedBy nonnull iff Manual; versions>=1; counts 0..200/0..200/0..400 with Matched<=Evaluated and Matched<=IntentCount<=2*Matched. Index (AcceptedAtUtcMs DESC,Id DESC); (SourceId,AcceptedAtUtcMs DESC,Id DESC); (Category,AcceptedAtUtcMs DESC,Id DESC). No occurrence-time index without an actual endpoint query.

Category CHECKs require the matching field group and null all other groups:

- News requires Title, SummaryTruncated; Summary/ArticleUrl optional; no Market/Earthquake fields. Published time is OccurredAt. Summary may be null (no summary), never empty. RSS requires a valid publication time; title 1..500, optional summary <=4000. ArticleUrl is output/navigation only, HTTP(S), never fetched; safe encoder required. RSS may deterministically truncate normalized summary and flag; manual input rejects oversize. Keep content:encoded out of all payloads.
- Market requires instrument `BTCUSDT`, currency `USDT`, comparison `binance-rolling-24h`, start/end, open/last/signed percent; provider percent optional and bounded parsed decimal. `WindowEnd-WindowStart=86400000`; OccurredAt=WindowEnd. ScheduledSlot required only for Live, null for Manual; must be divisible by 60000. Domain checks close time within receipt minus5m/plus1m, exact symbol/scale and slot not expired at acceptance. Manual market uses the same window/freshness validation, but identity is actor+externalId, not polling slot.
- Earthquake requires magnitude, scale `Mw`, Simulated=1; Place optional. Domain rejects occurrence >receipt+5m; past events permitted. Manual earthquakes remain clearly simulated/manual, not live seismic evidence.

No mutable processed flag or unmatched queue: every persisted Event was evaluated synchronously and atomically. No negative RuleMatch records.

## Rules and positive matches

| Table | Columns |
|---|---|
| Rule | `Id TEXT(36)`, `OwnerId TEXT(36)`, `Name TEXT(100)`, `Category TEXT(10)`, `Keyword TEXT(100)?`, `Instrument TEXT(20)?`, `ComparisonKind TEXT(32)?`, `ThresholdPercentText TEXT(64)?`, `MinimumMagnitudeText TEXT(64)?`, `MagnitudeScale TEXT(4)?`, `EmailSelected BOOL`, `SlackSelected BOOL`, `Enabled BOOL`, `Version INTEGER`, `CreatedAtUtcMs INTEGER`, `UpdatedAtUtcMs INTEGER`, `DeletedAtUtcMs INTEGER?` |
| RuleMatch | `Id TEXT(36)`, `EventId TEXT(36)`, `RuleId TEXT(36)`, `OwnerId TEXT(36)`, `RuleVersion INTEGER`, `RuleNameSnapshot TEXT(100)`, `Category TEXT(10)`, `FilterSnapshotJson TEXT(2048)`, `ChannelMask INTEGER`, `PredicateVersion INTEGER`, `MatchedAtUtcMs INTEGER` |

Rule: PK Id; FK OwnerId -> user; alternate unique (Id,OwnerId); index (OwnerId,DeletedAtUtcMs,CreatedAtUtcMs DESC,Id DESC); index (Category,Enabled,DeletedAtUtcMs). At least one selected channel. Deleted implies Enabled=0. Name/keyword trimmed NFC, nonempty. Filter checks: News requires Keyword only; Market requires Instrument=`BTCUSDT`, ComparisonKind=`binance-rolling-24h`, Threshold only; Earthquake requires MinimumMagnitude and MagnitudeScale=`Mw` only. Null other filter fields. Category immutable after create; replacing category requires new rule. Enforce <=20 undeleted per owner and <=200 global inside gate, including paused rules; soft-deleted rows do not count. The API update supplies expected version, delete uses If-Match; under gate require exact current version or fail precondition. Every successful mutation increments version once; duplicate delete is not a new mutation. Pausing/deleting leaves all matches/intents.

RuleMatch: PK Id; FK EventId; composite FK (RuleId,OwnerId) -> Rule; unique (RuleId,EventId); alternate unique (Id,RuleId,EventId,OwnerId); index (EventId,Id); RuleVersion/PredicateVersion>=1; ChannelMask IN (1,2,3), Email=1, Slack=2. Snapshot JSON is exact API filter shape plus `schemaVersion:1`, bounded/validated by application; SQLite CHECK json_valid and object type. Category agrees with both event/rule (application invariant, relational tests); all rows represent true matches. Snapshot and rule version are immutable even after rule edit/deletion. Do not use snapshot JSON for predicates.

## Durable delivery and attempts

| Table | Columns |
|---|---|
| NotificationIntent | `Id TEXT(36)`, `MatchId TEXT(36)`, `EventId TEXT(36)`, `RuleId TEXT(36)`, `OwnerId TEXT(36)`, `Channel TEXT(5)`, `DestinationId TEXT(36)`, `DestinationConfigurationVersion INTEGER`, `DestinationIdentitySnapshot TEXT(128)`, `RecipientAddressSnapshot TEXT(320)?`, `SecretReferenceSnapshot TEXT(256)?`, `SubjectSnapshot TEXT(200)?`, `BodySnapshot TEXT(16000)`, `PayloadSchemaVersion INTEGER`, `CreatedAtUtcMs INTEGER`, `Status TEXT(8)`, `AttemptCount INTEGER`, `NextAttemptAtUtcMs INTEGER?`, `ClaimToken TEXT(36)?`, `LeaseExpiresAtUtcMs INTEGER?`, `SentAtUtcMs INTEGER?`, `FailedAtUtcMs INTEGER?`, `LastErrorCode TEXT(64)?`, `LastErrorSummary TEXT(500)?`, `Version INTEGER` |
| DeliveryAttempt | `Id TEXT(36)`, `IntentId TEXT(36)`, `Ordinal INTEGER`, `ClaimToken TEXT(36)`, `StartedAtUtcMs INTEGER`, `LeaseExpiresAtUtcMs INTEGER`, `FinishedAtUtcMs INTEGER?`, `Outcome TEXT(20)`, `ErrorCode TEXT(64)?`, `ErrorSummary TEXT(500)?`, `HttpStatus INTEGER?`, `SmtpStatus INTEGER?`, `RetryAfterMs INTEGER?` |

Intent: PK Id; composite FK (MatchId,RuleId,EventId,OwnerId) -> RuleMatch; composite FK (DestinationId,OwnerId,Channel) -> ChannelDestination; unique (RuleId,EventId,Channel). Index (Status,NextAttemptAtUtcMs,CreatedAtUtcMs,Id); index (Status,LeaseExpiresAtUtcMs); index (CreatedAtUtcMs DESC,Id DESC); index (EventId,Id); index (OwnerId,CreatedAtUtcMs DESC,Id DESC). Channel Email/Slack; versions>=1; AttemptCount between0 and5. Snapshot destination version must equal referenced immutable row (application check); Email requires address+subject, no secret reference; Slack requires secret reference, null address/subject. Body is bounded plain-text payload rendered locally/escaped per adapter, never raw provider HTML. Configured Slack URL resolves outside DB by immutable destination identity. Stored message never embeds destination credentials.

Status checks (four-state handoff contract, pending architect reconciliation):

| Status | Required/null fields |
|---|---|
| Pending | NextAttempt nonnull; AttemptCount 0..4; Claim/Lease/Sent/Failed null. Retry is Pending with AttemptCount>0 and persisted future NextAttempt. |
| InFlight | AttemptCount1..5; Claim/Lease nonnull; NextAttempt/Sent/Failed null. |
| Sent | AttemptCount1..5; SentAt nonnull; NextAttempt/Claim/Lease/Failed null. |
| Failed | AttemptCount1..5; FailedAt nonnull; LastErrorCode nonnull; NextAttempt/Claim/Lease/Sent null. |

Attempt: PK Id; FK IntentId; unique (IntentId,Ordinal); unique ClaimToken; Ordinal1..5; Outcome IN ('InFlight','Succeeded','TransientFailure','PermanentFailure','Ambiguous','Abandoned'); InFlight iff Finished null; finished>=started; lease>started; HttpStatus null or100..599; SmtpStatus null or100..599; RetryAfterMs null or>=0. Index (IntentId,Ordinal) supplied by unique key. No remote body/error/secret stored; classify with safe local codes. Cross-row invariant: intent AttemptCount equals highest committed ordinal, at most one InFlight attempt per intent (partial unique IntentId WHERE Outcome='InFlight'). Application plus integration tests enforce terminal outcome compatibility.

Claim oldest due Pending by (NextAttempt,CreatedAt,Id), increment count, generate token and attempt, commit under gate; send outside gate; complete only with matching token. Claim duration2m, send timeout15s, one global send. Retry delays10s/30s/2m/10m plus nonnegative jitter0..10%; honor greater valid Retry-After up to24h, else terminal DeferredTooLong. Recover expired abandoned claims without resetting ordinal; finish attempt Ambiguous/Abandoned, then Pending with persisted retry or Failed at5. Do not recover a local send still running; cancel/await its bounded I/O first. Sent means receiver acceptance, not human receipt; Failed can follow earlier ambiguous acceptance. No exactly-once claim.

All intent insertion counts toward a serialized cap of10000 Pending+InFlight. Precompute entire fan-out (max400); over-cap/unavailable destination/overflow rolls back the event, matches, seen entry and intents. No selected-channel partial acceptance. Existing snapshots survive pause/delete and destination reprovisioning; revoked old destination fails, never reroutes.

## SQLite operations and evolution

1. **Initial migration:** review generated tables, indexes, FK restrictions, explicit CHECKs/converters and Identity overrides; test a fresh real SQLite file and foreign-key/uniqueness failures. Set a bounded busy timeout (proposed1s) shorter than external operation budgets; retain 5s application gate wait. WAL/local storage may be proposed at build but must be explicitly tested/configured, not assumed by tests.
2. **Apply deliberately:** stopped single app, reviewed known-range EF migration SQL/tool command, consistent backup and rollback plan; never EnsureCreated plus migrations, automatic destructive startup migration, or blind abandoned-lock deletion. Startup checks required schema/version and fails readiness if missing; it does not repair it.
3. **Additive fields:** add nullable/default-compatible field, deploy reader tolerant of old rows, bounded reviewed backfill with app stopped or gated maintenance, validate, then separately approve tightening. SQLite table rebuilds can be destructive even for apparently small changes: inspect SQL and require Level3 approval when destructive. Binary rollback only against compatible expanded schema; otherwise explicitly approved restore.
4. **Providers/channels:** add new explicit adapter/category mapping/versioned source row or channel mapping only after contract/rights/dependency approval. Do not reinterpret old SourceId/external keys, decimal text, filter snapshots or delivery statuses. Version new payload/snapshot parsers; retain old readers while old intents can send. New RSS source starts suppressed baseline. Channel addition needs CHECK/index/API/dispatcher changes and unavailable-channel behavior, not a plugin downloaded at runtime.
5. **Retention:** none automatic in MVP. Track file size/free space, backlog and history growth. A future policy must preserve dedup keys or explicitly change replay guarantees, handle recipient PII and FK history, and secure backups. Production retention/capacity are open approval gates.

## Dedicated PostgreSQL migration path (not implemented)

- Approve Npgsql/EF provider and target PostgreSQL version as new Level2 dependencies. Create a separate migration assembly/history for PostgreSQL; no reuse of SQLite migrations or connection-string-only switch. Keep one application process and gate initially; distributed ordering is a separate architect decision.
- Preserve IDs, actor/source-scoped keys, version values and binary ordinal uniqueness; use deterministic tested collation (e.g. C) and UTF-8. Test PostgreSQL B-tree key-size limits: external keys up to12288 can exceed index limits. Proposed future redesign stores full canonical key plus SHA-256 indexed identity digest and performs collision equality checks under the gate; approve collision-handling/uniqueness schema before cutover, do not silently truncate keys or assume wide SQLite indexes port unchanged.
- Decide explicitly whether to retain canonical decimal text first (minimum semantic change) or migrate to numeric with validated precision/scale, especially derived signed percent and existing bounds. Never round historical values silently; compare cross-product predicate fixtures before/after. UTC epoch long can remain bigint; any timestamptz move must prove millisecond/timezone/range equivalence. Identity lockout conversion/stamps and case-normalized unique emails must survive.
- Rehearse on a sanitized copy: stop writes/polling/sends, consistently export SQLite and verify backup; create PostgreSQL schema; transfer in FK order (users/roles, destinations/sources, rules/events, seen/matches/intents/attempts/failures); preserve every committed retry timestamp/ordinal/claim and snapshot; validate row counts per table/source/status, full normalized hashes, uniqueness/FKs, max versions, decimal/time boundaries, baseline/seen dedup and pending-delivery outcomes. Reconcile Identity auto-generated integer claim sequences after explicit ID transfer.
- Cutover only after rehearsal, reviewed SQL, restore drill and explicit operational approval. Change configuration while app stopped; no dual writes; resume a single host. Recover expired claims using the same ambiguity rules; expect possible duplicate delivery around shutdown. Smoke-test auth, ownership, dedup, baseline and sender snapshots before live polling.
- Rollback before new writes can restore old configuration; after new PostgreSQL writes a blind old-file restore loses data and is forbidden. Choose a reviewed reverse-transfer or accepted-loss restore only with explicit Level3 approval.
- **Database file is not deployment identity:** Data Protection key ring, key-encryption certificates/OS binding, application discriminator, webhook/SMTP/Slack secrets and configuration are separate. A copied SQLite database cannot by itself preserve bearer tokens or decrypt OS-bound keys on another host. Plan separate protected key/secret continuity or explicit token invalidation and secret reprovisioning. Never export secrets into migration logs.

## Verification and unresolved gates

Required future tests: actual SQLite CHECK/FK/uniques, canonical decimal round-trip/culture/overflow, UTC indexed due queries, max key round-trip, two-actor manual key isolation, stale rule versions, zero-match event, every-stage rollback, rule/event races, RSS partial-poll replay/baseline, slot dedup, cap rollback, claim-token fencing, restart recovery and immutable recipients. No runtime test is claimed here.

BLOCKERS for affected implementation: reconcile rule concurrency and status naming with architect; approve schema/contracts/limits/dependency pins; settle safe deterministic HTML-to-text normalization; approve providers/rights before live enabling; settle environment key protection and production retention before nonlocal operation. These do not prevent completing documentation.
