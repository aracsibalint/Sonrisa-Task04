# Backend roadmap

Status: **Proposed — implementation not approved.** All chunks are **Not started (approval/dependency gated)**. This roadmap defines order and evidence, not a schedule. No implementation, runtime verification or milestone commit is claimed.

## Authority and canonical contracts

[Approved scope](../process/approved-scope.md) and [requirements](../requirements/alerts-mvp.md) define the product. Read [planning approach](../process/planning-approach.md) and [shared AI build-session rules](../process/ai-build-session.md) before executing any chunk.

Technical proposals are owned by [architecture overview](../architecture/overview.md), [processing flows](../architecture/processing-flows.md), [data model and evolution](../data/data-model-and-evolution.md), [backend API](../api/backend-api-design.md), [security](../architecture/security.md) and [ADR register](../adr/README.md). Task paths/components are proposed working locations, not new API/schema contracts. Reconcile disagreements in those canonical documents and obtain approval before coding.

The proposed shape is one deployable modular .NET 10 application with API and in-process background workers, EF Core SQLite, bounded synchronous event/matching/durable-intent acceptance in one transaction, and one in-process mutation gate covering every writer, including Identity lockout/provisioning. Matching is evaluated in memory; intents contain immutable match/rule/filter/version snapshots. There is no separate Match/RuleMatch table or matching queue. Real seeded Identity opaque bearer authentication, centrally provisioned sources/destinations and simple typed rules are design proposals. Nothing here authorizes another deployment unit, a source/destination CRUD API or a generalized rules engine.

## User approval checkpoint — before BE-001

Record the user's explicit acceptance of the technical architecture, applicable ADRs, matching/repeat/baseline semantics, API and persistence contracts, security approach, operating bounds and dependency inventory with exact compatible package/tool versions. Resolve material contradictions or missing contracts first. Approval of documentation quality is not implementation approval. Later added dependencies or changed contracts need renewed approval.

The recovery overview, processing flows and six ADRs linked by the register settle the proposed direction: intent-contained matching snapshots; `Pending/InFlight/Delivered/RetryScheduled/Failed`; one dispatcher per channel; application-managed rule Version; HMAC-authenticated earthquake requests; and AngleSharp as the proposed HTML parser dependency. Commit ordering and optimistic version preconditions are complementary, not competing concurrency policies. These remain user-approval gates, not missing architectural choices. Use the ADR register, not similarly numbered superseded files.

### Residual cross-document alignment checkpoint

At this follow-up's inspection, the schema/API/security files still contained the following exact differences. The lead/designer owns their correction; verify the forthcoming revisions before implementation. This records observed draft drift, not a second proposed design or permission to implement stale contracts.

| Area | Recovery architecture / registered ADRs | Observed schema/API/security difference | Affected tasks |
|---|---|---|---|
| Persistence | No separate match table; snapshots on intents | Schema defines RuleMatch and required NotificationIntent.MatchId FK | BE-003, BE-006 |
| Delivery | Delivered and RetryScheduled; interrupted outcome OutcomeUnknown; one dispatcher/channel | Schema/API use Sent and Pending-for-retry; schema attempt enum uses Ambiguous/Abandoned; schema/security require one global send | BE-003, BE-009, BE-010 |
| Identity | Access lifetime in overview/ADR-004 | API/security retain a different access lifetime and login response expiry | BE-004 |
| Rule/fan-out bounds | Active-rule caps and complete fan-out bounded by overview | Schema/API/security use larger total undeleted-rule/fan-out bounds, including paused rules | BE-003, BE-005, BE-006 |
| Admission/retention | Bounded waiter count/deadline plus outstanding and retained-intent caps | Schema/API/security use different gate/backlog bounds and omit the overview's retained-intent admission cap | BE-003, BE-006, BE-010 |
| Source/text/delivery bounds | RSS item/text limits, network timeout and retry policy in overview | Schema/API/security retain different RSS/string/send/retry bounds and long-Retry-After terminal policy | BE-002, BE-008, BE-009 |
| Dependencies | AngleSharp proposed in overview | Security still describes HTML parser selection as undecided | BE-001, BE-002, BE-008 |

Exact replacement values belong to the canonical overview and forthcoming schema/API/security, not duplicated roadmap configuration. The existing explicit Version/ETag details should be retained only as reconciled by their owner; no last-write-wins alternative is introduced here. Remove resolved rows when the corresponding canonical revisions are confirmed.

