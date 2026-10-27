# Implementation Plan: Alerts MVP backend

Status: Ready for Approval

Readiness is documentation readiness only, **not technical approval or permission to implement**. All technical proposals below remain Proposed; the explicit reconciliation/dependency/provider gates in Open Questions block the affected work. No code, test execution, deployment or migration is claimed.

## Problem

Build the backend described by [approved scope](../process/approved-scope.md) and [requirements](../requirements/alerts-mvp.md): provisioned users manage owned news/market/earthquake rules; newly accepted events synchronously produce durable selected-channel notification intents; an admin submits manual events and inspects operations. Sandbox email/fake Slack suffice. This package specifies future implementation without adding a UI, registration, arbitrary fetch URLs, historical replay or delivery guarantees.

## Proposed Solution

One .NET10 deployable ASP.NET Core Api with a BackgroundService supervising independent polling and delivery loops. Four code projects: Domain, Application, Infrastructure, Api. EFCore/SQLite holds Identity, rules, normalized immutable events, RSS checkpoints/seen keys and durable notification intents/attempts. One singleton async mutation gate serializes **all** writes. A bounded acceptance transaction commits event + rule evaluation + positive matches + every selected delivery intent, with no network under gate/transaction. Send asynchronously only after commit; acknowledge durable acceptance, not delivery.

Use fixed configured sources/destinations, provisioned identities/roles, built-in opaque bearer explicit login, source-specific HMAC earthquake ingress. Conditional live proposals are NASA news RSS and Binance BTCUSDT; approval/rights/region remain blockers. Exact market threshold is absolute inclusive magnitude per poll slot, not crossing; RSS initial baseline is suppressed.

## Architecture

Canonical boundaries/alternatives: [overview](../architecture/overview.md), [processing flows](../architecture/processing-flows.md), [ADR register](../adr/README.md). Architecture authority remains architect; do not implement new topology or quietly resolve conflicts in code.

Dependency direction: Domain none; Application -> Domain; Infrastructure -> Application/Domain; Api -> Application plus Infrastructure composition. One operation-scoped DbContext, never singleton/captured by workers. No mediator, generic repository, plugin framework, broker, event sourcing or separate matcher worker. Hosting boundary creates scopes per poll acceptance/claim/completion. Async I/O with CancellationToken throughout, no blocking waits. Reads AsNoTracking/projected/paged; pure bounded decimal predicates in memory by explicit SQLite design exception.

One process and local persistent SQLite only; in-process gate is not distributed. No horizontal replicas or network-share database. Persist separate protected Data Protection key ring. Explicit pre-start migrations, startup validates schema/config, independent loop health, no destructive self-repair. PostgreSQL is a future separately approved migration, not another supported runtime now.

## Components

| Project/component | Responsibility / exact handoff |
|---|---|
| Domain — EventCandidate and three typed payloads | News (title/summary/link/publication), market (instrument/prices/exact interval), earthquake (Mw magnitude/simulated); immutable normalization/version values, checked decimal rules, validation |
| Domain — Rule and three filters | Owner identity, category-specific filter, selected channels, enabled/deleted, expected version; no EF/Identity references |
| Domain — MatchPredicates | NFC ordinal-ignore-case news substring; market checked cross-product >=; Mw inclusive >=; deterministic predicate version1 |
| Application — rule handlers | Create/Get/List/Replace/DeleteOwnedRule; enforce owner/quota/channel availability and expected-version checks under gate for mutations; no historical scan |
| Application — AcceptEvent | Trusted mapped source/actor + stable key + canonical candidate; idempotency equality check, bounded active-rule load, pure match/render, full capacity precheck, atomic event/seen/matches/intents commit |
| Application — delivery handlers | ClaimDueIntent, CompleteAttempt, RecoverExpiredClaims; token-fenced updates, attempt budget, immutable recipient snapshots, transient/permanent classification policy |
| Application ports | Narrow IEventAcceptanceStore, IRuleStore, ISourceStateStore, IDeliveryStore, IIdentitySessionService, IChannelDirectory, ISourcePoller, INotificationSender, IClock and IAsyncMutationGate; unit-of-work operation boundary rather than generic repository CRUD |
| Infrastructure — persistence | EF mappings/converters/checks/indexes/migrations, Identity stores, transaction/gate implementation, canonical keys/fingerprints, versioned config destination lookup |
| Infrastructure — input adapters | Fixed NASA RSS, Binance ticker and manual/webhook normalizers; network outside gate; source baseline/seen/validator/backoff semantics; no live enablement before approval |
| Infrastructure — output adapters | Proposed MailKit SMTP sender and typed HttpClient fake/real Slack sender; resolve immutable target secret reference at send time; bounded safe rendering/response validation |
| Api — transport/hosting | v1 DTOs, ProblemDetails/auth/ownership policy wiring, HMAC raw-byte validation, limits, DI, explicit login and BackgroundService lifecycle, health/readiness, OpenAPI surface |

