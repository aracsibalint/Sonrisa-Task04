# Implementation Plan: Alerts MVP backend

Status: Approved

The user accepted the active six-ADR recovery baseline and Windows local sandbox; see [approval register](../process/approval-register.md). This is implementation authorization for that baseline, **not runtime verification**. Exact approved-family stable pins are delegated after compatibility/license/advisory checks. Provider choice/rights and nonlocal/production operation remain separate gates. This documentation task claims no code, tests, deployment or migration.

## Problem

Build the backend described by [approved scope](../process/approved-scope.md) and [requirements](../requirements/alerts-mvp.md): provisioned users manage owned news/market/earthquake rules; newly accepted events synchronously produce durable selected-channel notification intents; an admin submits manual events and inspects operations. Sandbox email/fake Slack suffice. This package specifies future implementation without adding a UI, registration, arbitrary fetch URLs, historical replay or delivery guarantees.

## Proposed Solution

One .NET10 deployable ASP.NET Core Api with a BackgroundService supervising independent polling and delivery loops. Four projects: Domain, Application, Infrastructure, Api. EFCore/SQLite holds Identity, rules, immutable events, RSS checkpoints/seen keys and durable intents/attempts. One singleton nonreentrant async mutation gate serializes **all** writes. Outer Application use case owns admission once; scoped participants receive internal admission context without reacquisition/capture. A bounded transaction commits event + rule evaluation + all selected intents directly, no match table. No network/hash work under admission/transaction. Send after commit; acknowledge durable acceptance, not delivery.

Use fixed configured sources/destinations, provisioned identities/roles, built-in opaque bearer explicit login, source-specific HMAC earthquake ingress. Conditional live proposals are NASA news RSS and Binance BTCUSDT; approval/rights/region remain blockers. Exact market threshold is absolute inclusive magnitude per poll slot, not crossing; RSS initial baseline is suppressed.

## Architecture

Canonical boundaries/alternatives: [overview](../architecture/overview.md), [processing flows](../architecture/processing-flows.md), [ADR register](../adr/README.md). Architecture authority remains architect; do not implement new topology or quietly resolve conflicts in code.

Dependency direction: Domain none; Application -> Domain; Infrastructure -> Application/Domain; Api -> Application plus Infrastructure composition. One operation-scoped DbContext, never singleton/captured by workers. No mediator, generic repository, plugin framework, broker, event sourcing or separate matcher worker. Hosting boundary creates scopes per poll acceptance/claim/completion. Async I/O with CancellationToken throughout, no blocking waits. Reads AsNoTracking/projected/paged; pure bounded decimal predicates in memory by explicit SQLite design exception.

One Windows process and local-NTFS SQLite only; gate is not distributed. Before every writer including migration/provisioning, canonicalize/validate DB path and hold sidecar FileStream(FileShare.None) for lifetime. Non-thread-affine/crash release; existence is not ownership, leave sidecar/no stale deletion. Reject UNC/network/ADS/reparse traversal/unsupported aliases/hardlinks; verify final identity/link counts, fail closed. BE-003 tests actual mechanics; no hostile-local-operator guarantee. No replicas/network DB. Separate current-user DPAPI key ring accepted for sandbox; no Docker requirement. Explicit pre-start migrations/schema checks, independent loop health, no destructive self-repair. PostgreSQL is separately approved future work.

## Components