Provider selection, rights, recipients, attribution and regional eligibility are a separate gate. [Source feasibility](../research/source-feasibility.md) proposes candidates; HTTP success does not establish permission. Synthetic BE-008 work can proceed after technical approval while live use is blocked. It cannot close AC-006/AC-015 or be reported as completed live integration. Final MVP acceptance remains blocked until the approved real RSS and market flows are demonstrated.

## Dependency-ordered chunks

Every dependency below is a prerequisite for claiming completion; approval gates apply in addition. BE-002 defines domain types before BE-003 maps them. The graph is acyclic.

| Task | Scope | Explicit dependencies | Status |
|---|---|---|---|
| [BE-001](tasks/BE-001.md) | Solution/tooling scaffold | User technical/dependency approval checkpoint | Not started (approval/dependency gated) |
| [BE-002](tasks/BE-002.md) | Domain validation and matching | BE-001 | Not started (approval/dependency gated) |
| [BE-003](tasks/BE-003.md) | Relational model/migrations | BE-001, BE-002 | Not started (approval/dependency gated) |
| [BE-004](tasks/BE-004.md) | Seeded identity/authorization | BE-003 | Not started (approval/dependency gated) |
| [BE-005](tasks/BE-005.md) | Owner rule APIs/provisioned channels | BE-002, BE-003, BE-004 | Not started (approval/dependency gated) |
| [BE-006](tasks/BE-006.md) | Atomic event acceptance/manual admin input | BE-002, BE-003, BE-004, BE-005 | Not started (approval/dependency gated) |
| [BE-007](tasks/BE-007.md) | Authenticated simulated earthquake webhook | BE-006 | Not started (approval/dependency gated) |
| [BE-008](tasks/BE-008.md) | RSS and market adapters/polling | BE-006; provider gates for live operation/completion | Not started (approval/dependency gated) |
| [BE-009](tasks/BE-009.md) | Durable delivery worker/sandbox adapters | BE-006 | Not started (approval/dependency gated) |
| [BE-010](tasks/BE-010.md) | Admin inspection/health/logging | BE-004, BE-006, BE-008, BE-009 | Not started (approval/dependency gated) |
| [BE-011](tasks/BE-011.md) | Adversarial end-to-end/restart verification | BE-001, BE-002, BE-003, BE-004, BE-005, BE-006, BE-007, BE-008, BE-009, BE-010 | Not started (approval/dependency gated) |
| [BE-012](tasks/BE-012.md) | Reproducible demo/handoff/CI/submission evidence | BE-011 | Not started (approval/dependency gated) |

After BE-006, BE-007/BE-008/BE-009 have no mutual prerequisite. Coordinate edits to shared host composition and contracts. This permits independent work; it does not authorize uncontrolled parallel changes. Where provider approval blocks BE-008 completion, downstream fixture-only checks can be prepared against its implemented synthetic interface, with the dependency explicitly recorded as unmet. BE-010/BE-011/BE-012 cannot be marked complete by silently waiving that prerequisite.

## Stages and exit milestones

| Stage | Chunks | Observable exit milestone / review checkpoint |
|---|---|---|
| Approval | Before BE-001 | User accepts disclosed technical design and exact dependencies; unresolved live-provider gate recorded separately. |
| Foundation | BE-001–BE-003 | Reproducible build, pure boundary-tested predicates, reviewed SQLite schema/migrations and relational constraints. |
| Secure rule surface | BE-004–BE-005 | Real seeded login, role/owner isolation and validated provisioned-channel rule lifecycle. |
| Durable acceptance | BE-006 | Admin input atomically persists event and complete intent fan-out with embedded matching/rule snapshots; rollback and ordering evidence. |
| Ingress and dispatch | BE-007–BE-009 | Machine-authenticated simulated ingress, approved live RSS/market normalization and recoverable independent sandbox delivery. Fixture-only source results remain partial. |
| Operations | BE-010 | Sanitized admin inspection, accurate health and observable failure/backlog state. |
| Verification | BE-011 | All acceptance criteria have reviewed evidence, including real SQLite fault/restart behavior, sandbox receipts and separately approved live-source checks. |
| Handoff | BE-012 | Clean-checkout demo, offline CI, explicit live evidence, prompt provenance, limitations and authorized milestone history are reproducible. |

Each stage is reviewed under the shared build-session rules. Risk-relevant audits and corrections precede completion. Suggested per-task commits are future milestones, never proof that work ran; creation requires explicit authorization.

