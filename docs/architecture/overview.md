# Architecture

Status: **Approved recovery baseline for implementation; runtime unverified.** The user accepted the active six ADRs and Windows local sandbox; see [approval register](../process/approval-register.md). Provider choice/rights and nonlocal/production operation remain separate gates.

## Boundaries and components
One .NET 10 ASP.NET Core deployable with in-process BackgroundServices, one local SQLite database, and one active application instance. Sandbox email/fake Slack are external test receivers, not domain services. No frontend, broker, general plugin framework, CQRS/event sourcing or microservices.

```text
Authenticated users/admins -> API -> Application -> Domain
                                      | ports
RSS / market / simulated webhook -> Infrastructure
                                      | EF Core
                                SQLite + Identity
                                      | durable intents
                          Hosted dispatchers -> sandbox channels
```

- **Domain:** typed category validation/matching; no EF/HTTP/Identity dependencies.
- **Application:** owner-aware use cases, normalized event acceptance, transaction orchestration and ports.
- **Infrastructure:** EF context including Identity tables, source adapters, channel transports, secret-reference resolution.
- **Api:** DTOs/auth/policies, composition and hosted services. Workers create DI scopes; never share a DbContext instance.
- Dependencies point inward. No generic repository/mediator needed. Source definitions and per-user destination definitions are provisioned configuration, not user-editable URLs.

## Consistency and ownership
SQLite is the system of record. A **singleton nonreentrant bounded async write gate covers every mutation**, including Identity failed counts/reset/rehash/provisioning, rule edits, source checkpoints, event acceptance and dispatch state. The outer Application use case acquires once and passes an internal admission context to scoped participants; participants never reacquire or capture it. Normalize, hash/verify and network IO outside admission/transaction. Under admission, reload relevant Identity hash/stamps/lockout before sign-in; changed credentials require bounded re-verification outside admission. A fresh short transaction deduplicates the event, reads active rules, evaluates bounded candidates, inserts event and all delivery intents and commits. No match table exists: Event + Rule + Owner + Destination link directly to NotificationIntent with immutable matching snapshots.

Optimistic Version/ETag checks prevent stale rule edits; commit ordering determines which successful rule version event ingestion sees. A rule change affects only subsequent event transactions. Existing intents retain immutable payload, rule-version and destination-version snapshots after pause/update/soft delete. Source/event and event/rule/channel unique keys prevent duplicate logical work. External retries can still duplicate sends.

Store UTC epoch-millisecond integers and canonical invariant decimal text. Query candidate rules by category/owner/status then match exact decimal values in bounded memory; never numerically sort text in SQL. [Schema](../data/data-model-and-evolution.md) specifies fields and constraints.

## Integration and semantics
See [processing flows](processing-flows.md). Candidates are NASA RSS and Binance BTCUSDT, **conditional on coverage/currency, rights and regional-access approval**. Do not substitute BBC/Coinbase automatically. HTTP 200 is not licensing or integration proof. [Research](../research/source-feasibility.md) records evidence and limitations.

## Security boundaries
Seeded Identity user/admin accounts; explicit framework-backed opaque-bearer login only, not blanket `MapIdentityApi`. Access lifetime15 minutes (`expiresIn:900`); no refresh route; framework-issued refresh field is explicitly unused/discard-only. No instantaneous revocation claim. Persist keys separately from database with current-user Windows DPAPI under the sandbox service identity. Ownership applies even to admins; admin inspection is a distinct policy.

Controlled earthquake sender uses HMAC-SHA256 over timestamp plus exact raw body bytes with freshness checking; separate from user authentication. Fixed outbound hosts/destinations, no redirects or linked-article fetches; safe XML and bounded decompression. TLS outside isolated local development. See [security](security.md) and [API](../api/backend-api-design.md).

## Approved operating envelope
These are unmeasured safety defaults, not production capacity commitments.