| Project/component | Responsibility / exact handoff |
|---|---|
| Domain — EventCandidate and three typed payloads | News (title/summary/link/publication), market (instrument/prices/exact interval), earthquake (Mw magnitude/simulated); immutable normalization/version values, checked decimal rules, validation |
| Domain — Rule and three filters | Owner identity, category-specific filter, selected channels, enabled/deleted, expected version; no EF/Identity references |
| Domain — MatchPredicates | NFC ordinal-ignore-case news substring; market exact scale8 BigInteger cross-product >= (lift before arithmetic, no decimal rounding); Mw inclusive >=; deterministic predicate version1 |
| Application — rule handlers | Create/Get/List/Replace/DeleteOwnedRule; enforce owner/quota/channel availability and expected-version checks under gate for mutations; no historical scan |
| Application — AcceptEvent | Trusted source/actor + stable key + canonical candidate; equality check, bounded active rules, pure match/render, full capacity/disk precheck, atomic event/seen/intents commit; no match entity |
| Application — delivery handlers | ClaimDueIntent, CompleteAttempt, RecoverExpiredClaims; token-fenced updates, attempt budget, immutable recipient snapshots, transient/permanent classification policy |
| Application ports | Narrow IEventAcceptanceStore, IRuleStore, ISourceStateStore, IDeliveryStore, IIdentitySessionService, IChannelDirectory, ISourcePoller, INotificationSender, IClock and IAsyncMutationGate; unit-of-work operation boundary rather than generic repository CRUD |
| Infrastructure — persistence | EF mappings/converters/checks/indexes/migrations, Identity stores, transaction/gate implementation, canonical keys/fingerprints, versioned config destination lookup |
| Infrastructure — input adapters | Fixed NASA RSS, Binance ticker and manual/webhook normalizers; network outside gate; source baseline/seen/validator/backoff semantics; no live enablement before approval |
| Infrastructure — output adapters | Approved MailKit family SMTP sender and typed HttpClient fake/real Slack sender; immutable target secret resolution, bounded safe rendering/response validation; real channel enablement separately gated |
| Api — transport/hosting | v1 DTOs, ProblemDetails/auth/ownership policy wiring, HMAC raw-byte validation, limits, DI, explicit login and BackgroundService lifecycle, health/readiness, OpenAPI surface |

Ports describe responsibility boundaries, not a generic framework per row. Concrete signatures/naming are developer-level choices within accepted contracts; no component may move predicates into endpoints or accept caller-selected source/destination URLs.

## Data Model

Normative [data model and evolution](../data/data-model-and-evolution.md) specifies fields/nullability/CHECK/FK/uniques/indexes. Entities: Identity tables, ChannelDestination, SourceState, SeenSourceItem, SourceFailure, Event, Rule, NotificationIntent, DeliveryAttempt. Direct Event+Rule+Owner+Destination -> NotificationIntent edges; no match table or identifier.

- Canonical invariant decimal TEXT with strict converter/domain checks; numeric comparisons in bounded in-memory decimal operations, never database ordering/range of numeric text or REAL conversions. UTC epoch milliseconds INTEGER for scheduling; no SQLite DateTimeOffset comparison assumptions.
- Event unique `(SourceId,ExternalKey)`; manual key includes authenticated actor, webhook key uses source+producer ID, RSS GUID/link key is versioned, market key is source/instrument/window/schedule/slot. Payload canonical equality: unchanged retry original, changed controlled input409, changed poll diagnostic only. Identity strings use ordinal comparison and collision-free encoding, not ambiguous concatenation.
- RSS baseline/seen identities persist; initial acceptable200 suppresses current valid items atomically, later new eligible entries commit one at a time. Validators advance only after complete response processing; partial failure dedups earlier committed items on replay.
- Rule soft delete, immutable category, optimistic Version/ETag;20 active/owner,100 active global, paused excluded. Creation/activation checks quota under gate. Commit ordering selects successful rule version for event acceptance; old snapshots survive pause/delete.
- Event transaction includes evaluated count/version, no match rows/unprocessed queue. Unique(EventId,RuleId,Channel), max200 intents/event; projected Pending+InFlight+RetryScheduled<=20,000, retained all statuses<=250,000; all-or-nothing fan-out.
- Intent directly contains immutable RuleVersion, RuleNameSnapshot, Category, FilterSnapshotJson(max2048 validated), PredicateVersion, MatchedAtUtcMs, recipient/config/payload versions and rendered body; no raw Slack URLs. Five states Pending/InFlight/Delivered/RetryScheduled/Failed and OutcomeUnknown attempts; token-fenced history survives restart.
- SourceState.ActivatedAtUtcMs persists source initialization/enable time, never resets on process restart; staleness falls back to it before first successful fetch/observation.
- No automatic retention purge; schema evolution, backup/key continuity and PostgreSQL provider/index/decimal/collation cutover checks are specified separately. SQLite maximum key length does not automatically fit PostgreSQL B-tree limits.

