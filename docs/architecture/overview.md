# Proposed architecture

Status: **Proposed — ready for review, not user-approved.** Based on the architect's recovery draft. Product scope is [approved](../process/approved-scope.md); technical choices below need acceptance.

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
SQLite is the system of record. A **single bounded async write gate covers every mutation**, including Identity lockout/provisioning, rule edits, source checkpoints, event acceptance and dispatch state. Normalize/network IO outside it. Under the gate, a fresh short transaction deduplicates the event, reads active rules, evaluates bounded candidates, inserts event and all delivery intents and commits. No separate matching queue/table is required.

Commit orders rule edits versus event ingestion. A rule change affects only subsequent event transactions. Pending intents retain immutable payload, rule-version and destination-version snapshots after pause/update/soft delete. Source/event and event/rule/channel unique keys prevent duplicate logical work. External retries can still duplicate sends.

Store UTC epoch-millisecond integers and canonical invariant decimal text. Query candidate rules by category/owner/status then match exact decimal values in bounded memory; never numerically sort text in SQL. [Schema](../data/data-model-and-evolution.md) specifies fields and constraints.

## Integration and semantics
See [processing flows](processing-flows.md). Candidates are NASA RSS and Binance BTCUSDT, **conditional on coverage/currency, rights and regional-access approval**. Do not substitute BBC/Coinbase automatically. HTTP 200 is not licensing or integration proof. [Research](../research/source-feasibility.md) records evidence and limitations.

## Security boundaries
Seeded Identity user/admin accounts; explicit framework-backed opaque-bearer login only, not blanket `MapIdentityApi`. Proposed access lifetime 15 minutes; no refresh route; framework-issued refresh field is explicitly unused/discard-only. No instantaneous revocation claim. Persist/protect Data Protection keys separately from database. Ownership applies even to admins; admin inspection is a distinct policy.

Controlled earthquake sender uses HMAC-SHA256 over timestamp plus exact raw body bytes with freshness checking; separate from user authentication. Fixed outbound hosts/destinations, no redirects or linked-article fetches; safe XML and bounded decompression. TLS outside isolated local development. See [security](security.md) and [API](../api/backend-api-design.md).

## Proposed operating envelope
These are unmeasured safety defaults, not production capacity commitments.

| Bound | Proposed value / behavior |
|---|---|
| Processes | One writer application; refuse second instance for same local DB; no network filesystem. |
| Rules | 100 active total, 20 per owner, at most 2 channels each; maximum 200 new intents/event. |
| RSS | 100 items, 2 MiB decompressed response; 16 KiB normalized news text. |
| Other ingress | 64 KiB market/webhook/manual body; pagination maximum 100 rows. |
| Polling | NASA hourly, market 60 seconds; one in-flight poll/source. |
| Network timeout | 10 seconds/attempt, including delivery. |
| Write admission | At most 32 waiters, 2-second admission deadline; retryable failure when saturated. |
| Outstanding intents | Projected total must remain <=20,000; reject/defer event transaction atomically otherwise. |
| Retained intents | Projected total must remain <=250,000; pause ingestion for operator review rather than silently purge history. |
| Delivery | One dispatcher/channel, 5 attempts total; initial retry 30 seconds exponential/jitter cap 30 minutes; honor longer valid Retry-After. |

Rule caps must be enforced on creation/activation under the gate; do not select an arbitrary subset of matching rules. Capacity rejection creates neither event nor partial intents. Existing delivery and rule administration remain available. Retention/pruning policy is future work requiring explicit approval; local disk still needs monitoring and backup.

## Reliability, observability and deployment
Local process/disk are explicit single points of failure. Graceful shutdown stops intake and allows bounded sends; interrupted claims recover without exactly-once promises. Source outage does not block already-accepted dispatch.

Structured logs correlate request/source/slot/event/rule/intent/attempt IDs without full payloads/credentials. Record source fetch success separately from valid observation freshness. Built-in logging/Meter/Activity suffice; collect queue depth/oldest age, failures, matching duration, gate wait and disk growth. Liveness reports process; readiness checks persistence/configuration, not upstream availability.

Review migrations explicitly with writers stopped; no automatic shared-environment startup migration. Consistent SQLite backup must account for WAL; back up protected keys separately. Restore testing is required. Roll back binaries only across compatible schemas; destructive/down-migration recovery needs explicit approval.

## Dependency candidates and approval
Approve and pin exact compatible patch versions during BE-001: Microsoft EF Core 10 SQLite/Design, Identity EF stores, ASP.NET Core OpenAPI/Mvc.Testing, xUnit and test SDK; MailKit for SMTP; AngleSharp for HTML-to-plain-text parsing. Use BCL HTTP/JSON/XML/crypto and framework hosting/rate limiting/Data Protection. Local mail capture such as Mailpit is a separately approved pinned sandbox dependency; fake Slack is a small test receiver. Do not add Npgsql, Polly, MediatR, provider SDKs, Swagger UI or telemetry backend by default.

## Evolution and trade-offs
Serialized writes and fan-out limit scale but avoid asynchronous rule-history complexity. Postgres requires provider-specific migrations plus validated data transfer, not a connection-string flip. It does not remove the need to redesign interprocess ordering/claims before multiple replicas. Revisit on sustained contention/backlog, increased symbols/rules, independent worker availability or production requirements. [ADRs](../adr/README.md) preserve alternatives and gates.