## Acceptance trace matrix

AC wording and authority remain in [requirements](../requirements/alerts-mvp.md). BE-011 verifies the integrated acceptance matrix; BE-012 preserves reproducibility and evidence for every criterion.

| Acceptance | Implementing / focused verification tasks | Integrated evidence / qualification |
|---|---|---|
| AC-001 — seeded login | BE-003, BE-004 | BE-011, BE-012; actual Identity login, invalid credentials and restart. |
| AC-002 — owner lifecycle | BE-004, BE-005 | BE-011, BE-012; user A/user B/admin isolation. |
| AC-003 — category/filter/channel validity | BE-002, BE-005 | BE-011, BE-012; reject incompatible filters and unavailable channels. |
| AC-004 — category predicate boundaries | BE-002, BE-006, BE-007, BE-008 | BE-011, BE-012; exact inclusive and nonmatch fixtures. |
| AC-005 — new events only | BE-005, BE-006, BE-008, BE-009 | BE-011, BE-012; no rule-change replay; existing intents remain snapshots. |
| AC-006 — real configured RSS/market | BE-008 | BE-011, BE-012; approved live endpoints required, fixtures insufficient. |
| AC-007 — authenticated simulated earthquake | BE-007 | BE-011, BE-012; rejected auth leaves no persisted event/intent. |
| AC-008 — admin manual categories/shared pipeline | BE-006 | BE-011, BE-012; non-admin denied and normalized behavior equivalent. |
| AC-009 — admin inspection/no cross-owner edit | BE-005, BE-010 | BE-011, BE-012; source/delivery failures visible and sanitized. |
| AC-010 — selected sandbox channels | BE-005, BE-006, BE-009 | BE-011, BE-012; capture receipts and independent failures. |
| AC-011 — authorization matrix | BE-004, BE-005, BE-006, BE-007, BE-010 | BE-011, BE-012; anonymous/user A/user B/admin plus isolated machine credential. |
| AC-012 — stack/persistence/PostgreSQL path | BE-001, BE-003, BE-004, BE-009 | BE-011, BE-012; restart evidence and documented, unexecuted future migration path. |
| AC-013 — failure/dedup/ordering | BE-002, BE-003, BE-005, BE-006, BE-007, BE-008, BE-009, BE-010 | BE-011, BE-012; transaction/crash/retry/concurrency checks, no exactly-once claim. |
| AC-014 — honest evidence classes | BE-007, BE-008, BE-009, BE-010, BE-011, BE-012 | Label live, simulated, manual, synthetic fixture and sandbox separately. |
| AC-015 — provider permission | BE-008, BE-011, BE-012 | Explicit provider/operator/recipient/use/restriction evidence before live enablement and final acceptance. |

## Task execution and completion

Each task is standalone: inspect its dependencies and canonical references; apply its approval gates; implement only scoped components; run relevant edge cases; obtain independent review and risk-relevant audit; record actual outcomes. Use [acceptance and test plan](../validation/acceptance-and-test-plan.md) for test layers and evidence format. Never replace SQLite constraint/transaction tests with EF InMemory, actual login with only a fake authentication handler, or source integration with fixtures alone.

Completion requires the task's criteria, fulfilled prerequisites, an approval reference, actual check outputs, reviewed corrections and an honest limitation record. Record exact application-level prompts and summarized outcomes under `docs/process/`; protect secrets. Scope changes return to user approval. Unrun checks, blocked providers and missing conversation history remain explicit gaps. No deployment, destructive migration, secret change or commit/push is authorized by this roadmap.

## Separately approved future work

These IDs are backlog references, not dependencies of the MVP and not commitments.

| ID | Candidate scope | Entry condition |
|---|---|---|
| FUT-001 | PostgreSQL | Approve provider/migration plan; verify transfer, precision, UTC, constraints and concurrency using the canonical evolution plan. |
| FUT-002 | Real channels/destination verification/OAuth | Approve real delivery use, verified destination lifecycle, credentials, consent and channel contracts. |
| FUT-003 | Cooldown/crossing/digests/richer filters | Approve changed event/repeat semantics and additional persisted state; no accidental expansion of typed MVP predicates. |
| FUT-004 | Distributed worker leases/broker | Demonstrated contention/capacity/reliability need plus approved coordination redesign; database migration alone does not permit multiple writers. |
| FUT-005 | Frontend/tenants | Separate product approval and authorization/data-isolation design. |