## APIs

Normative [backend API design](../api/backend-api-design.md) is the parallel-build contract: exact request/response unions/examples, error/status/auth matrix, ownership, keyset pagination, concurrency and HMAC signing bytes.

Owner surface: POST /api/v1/auth/login; GET /api/v1/me; GET /api/v1/me/channels; GET/POST /api/v1/rules; GET/PUT/DELETE /api/v1/rules/{id}. Update includes expected version; delete strong If-Match. No per-action enable endpoint: enabled toggles via PUT. No ordinary-user event feed.

Admin surface: POST /api/v1/admin/events; GET events/list/detail, sources/list/detail/failures, deliveries/list/detail/attempts. Admin cannot mutate others' rules/configuration/users or replay deliveries. Machine surface: POST /api/v1/webhooks/earthquakes, HMAC-SHA256(timestamp LF raw body) with per-source key,5m skew, durable source/external-ID dedup; user bearer does not substitute.

New manual/webhook event201 after commit; identical retry200 original; changed canonical payload same key409. Admin counts accepted matches/intents; webhook response omits cross-user counts. Uncertain client commit resolves with stable retry key, not a new ID. JSON decimals are strings; UTC timestamps millisecond `Z`. Errors ProblemDetails with safe codes; wrong-owner404, stale version412, missing delete precondition428, caps/writer timeout503, rate limit429. Lists default25/max100 with scope/filter-bound protected cursors.

Explicit framework login emits15m opaque access token(expiresIn900); no MapIdentityApi, registration/reset/logout/refresh endpoint. Framework refreshToken is unused/discard-only; verify pinned framework surface in auth task. OpenAPI/HTTP examples are backend deliverables, not frontend permission.

## Backend

### Rule and authentication flow

Authenticate/derive subject; validate DTO; outer Application use case acquires once(max32 waiters,2s deadline; SQLite busy1s), measures disk admission, creates scoped short transaction, loads owned rule/version, enforces active quota/destinations, commits incremented version, releases. Scoped participants use internal admission context without reacquisition/capture. Every Identity write including failed count/reset/rehash/lockout/stamps/provisioning shares admission. Hash/verify outside admission/transaction; reload relevant hash/stamps/lockout under admission before sign-in; if changed release for bounded re-verification outside admission then reload. No credential issuance without required commit. Idempotent injected-secret provisioning never resets existing passwords on restart. Explicit roles/Application authorization.

### Event acceptance sequence

1. API auth or trusted poll mapping selects source/category/actor; bounded parse and pure canonicalization outside gate. Source requests finish first; no upstream network under lock.
2. Acquire gate and transaction. Lookup canonical key first; equal existing normalized content returns original without rematching (relative age checks only for a new event). Changed controlled content409; poll preserves first value/diagnoses. Suppressed RSS identity remains suppressed.
3. For new key, validate clock/window/slot; load enabled/undeleted category rules<=100 and destinations. Compute predicates/snapshots; check max200 fan-out, projected outstanding20,000/retained250,000 and disk headroom. Reject overflow/unusable target/capacity rather than omit a rule/channel.
4. Insert immutable event/evaluated count/version, applicable seen row and all intents with direct rule/filter snapshots; no match rows. Enforce uniques/FKs; commit/release before201 or poll completion. Zero-match events still require normal growth admission.
5. Worker independently sends intents; API caller never waits for email/Slack. Crash before commit leaves no partial records; after commit all work durable. Request disconnect after commit may hide response, so stable-key retry is required.

### Polling sequence

One poll/source, fixed HTTPS/no redirects;10s timeout, RSS2MiB compressed/decompressed/max100 items, market64KiB compressed/decompressed; bounded XML/JSON. News title500/summary4000/combined16KiB UTF-8; approved AngleSharp family deterministic text extraction, RSS summary truncated/flagged before matching, manual oversize rejected. Persist attempt/health under admission. RSS whole-document validation before baseline, no DTD/resolver; GUID else normalized link; invalid dates rejected, baseline/old suppressions durable; validators only after all items resolved. Accept eligible items one-by-one; rule edits between items allowed. NASA candidate hourly, no historical fetch. Persist source activation at enable, not restart.