Ports are proposed responsibility boundaries, not a requirement to create one generic framework per row. Concrete signatures/namespace naming are developer-level choices after contract acceptance; no component may move domain predicates into endpoints or accept caller-selected source/destination URLs.

## Data Model

Normative [data model and evolution](../data/data-model-and-evolution.md) specifies every field, nullability, CHECK, FK, unique key and query index. Initial entities: Identity tables, ChannelDestination, SourceState, SeenSourceItem, SourceFailure, Event, Rule, RuleMatch, NotificationIntent, DeliveryAttempt.

- Canonical invariant decimal TEXT with strict converter/domain checks; numeric comparisons in bounded in-memory decimal operations, never database ordering/range of numeric text or REAL conversions. UTC epoch milliseconds INTEGER for scheduling; no SQLite DateTimeOffset comparison assumptions.
- Event unique `(SourceId,ExternalKey)`; manual key includes authenticated actor, webhook key uses source+producer ID, RSS GUID/link key is versioned, market key is source/instrument/window/schedule/slot. Payload canonical equality: unchanged retry original, changed controlled input409, changed poll diagnostic only. Identity strings use ordinal comparison and collision-free encoding, not ambiguous concatenation.
- RSS baseline/seen identities persist; initial acceptable200 suppresses current valid items atomically, later new eligible entries commit one at a time. Validators advance only after complete response processing; partial failure dedups earlier committed items on replay.
- Rule soft delete, immutable category, versioned optimistic concurrency;20 undeleted/owner,200 total. Rule changes affect future accepted events only; old pending snapshots survive pause/delete.
- Event transaction includes evaluated count/version and positive RuleMatch rows, no negative matches/unprocessed event queue. Intent per matching rule/event/channel unique, bounded<=400/event, nonterminal cap10000; all-or-nothing fan-out.
- Intent snapshot includes recipient identity/config version, rule/filter/payload version and rendered body; no raw Slack URLs in DB. Four-state handoff contract Pending/InFlight/Sent/Failed; future-due Pending represents retry, token-fenced claim/attempt history survives restart. Pending architect naming reconciliation noted below.
- No automatic retention purge; schema evolution, backup/key continuity and PostgreSQL provider/index/decimal/collation cutover checks are specified separately. SQLite maximum key length does not automatically fit PostgreSQL B-tree limits.

## APIs

Normative [backend API design](../api/backend-api-design.md) is the parallel-build contract: exact request/response unions/examples, error/status/auth matrix, ownership, keyset pagination, concurrency and HMAC signing bytes.

Owner surface: POST /api/v1/auth/login; GET /api/v1/me; GET /api/v1/me/channels; GET/POST /api/v1/rules; GET/PUT/DELETE /api/v1/rules/{id}. Update includes expected version; delete strong If-Match. No per-action enable endpoint: enabled toggles via PUT. No ordinary-user event feed.

Admin surface: POST /api/v1/admin/events; GET events/list/detail, sources/list/detail/failures, deliveries/list/detail/attempts. Admin cannot mutate others' rules/configuration/users or replay deliveries. Machine surface: POST /api/v1/webhooks/earthquakes, HMAC-SHA256(timestamp LF raw body) with per-source key,5m skew, durable source/external-ID dedup; user bearer does not substitute.