| Bound | Value / behavior |
|---|---|
| Processes | One Windows instance on local NTFS; lifetime exclusive canonical-path sidecar handle before every writer including migration/provisioning; see security for fail-closed path/identity checks. |
| Rules | 100 active total, 20 per owner, at most 2 channels each; maximum 200 new intents/event. |
| RSS | 100 items, 2 MiB compressed/decompressed response; title500, summary4000, combined16 KiB UTF-8 normalized text. |
| Other ingress | 64 KiB market/webhook/manual body; pagination maximum 100 rows. |
| Polling | NASA hourly, market 60 seconds; one in-flight poll/source. |
| Network timeout | 10 seconds/attempt, including delivery. |
| Write admission | At most32 waiters, 2-second admission deadline; SQLite busy timeout1s; retryable failure when saturated. |
| Outstanding intents | Projected Pending + InFlight + RetryScheduled <=20,000; reject/defer event transaction atomically otherwise. |
| Retained intents | Projected total across all statuses <=250,000; pause ingestion for operator review rather than silently purge history. |
| Delivery | One dispatcher/channel,10s I/O timeout,2m claim lease,5 attempts total; retry after ordinal n uses30s*2^(n-1),0..10% nonnegative jitter, local cap30m; honor larger valid Retry-After without24h cutoff. |
| Storage admission | Normal mutation: >=1GiB free after16MiB conservative growth allowance. Emergency bounded completion/recovery and essential Identity security bookkeeping only: >=256MiB after1MiB allowance. |

Rule caps count active (enabled, undeleted) rules only:100 global/20 owner; paused rules consume no active quota. Enforce creation/activation under the gate; never select an arbitrary subset of matching rules. Capacity rejection creates neither event nor partial intents. Backlog pressure alone leaves delivery and rule administration available, subject to disk admission. Measure every mutation's growth, including zero-match events, seen keys, rules and failure history. No send without completion headroom. Below emergency/unmeasurable storage or DB write/checkpoint failure stops claims/sends/mutations and fails readiness503; no login credential or acceptance success without required commit. Only bounded nonpersistent redacted diagnostics may report storage failure, preventing recursive failure-history growth. These unmeasured budgets are not OS reservations; BE-003 must test WAL/checkpoint/headroom behavior. No automatic deletion.

## Reliability, observability and deployment
Local process/disk are explicit single points of failure. Five states are Pending, InFlight, Delivered, RetryScheduled and Failed; uncertain attempts use OutcomeUnknown. Graceful shutdown stops intake and allows bounded sends; recovery waits prior unexpired leases and cancels/awaits any local live operation before reclaiming. No exactly-once promise. Source outage does not block already-accepted dispatch.

Structured logs correlate request/source/slot/event/rule/intent/attempt IDs without full payloads/credentials. Record source fetch success separately from valid observation freshness. Built-in logging/Meter/Activity suffice; collect queue depth/oldest age, failures, matching duration, gate wait and disk growth. Liveness reports process; readiness checks persistence/configuration, not upstream availability.

Review migrations explicitly with writers stopped; no automatic shared-environment startup migration. Consistent SQLite backup must account for WAL; back up protected keys separately. Restore testing is required. Roll back binaries only across compatible schemas; destructive/down-migration recovery needs explicit approval.

## Dependency candidates and approval
Approved families: Microsoft EF Core10 SQLite/Design, Identity EF stores, ASP.NET Core OpenAPI/Mvc.Testing, xUnit v3/test SDK/runner, MailKit, AngleSharp HTML-to-plain-text parsing and Mailpit local capture. Exact maintained compatible stable pins are delegated after license/advisory checks, introduced as needed; material incompatibilities or new families require approval. Use BCL HTTP/JSON/XML/crypto and framework hosting/rate limiting/Data Protection. Fake Slack is a small test receiver; Windows sandbox has no Docker requirement. No unselected HTML-parser blocker remains. Do not add Npgsql, Polly, MediatR, provider SDKs, Swagger UI or telemetry backend by default.

## Evolution and trade-offs
Serialized writes and fan-out limit scale but avoid asynchronous rule-history complexity. Postgres requires provider-specific migrations plus validated data transfer, not a connection-string flip. It does not remove the need to redesign interprocess ordering/claims before multiple replicas. Revisit on sustained contention/backlog, increased symbols/rules, independent worker availability or production requirements. [ADRs](../adr/README.md) preserve alternatives and gates.