Binance one60s UTC slot captured before fetch; exact86400000ms interval with close freshness[-5m,+1m], BTCUSDT/USDT never USD; positive prices bounded and invariant. Lift price/threshold decimal coefficients to scale8 BCL BigInteger values before exact absolute cross-product comparison; checked decimal products can silently round. Derived signed decimal percent is display-only and may round, never authority for matching. Retry retains slot only while unexpired; restart/current slots only, no backfill. Same value in next slot intentionally can notify again. Source state records separate fetch and valid-observation times, failures/backoff/blocking;401/403/404/418 and repeated schema faults never silently switch provider. Source polls independent of delivery recovery.

### Delivery sequence

Each1s scan waits prior unexpired leases and recovers only expired orphaned claims after cancel/await of local live operations. One dispatcher/channel claims oldest due Pending or RetryScheduled under admission/transaction with completion headroom, increments ordinal/token/2m lease; releases and sends with10s I/O timeout. Recheck immutable target/revocation, never redirect/substitute. Completion under admission is token-fenced. SMTP success=server acceptance, Slack=expected HTTP success/body `ok`, not arbitrary200. Neither means human receipt.

Transient network/timeout/408/429/5xx/SMTP4xx -> RetryScheduled after ordinal n using30s*2^(n-1),0..10% nonnegative jitter, local cap30m; honor larger valid Retry-After with no24h terminal cutoff. Permanent rejection/revocation/missing old target or fifth failed attempt ->Failed safe code. Pending=count0/due; InFlight=count1..5/claim+lease; Delivered=count1..5/deliveredAt; RetryScheduled=count1..4/due; Failed=count1..5, or count0 only explicit PermanentProvisioningFailure before any claim/send(no fabricated attempt). Count interrupted claims; recover expired orphaned attempts as OutcomeUnknown then retry if budget remains else Failed. Delivered/Failed terminal. Receiver acceptance before lost acknowledgement/commit can duplicate sends; no exactly-once/eventual-delivery promise.

### Disk admission and storage failure

Measure every mutation/all growth, including zero-match events, seen keys, rules and failure history. Normal mutation requires1GiB free after16MiB conservative growth allowance. Emergency256MiB after1MiB allowance is only for bounded send-result/recovery and essential Identity security bookkeeping. No send without completion headroom. Below emergency, unmeasurable space or DB write/checkpoint failure stops claims/sends/mutations and fails readiness503; no login credential/acceptance success without required commit. Use bounded nonpersistent redacted diagnostics, preventing recursive failure-history growth; no automatic deletion. Budgets are unmeasured/not OS reservations. BE-003 tests actual WAL/checkpoint/headroom behavior.

### Dependencies (approved families; exact pins delegated after checks)

Accepted families: EFCore10 Sqlite/Design, Identity.EntityFrameworkCore10, ASP.NETCore OpenAPI/test host, xUnit v3/test SDK/runner, MailKit, AngleSharp, Mailpit. Select maintained compatible stable pins after support/license/advisory checks, introduce when needed; SDK10.0.303 selection is delegated per register. No unselected-parser blocker. Prefer BCL/framework HTTP/JSON/XML/crypto/rate limiting/logging. No Slack/feed SDK, resilience/mediator/PostgreSQL dependency. New families/material incompatibilities or semantic/schema/auth departures return for approval; destructive/security/production operations retain Level3. No package installation claimed.

## Frontend

N/A — backend-only approved scope. No UI, frontend framework, browser auth implementation or frontend task. Future consumers can build against approved v1 DTO/OpenAPI with safe GET /me/channels, expected rule versions and documented error handling. Test HTTP clients must discard refresh token, re-login on expiry, treat413/429/503 separately, and understand acceptance is not delivery. Do not add browser token storage/CORS choices as an accidental backend requirement.

## Security