New manual/webhook event201 after commit; identical retry200 original; changed canonical payload same key409. Admin counts accepted matches/intents; webhook response omits cross-user counts. Uncertain client commit resolves with stable retry key, not a new ID. JSON decimals are strings; UTC timestamps millisecond `Z`. Errors ProblemDetails with safe codes; wrong-owner404, stale version412, missing delete precondition428, caps/writer timeout503, rate limit429. Lists default25/max100 with scope/filter-bound protected cursors.

Explicit framework login emits30m opaque access token; no MapIdentityApi, registration, reset, logout or refresh endpoint. Framework response may include unusable refreshToken; verify exact pinned framework surface in auth task. Contract-first OpenAPI and HTTP examples are backend deliverables, not permission to add a frontend.

## Backend

### Rule and authentication flow

Authenticate, derive subject; validate DTO before mutation; acquire gate<=5s; create scoped transaction; load owned undeleted rule and expected version; enforce quota/available destinations; commit incremented version; release. Identity store writes including failed-login counters/rehash share gate but password hashing/network never holds an open transaction. Verify framework integration with actual stores. Provision accounts idempotently from injected secrets, never reset passwords on startup. Role membership is explicit; authorization rechecked at Application boundary.

### Event acceptance sequence

1. API auth or trusted poll mapping selects source/category/actor; bounded parse and pure canonicalization outside gate. Source requests finish first; no upstream network under lock.
2. Acquire gate and transaction. Lookup canonical key first; equal existing normalized content returns original without rematching (relative age checks only for a new event). Changed controlled content409; poll preserves first value/diagnoses. Suppressed RSS identity remains suppressed.
3. For new key, final validation includes clock/window bounds and slot not expired. Load enabled/undeleted category rules<=200 and selected provisioned destinations. Compute checked predicates/snapshots locally. Reject overflow, unusable destination or backlog capacity rather than omit a rule/channel.
4. Insert immutable event with evaluated count/version; applicable seen row; positive rule-version/filter snapshots; full delivery fan-out. Enforce DB uniqueness/FKs. Commit and release gate; only then respond201 or proceed with poll completion.
5. Worker independently sends intents; API caller never waits for email/Slack. Crash before commit leaves no partial records; after commit all work durable. Request disconnect after commit may hide response, so stable-key retry is required.

### Polling sequence

One poll/source, fixed HTTPS endpoints/no redirects;10s timeout, RSS2MiB/500 items and market64KiB after decompression; safely bounded XML/JSON. Persist attempt/health independently. RSS whole-document validation before baseline, no DTD/resolver; GUID else normalized link; missing/invalid dates rejected, baseline/old suppressions durable; successful validators only after all items resolved. New eligible items use acceptance one-by-one, so rule edits between feed items are allowed. NASA hourly conditional request; no historical page fetch.

Binance one60s UTC slot captured before fetch; exact86400000ms interval with close freshness[-5m,+1m], BTCUSDT/USDT never USD; positive prices bounded and invariant; derive signed percent and compare absolute cross-product before display rounding. Retry retains slot only while unexpired; restart/current slots only, no backfill. Same value in next slot intentionally can notify again. Source state records separate fetch and valid-observation times, failures/backoff/blocking;401/403/404/418 and repeated schema faults never silently switch provider. Source polls independent of delivery recovery.

### Delivery sequence

Each1s scan recovers expired orphaned claims, then claims oldest due Pending in transaction under gate; increment attempt count/create ordinal/token/2m lease; release; send snapshot with15s timeout. Recheck destination identity/revocation without redirect; never substitute current owner destination. Completion under gate requires same token, updates attempt/status only. SMTP success=server accepted message, not human receipt; Slack success=expected HTTP success and body `ok` for approved webhook/fake contract, not arbitrary200 with error text.

Transient network/408/429/5xx/SMTP4xx -> future Pending using10s/30s/2m/10m plus0..10% nonnegative jitter, longer valid Retry-After honored<=24h. Permanent destination/auth/payload error/SMTP5xx, revoked/missing old target, attempts exhausted or Retry-After>24h ->Failed with safe code. Five total attempts, including interrupted claims. Sent/Failed terminal. Never reclaim a local operation while still running; await/cancel bounded I/O first. Startup waits unexpired old lease, recovers expired as ambiguous/abandoned. Crash after receiver acceptance before DB completion may resend; no exactly-once or guaranteed eventual delivery.

### Dependencies (all exact pins require Level2 approval)

Approved stack does not approve arbitrary packages. Proposed EFCore10 Sqlite/Design and Identity.EntityFrameworkCore10; matching ASP.NETCore10 test host; xUnit/Microsoft.NET.Test.Sdk/runner; MailKit SMTP. Prefer shared-framework/BCL HTTP/JSON/XML/crypto/rate-limiting/logging. No Slack SDK, feed SDK, resilience/mediator package or PostgreSQL dependency now. Exact versions, support/licences/vulnerability status must be recorded before restore. HTML-to-text parser/policy remains a named blocker, not an implicit developer selection. Configuration, new dependency and material schema/auth changes follow engineering-policy Level2; destructive/security/production operations retain Level3.

## Frontend

N/A — backend-only approved scope. No UI, frontend framework, browser auth implementation or frontend task. Future consumers can build against approved v1 DTO/OpenAPI with safe GET /me/channels, expected rule versions and documented error handling. Test HTTP clients must discard refresh token, re-login on expiry, treat413/429/503 separately, and understand acceptance is not delivery. Do not add browser token storage/CORS choices as an accidental backend requirement.

## Security

Normative [security design](../architecture/security.md): real Identity, owner/Admin/machine separation, fixed source/destination allowlists, TLS nonlocal, narrow named-service sandbox exception, HMAC raw bytes/skew/replay, safe XML DTD off/resolver null, streaming compressed/decompressed limits, output escaping/mention suppression, secret redaction, separate protected Data Protection keys and provisioned password/secret injection.

Login10/min/IP and5 failures/15m lockout; webhook preauth120/min/IP and60/min/source; owner/admin120/min/subject, manual30/min/admin; global request concurrency20/no queue. These proposals are safety limits, not benchmarked capacity. Single writable process, read-only admin configuration surface, scoped cursors and explicit gate coverage prevent accidental bypass. Egress/DNS/proxy/key-at-rest deployment choice must be resolved before nonlocal operation.

## Observability

Use built-in structured logs/Meter/Activity with correlation/source/event/rule/version/intent/attempt IDs and safe codes, no PII/credentials/raw payloads/full URLs. Counters: accepted/duplicate/suppressed/rejected, match/intent count, poll/fetch/schema failure, auth/rate/owner rejects, delivery attempts/ambiguous/terminal, gate/capacity errors. Gauges/histograms: oldest due/queue age, backlog, source fetch versus observation age, gate wait/transaction/send duration, disk/file growth.

Persist source status/failures and attempt histories for admin inspection; do not depend on log survival. Health/live is process alive, ready checks schema/provisioning/local DB/worker loops; upstream outage is degraded source state, not an automatic crash/restart loop. No telemetry exporter/service dependency or production monitoring promise without approval.

## Testing Strategy

All tests below are future acceptance evidence; documentation research probes are not implementation tests.

| Layer | Required assertions |
|---|---|
| Domain | News NFC/case/entity/substring boundaries, selected normalization policy; exact +5/-5/equality vs104.999 market tests; invariant decimal scale/overflow; exact interval/freshness; Mw boundaries; predicate version/snapshot rendering |
| Persistence (real SQLite) | Actual CHECK/FK/unique behavior, text canonicalization and UTC indexed query translation; long identity key round-trip; all-stage acceptance rollback; zero-match evaluated event; soft delete and snapshot immutability; schema upgrade/backup rehearsal |
| Application races | Same key concurrency; two manual actors same ID; changed payload409; rule edit/pause versus acceptance commit order; stale PUT/DELETE; fan-out capacity/all-selected-channel rollback; all Identity mutations gated |
| Ingestion | RSS first baseline/restart/empty valid feed, invalid XML/DTD/oversize/no-date/future, GUID/link/edited identity, partial poll replay/validator correctness; market slots/expiry/no backfill/stale/interval; HMAC raw bytes/skew/rotation/changed content |
| Auth/API | Anonymous/UserA/UserB/Admin/machine matrix; unsupported Identity routes404; expiry/lockout/gate failures; body limits/strict unions/decimal strings/problem responses; owner hidden404, versions, cursor isolation; log/API redaction |
| Delivery | Only selected channels, immutable old destination after rule change/pause/delete; revoke fails; transient/permanent classifications, Retry-After bounds, fifth attempt terminal, token fencing, no local overlapping recovery; crash after receiver acceptance shows duplicate risk |
| End-to-end | Synthetic/manual/simulated event per category -> matching rule -> local email/fake Slack; nonmatch no intent; persistence across restart; rejected input no partial work; admin inspect; approved live-source smoke separately labelled |
| Security/ops | XML XXE/HTML/mention/CRLF, redirects/decompression/DNS policy, forwarded-IP trust, secrets absent, wrong host sandbox fail-closed, single-instance prevention, capacity pressure/readiness and key persistence/restore |