Normative [security design](../architecture/security.md): real Identity, owner/Admin/machine separation, fixed source/destination allowlists, TLS nonlocal, narrow named-service sandbox exception, HMAC raw bytes/skew/replay, safe XML DTD off/resolver null, streaming compressed/decompressed limits, output escaping/mention suppression, secret redaction, separate protected Data Protection keys and provisioned password/secret injection.

Login10/min/IP and5 failures/15m lockout; webhook preauth120/min/IP and60/min/source; owner/admin120/min/subject, manual30/min/admin; global request concurrency20/no queue. Approved safety limits are unbenchmarked. Single-instance ownership, read-only admin configuration, scoped cursors and gate coverage require tests. Current-user DPAPI is accepted locally; nonlocal egress/DNS/proxy/host/key controls remain gated.

## Observability

Use built-in structured logs/Meter/Activity with correlation/source/event/rule/version/intent/attempt IDs and safe codes, no PII/credentials/raw payloads/full URLs. Counters: accepted/duplicate/suppressed/rejected, match/intent count, poll/fetch/schema failure, auth/rate/owner rejects, delivery attempts/OutcomeUnknown/terminal, gate/capacity errors. Gauges/histograms: oldest due/queue age, backlog, source fetch versus observation age, gate wait/transaction/send duration, disk/file growth.

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

Fixtures synthetic/fixed clock/IDs; no live copyrighted datasets without rights clearance. WebApplicationFactory uses real SQLite, not EF InMemory. Unit tests offline; deterministic receiver failures/crash injection. Benchmark100-active-rule/200-intent transaction/backlog before throughput claims. BE-003 additionally tests Windows sidecar lifetime/aliases/link checks/crash release, gate nonreentrancy/admission propagation, every mutation's disk admission and WAL/checkpoint failure. BE-004 tests hash/stamp/lockout races and no credential issuance without required commit. Review -> audit -> fix -> retest max3 automatic cycles then escalate.

## Migration / Rollout

Documentation-only in this task; baseline implementation is approved, not verified. Select/check approved-family pins, create tested initial SQLite migration; no EnsureCreated/automatic destructive changes. Rehearse initialization/additive upgrade; reviewed stopped-app maintenance under lifetime writer ownership, consistent backup and separate DPAPI key/secret config. Provision synthetic users/destinations, keep live providers disabled, validate path/lock/headroom/readiness behavior.

Enable sandbox/manual/webhook scenarios only after tests; enable each live source only after provider content/rights/region approval plus explicit runtime smoke checks. Successful public GET research is not permission to enable. Verify source baseline before expecting RSS events; never disable baseline to make a demo appear active. Production deployment/secret changes are not authorized by this plan.