Fixtures are synthetic with fixed clock/IDs; do not commit live copyrighted feed/market datasets without rights clearance. WebApplicationFactory uses actual SQLite for relational semantics, not EF InMemory. Unit tests have no network; deterministic receiver failures and crash injection in integration harness. Benchmark bounded200-rule/400-intent transaction and sender backlog before claiming supported throughput. Review -> audit -> fix -> retest capped at3 automatic cycles, then escalate blockers.

## Migration / Rollout

Documentation-only now. Before implementing: approve relevant ADRs, contracts/schema/limits/dependencies and reconcile conflicts. Create tested initial SQLite migration; no EnsureCreated or automatic destructive startup changes. Rehearse fresh initialization and additive upgrade; reviewed pre-start maintenance with app stopped, consistent backup and separate protected key/secret configuration. Provision synthetic users/destinations, keep live providers disabled, validate one-process lock and readiness.

Enable sandbox/manual/webhook scenarios only after tests; enable each live source only after provider content/rights/region approval plus explicit runtime smoke checks. Successful public GET research is not permission to enable. Verify source baseline before expecting RSS events; never disable baseline to make a demo appear active. Production deployment/secret changes are not authorized by this plan.

Future schema/channel/provider evolution and dedicated PostgreSQL data transfer/cutover/validation/rollback are specified in [model](../data/data-model-and-evolution.md#dedicated-postgresql-migration-path-not-implemented). Preserve identity/canonical keys/versions/retry snapshots; separately resolve wide-key PostgreSQL indexing and decimal/time parity. Keep single-process gate until architect approves distributed coordination. DB copy alone does not transfer Data Protection portability, credentials or source configuration.

## Implementation Steps

Fixed roadmap IDs below refer to [roadmap](../roadmap/backend-roadmap.md) and actual per-task filenames. Dependency relationships are implementation order, **not dates or estimates**. Each task requires its disclosed technical choices accepted before code; no task may silently choose architecture to clear a blocker.

| ID / task | Requires | Deliverable and exit evidence |
|---|---|---|
| [BE-001 — Scaffold](../roadmap/tasks/BE-001.md) | Technical/dependency baseline approval | Four projects, .NET10 pinned supported tooling, nullable/async/build/test structure, composition skeleton and dependency record; restore/build proof, no hidden packages. |
| [BE-002 — Domain](../roadmap/tasks/BE-002.md) | BE-001; matching/window/baseline and news-text policy approval | Three payload/filter value models, decimal/time/key normalization, pure predicates/snapshots; deterministic bounds/overflow/normalization tests. |
| [BE-003 — Persistence](../roadmap/tasks/BE-003.md) | BE-001,002; schema approval and status reconciliation | Explicit Identity/domain mappings/migration, indexes/checks/converters, writer gate/unit-of-work; real SQLite relational/rollback/restart tests. |
| [BE-004 — Authentication](../roadmap/tasks/BE-004.md) | BE-003; auth/key/secret choices approved | Provisioned roles/accounts and versioned destinations, explicit built-in opaque login, owner/Admin policies, safe me/channels; gate-covered Identity writes, route-inventory/expiry/lockout/redaction tests. |
| [BE-005 — Rules](../roadmap/tasks/BE-005.md) | BE-002,003,004; expected-version reconciliation and rule contract approval | Owner list/get/create/replace/soft-delete and enabled toggle; channel/quota/version validation, keyset paging; A/B/admin isolation and stale-edit tests. |
| [BE-006 — Acceptance/manual](../roadmap/tasks/BE-006.md) | BE-002,003,004,005 | One shared acceptance pipeline, actor-scoped manual keys, event/match/full-intent transaction and snapshots/cap;201/200/409/503 and race/fault rollback evidence. No send required to accept durably. |
| [BE-007 — Webhook](../roadmap/tasks/BE-007.md) | BE-006; HMAC contract/secret provisioning approved | Raw-byte signature authentication, source mapping and earthquake DTO, sample controlled sender contract; valid/invalid/skew/replay/changed-body tests. |
| [BE-008 — Polling](../roadmap/tasks/BE-008.md) | BE-006; approved normalization and adapter contracts; live gate separate | Fixed-source RSS/market adapters, scheduler/source health/seen/baseline/slot state/backoff; offline fixtures and fault/restart evidence. Live smoke only after permissions/currency/region approval. |
| [BE-009 — Delivery](../roadmap/tasks/BE-009.md) | BE-006; state/dependency/receiver contracts approved | Durable claim/send/complete/recover, MailKit and fake-Slack transports, sanitized snapshots; transient/permanent/restart/duplicate ambiguity/recipient revocation tests. Can proceed parallel to007/008 after shared contracts. |
| [BE-010 — Admin/ops](../roadmap/tasks/BE-010.md) | BE-004,006,008,009 | Read-only admin projections/pagination/failure history, health/logging/metrics/rate limits/one-instance safety; authorization/redaction/readiness/backlog tests. |
| [BE-011 — End-to-end](../roadmap/tasks/BE-011.md) | BE-005..010 | Full category/channel sandbox matrix and restart/crash/ordering/limits/security tests; optional approved live-source smoke separately labelled; fix/retest/review/audit evidence. |
| [BE-012 — Handoff/CI](../roadmap/tasks/BE-012.md) | BE-001..011 evidence; unresolved gates explicitly listed | Repeatable CI restore/build/test, OpenAPI/HTTP examples, approved dependency list, local run/provision/backup/restore/migration instructions and evidence register; no production deployment or secret defaults. |

CI scaffolding can be prepared in BE-001; BE-012 finalizes it against actual evidence. Api DTO/OpenAPI agreement precedes consumer integration; schema/gate precedes write handlers; rule/destination state precedes acceptance; acceptance precedes adapters/delivery. Polling and delivery remain independent loops in one host, never independently deployed services.

## Risks

- Provider rights/region unresolved; NASA coverage narrower and BTCUSDT not BTC/USD. Fixtures cannot satisfy real-source acceptance; do not silently downgrade acceptance or substitute providers.
- Per-minute level-triggered market observations may generate repeated alerts; approve behavior/volume.200 matching rules *2 channels can create400 intents/minute while one slow sender cannot sustain it. Limits are safety bounds, not throughput claims; backlog may prevent ingestion and expose capture gaps.
- Local disk/process is a single point of failure; file loss is not solved by durable intents. One process essential for gate semantics; DB/key backup/restore testing essential for recoverability.
- External send acknowledgment/timeout is ambiguous; retries may duplicate and final Failed may follow receipt. No exactly-once or eventual-success promise.
- Source baseline/eviction/cache and no backfill can miss/suppress events; rules are ingestion-time eligible, not publication-time historical matches.
- Decimal TEXT requires strict converters/in-memory comparison; arbitrary SQL numeric queries would be wrong. PostgreSQL wide-key indexes and key-ring portability require explicit migration design, not provider swap.
- Identity explicit-login integration, every write path under gate and HTML text extraction require actual framework/fixture verification; this plan does not prove them.
- History/PII storage grows without retention; production retention/capacity/privacy choices remain open.

## Alternatives Considered

- Async separate matching/outbox stage: lower ingress latency but needs durable unmatched work and historical-rule version semantics; bounded atomic acceptance is simpler for approved scope.
- Separate workers/broker/microservices: more operational/distributed ordering complexity without approved scale requirement; defer to architect if measured limits require them.
- Memory queue/inline send: rejected because restart loses work or network spans transaction and still cannot retract accepted messages.
- Decimal REAL or SQLite text range matching: rejected for precision/ordering correctness; canonical text plus bounded checked decimals instead.
- JWT/custom password/HMAC token scheme for users or MapIdentityApi wholesale: unnecessary/custom security or out-of-scope routes; framework opaque login only. HMAC is separate machine-message authentication using BCL primitives, not user token issuance.
- BBC/Coinbase: research documents conditional alternatives, rights/window differences; not fallback providers. Exact Binance rolling interval is better evidenced, still unapproved.
- BCL SmtpClient versus MailKit: avoiding a package is simpler dependency-wise, but framework guidance discourages SmtpClient for new work; MailKit is a disclosed Level2 proposal, not silently installed.

## Estimated Effort

N/A per user request. No estimates or dates are included in the roadmap/task sequence.

## Definition of Done

- Affected ADRs/contracts/schema/limits/dependency versions accepted; architectural discrepancy notes resolved visibly, no technical proposal mislabelled approved.
- All FR/AC in [requirements](../requirements/alerts-mvp.md) traced to actual tests/evidence; failed/skipped/live-blocked items not marked passed. No new scope/UI/routes.
- Real SQLite transactions/dedup/ordering/constraints, owner/Admin/machine isolation, all selected-channel delivery snapshots, restart/retry/ambiguity, safe bounds and log redaction verified.
- Sandbox email/fake Slack flows reproducible with synthetic data; live NASA/Binance only if approved and actually tested, with precise limitations recorded. Documentation probes remain separate evidence class.
- Reviewer/security audit findings resolved or explicitly approved as remaining risks; automatic loops capped at3 before escalation.
- Build/test/CI, OpenAPI/examples, safe provisioning/config, pre-start migration/backup/restore and separate Data Protection/secret continuity instructions match implementation. No committed credentials/default passwords or destructive automatic operations.
- PostgreSQL/additive evolution is documented honestly, not claimed implemented/tested. No production deployment performed without explicit approval.

## Open Questions

| Gate | Status / owner / required resolution |
|---|---|
| OQ-SD-001 — optimistic rule edits | **BLOCKER BE-005**: received handoff requires version concurrency; initial processing-flows section1 says last-committed edits. Proposed contract is expected version,412 stale,If-Match delete. Architect/orchestrator must align canonical flows before implementation. |
| OQ-SD-002 — delivery state vocabulary | **BLOCKER BE-003/009**: handoff requests Pending/InFlight/Sent/Failed; initial ADR-003/flows use RetryScheduled/Delivered too. Proposed four-state contract maps retry to Pending+NextAttempt and Delivered to Sent without changing retry semantics; architect must reconcile schema/API/ADR. |
| OQ-SD-003 — news HTML-to-text policy | **BLOCKER news portion BE-002/008**: approve vetted parser dependency/version or explicit conservative policy. No regex parser or silently added package; architectural normalization behavior must be deterministic/tested. |
| OQ-SD-004 — technical acceptance | **BLOCKER affected build**: user accepts architecture, schema/API/auth/security limits, baseline/threshold/repeat behavior and exact dependency pins. Documentation authorization alone is insufficient. |
| OQ-SD-005 — live providers | **BLOCKER live enablement**: user/operator approves NASA coverage, Binance BTC/USDT/region/terms/redistribution/attribution and exact24h contract; approve no-backfill baseline/repeat behavior. Offline adapters can be built after their technical contracts are approved. |
| OQ-SD-006 — deployment/key protection | **BLOCKER nonlocal operation**: operator selects service identity/host, TLS/proxy/egress and Data Protection encryption/persistence/backup; do not assume DPAPI keys portable with DB. |
| OQ-SD-007 — production operations | **BLOCKER production use**: retention/privacy/deletion/capacity/real-channel and availability objectives; no unstated SLA, automated purging or scaling proposal. |
| OQ-SD-008 — framework verification | BE-001/003/004 must confirm actual .NET10 pins/login serializer/Identity converter and gated write paths; any materially different contract returns for Level2 approval, not silent implementation adaptation. |

All other limits/schema/endpoints are explicit proposals in the linked documents. Changes to topology/system boundaries return to architect; solution designer does not authorize a replacement architecture.