Future schema/channel/provider evolution and dedicated PostgreSQL data transfer/cutover/validation/rollback are specified in [model](../data/data-model-and-evolution.md#dedicated-postgresql-migration-path-not-implemented). Preserve identity/canonical keys/versions/retry snapshots; separately resolve wide-key PostgreSQL indexing and decimal/time parity. Keep single-process gate until architect approves distributed coordination. DB copy alone does not transfer Data Protection portability, credentials or source configuration.

## Implementation Steps

Fixed IDs refer to [roadmap](../roadmap/backend-roadmap.md) and per-task filenames (not edited here). Dependencies are order, **not dates/estimates**. Baseline is accepted; prerequisites/tests and remaining provider/nonlocal gates still apply. No task may silently invent architecture to clear a blocker.

| ID / task | Requires | Deliverable and exit evidence |
|---|---|---|
| [BE-001 — Scaffold](../roadmap/tasks/BE-001.md) | Accepted baseline; delegated version checks | Four projects, pinned supported .NET10 tooling, nullable/async/build/test/composition/dependency record; restore/build proof, no hidden packages. |
| [BE-002 — Domain](../roadmap/tasks/BE-002.md) | BE-001; accepted matching/baseline/AngleSharp family | Three payload/filter models, decimal/time/key normalization, pure predicates/snapshots; deterministic bounds/overflow/text tests. |
| [BE-003 — Persistence](../roadmap/tasks/BE-003.md) | BE-001,002; accepted schema/five states | Identity/domain migration/CHECK/FK/index/converters, outer-owned nonreentrant gate, direct intent snapshots; real SQLite rollback/restart/WAL/checkpoint/headroom and Windows lifetime sidecar/path/link/crash tests. |
| [BE-004 — Authentication](../roadmap/tasks/BE-004.md) | BE-003; auth/key/secret choices approved | Provisioned roles/accounts and versioned destinations, explicit built-in opaque login, owner/Admin policies, safe me/channels; gate-covered Identity writes, route-inventory/expiry/lockout/redaction tests. |
| [BE-005 — Rules](../roadmap/tasks/BE-005.md) | BE-002,003,004; accepted expected-version contract | Owned CRUD/soft-delete/enabled toggle; active20-owner/100-global quotas/channel/version/keyset validation; isolation/stale-edit tests. |
| [BE-006 — Acceptance/manual](../roadmap/tasks/BE-006.md) | BE-002,003,004,005 | Shared pipeline, actor-scoped keys, event/full-intent direct snapshots/caps/disk transaction(no match table);201/200/409/503, races/rollback. No send required for durable acceptance. |
| [BE-007 — Webhook](../roadmap/tasks/BE-007.md) | BE-006; HMAC contract/secret provisioning approved | Raw-byte signature authentication, source mapping and earthquake DTO, sample controlled sender contract; valid/invalid/skew/replay/changed-body tests. |
| [BE-008 — Polling](../roadmap/tasks/BE-008.md) | BE-006; approved normalization and adapter contracts; live gate separate | Fixed-source RSS/market adapters, scheduler/source health/seen/baseline/slot state/backoff; offline fixtures and fault/restart evidence. Live smoke only after permissions/currency/region approval. |
| [BE-009 — Delivery](../roadmap/tasks/BE-009.md) | BE-006; state/dependency/receiver contracts approved | Durable claim/send/complete/recover, MailKit and fake-Slack transports, sanitized snapshots; transient/permanent/restart/duplicate ambiguity/recipient revocation tests. Can proceed parallel to007/008 after shared contracts. |
| [BE-010 — Admin/ops](../roadmap/tasks/BE-010.md) | BE-004,006,008,009 | Read-only admin projections/pagination/failure history, health/logging/metrics/rate limits/one-instance safety; authorization/redaction/readiness/backlog tests. |
| [BE-011 — End-to-end](../roadmap/tasks/BE-011.md) | BE-005..010 | Full category/channel sandbox matrix and restart/crash/ordering/limits/security tests; optional approved live-source smoke separately labelled; fix/retest/review/audit evidence. |
| [BE-012 — Handoff/CI](../roadmap/tasks/BE-012.md) | BE-001..011 evidence; unresolved gates explicitly listed | Repeatable CI restore/build/test, OpenAPI/HTTP examples, approved dependency list, local run/provision/backup/restore/migration instructions and evidence register; no production deployment or secret defaults. |

CI scaffolding can be prepared in BE-001; BE-012 finalizes it against actual evidence. Api DTO/OpenAPI agreement precedes consumer integration; schema/gate precedes write handlers; rule/destination state precedes acceptance; acceptance precedes adapters/delivery. Polling and delivery remain independent loops in one host, never independently deployed services.

## Risks

- Provider rights/region unresolved; NASA coverage narrower and BTCUSDT not BTC/USD. Fixtures cannot satisfy real-source acceptance; do not silently downgrade acceptance or substitute providers.
- Accepted per-minute level-triggered market observations may repeat alerts.100 matching rules*2 channels can create200 intents/minute while one slow dispatcher/channel may not sustain it. Safety bounds are not throughput claims; backlog can prevent ingestion/expose capture gaps. Provider choice/rights remain gated.
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
- Decimal REAL or SQLite text range matching: rejected for precision/ordering correctness; canonical decimal text plus bounded in-memory predicates instead. Market cross-products use exact scale8 BCL BigInteger coefficients, not rounded decimal multiplication.
- JWT/custom password/HMAC token scheme for users or MapIdentityApi wholesale: unnecessary/custom security or out-of-scope routes; framework opaque login only. HMAC is separate machine-message authentication using BCL primitives, not user token issuance.
- BBC/Coinbase: conditional alternatives with rights/window differences, not fallback providers. Binance remains candidate, not cleared provider.
- BCL SmtpClient versus MailKit: framework guidance discourages SmtpClient for new work; MailKit family is accepted, pins delegated after checks, not installed by this documentation task.

## Estimated Effort

N/A per user request. No estimates or dates are included in the roadmap/task sequence.

## Definition of Done

- Accepted baseline reflected consistently; approved-family pins checked/recorded; material departures and still-gated providers/nonlocal/production choices not mislabelled approved or verified.
- All FR/AC in [requirements](../requirements/alerts-mvp.md) traced to actual tests/evidence; failed/skipped/live-blocked items not marked passed. No new scope/UI/routes.
- Real SQLite transactions/dedup/ordering/constraints, owner/Admin/machine isolation, all selected-channel delivery snapshots, restart/retry/ambiguity, safe bounds and log redaction verified.
- Sandbox email/fake Slack flows reproducible with synthetic data; live NASA/Binance only if approved and actually tested, with precise limitations recorded. Documentation probes remain separate evidence class.
- Reviewer/security audit findings resolved or explicitly approved as remaining risks; automatic loops capped at3 before escalation.
- Build/test/CI, OpenAPI/examples, safe provisioning/config, pre-start migration/backup/restore and separate Data Protection/secret continuity instructions match implementation. No committed credentials/default passwords or destructive automatic operations.
- PostgreSQL/additive evolution is documented honestly, not claimed implemented/tested. No production deployment performed without explicit approval.

## Open Questions

| Gate | Status / owner / required resolution |
|---|---|
| OQ-SD-001 — optimistic rule edits | **Resolved baseline**: Version/ETag,412 stale PUT/DELETE,If-Match delete; commit ordering selects eligible successful version, not last-write-wins. Runtime race tests required. |
| OQ-SD-002 — delivery state vocabulary | **Resolved baseline**: Pending/InFlight/Delivered/RetryScheduled/Failed, OutcomeUnknown; no match table; direct immutable intent snapshots. Runtime state/recovery tests required. |
| OQ-SD-003 — news HTML-to-text policy | **Resolved family approval**: AngleSharp, exact compatible stable pins delegated after checks; deterministic malformed-HTML/no-fetch/Unicode tests required. No parser-selection blocker. |
| OQ-SD-004 — technical acceptance | **Accepted by user**: active six ADRs, recovery limits/schema/API/auth, baseline/threshold/repeat behavior and Windows local sandbox. Approved-family version selection delegated, not arbitrary dependency/material departure approval. |
| OQ-SD-005 — providers | **BLOCKER final provider-specific contract/live enablement**: NASA/Binance remain candidates; user/operator must approve coverage/currency/window fit, region/terms/reuse/recipients/attribution. Technical repeat/suppressed-baseline semantics are accepted, not rights clearance. Synthetic testing does not clear gate. |
| OQ-SD-006 — deployment/key protection | **Local choice accepted**: Windows current-user DPAPI, local NTFS single instance/loopback receivers, synthetic data, no Docker requirement. **BLOCKER nonlocal**: approve host/TLS/proxy/egress/key lifecycle; DPAPI not portable with DB. |
| OQ-SD-007 — production operations | **BLOCKER production use**: retention/privacy/deletion/capacity/real-channel and availability objectives; no unstated SLA, automated purging or scaling proposal. |
| OQ-SD-008 — framework verification | BE-001/003/004 must confirm actual .NET10 pins/login serializer/Identity converter and gated write paths; any materially different contract returns for Level2 approval, not silent implementation adaptation. |

Baseline limits/schema/endpoints are accepted build contracts, runtime unverified. Windows path/link ownership and disk/WAL/checkpoint mechanics are BE-003 verification work, not a hostile-local-operator guarantee or OS capacity reservation. Material topology/system-boundary changes return for approval; this document persists specialist conclusions rather than authoring replacement architecture.
