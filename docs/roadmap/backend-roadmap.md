# Backend roadmap

Status: **Baseline approved for Windows local implementation.** BE-001 implementation has been delegated; per-task evidence governs completion. Other chunks remain Not started, dependency/provider gated as applicable. This roadmap defines order and evidence, not a schedule. Documentation snapshot commit/push: `7cd3fbf`; no implementation milestone has yet been verified.

## Authority and canonical contracts

[Approved scope](../process/approved-scope.md) and [requirements](../requirements/alerts-mvp.md) define the product. Read [planning approach](../process/planning-approach.md) and [shared AI build-session rules](../process/ai-build-session.md) before executing any chunk.

Technical proposals are owned by [architecture overview](../architecture/overview.md), [processing flows](../architecture/processing-flows.md), [data model and evolution](../data/data-model-and-evolution.md), [backend API](../api/backend-api-design.md), [security](../architecture/security.md) and [ADR register](../adr/README.md). Task paths/components are proposed working locations, not new API/schema contracts. Reconcile disagreements in those canonical documents and obtain approval before coding.

The proposed shape is one deployable modular .NET 10 application with API and in-process background workers, EF Core SQLite, bounded synchronous event/matching/durable-intent acceptance in one transaction, and one in-process mutation gate covering every writer, including Identity lockout/provisioning. Matching is evaluated in memory; intents contain immutable match/rule/filter/version snapshots. There is no separate Match/RuleMatch table or matching queue. Real seeded Identity opaque bearer authentication, centrally provisioned sources/destinations and simple typed rules are design proposals. Nothing here authorizes another deployment unit, a source/destination CRUD API or a generalized rules engine.

## User approval checkpoint — before BE-001

Resolved: user accepted the active recovery baseline, matching/repeat/baseline semantics and Windows local sandbox, and delegated maintained compatible exact pins for the named dependencies after license/advisory checks. See approval register. New dependency families/material departures still require approval; this is not provider permission or production authorization.

The accepted recovery baseline uses intent-contained matching snapshots; `Pending/InFlight/Delivered/RetryScheduled/Failed`; one dispatcher per channel; application-managed rule Version; HMAC earthquake requests; approved AngleSharp family when parsing is needed. Commit ordering and optimistic versions are complementary. Use the active ADR register, not superseded drafts.

### Contract reconciliation — resolved

The six canonical design files are aligned to the accepted baseline. Independent changes-focused documentation audit found no blocking contract contradiction; security re-review closed S1–S4 at design level. Token lifetime, no-match-table model, five states, active quotas, waiter/backlog/retained caps, RSS/text bounds, retry/dispatch and approved parser-family choices are reconciled. See the reconciliation prompt record and review reports. Windows lock/Identity/disk headroom still need implementation evidence, not renewed approval of the same baseline.

Provider selection, rights, recipients, attribution and regional eligibility are a separate gate. [Source feasibility](../research/source-feasibility.md) proposes candidates; HTTP success does not establish permission. Synthetic BE-008 work can proceed after technical approval while live use is blocked. It cannot close AC-006/AC-015 or be reported as completed live integration. Final MVP acceptance remains blocked until the approved real RSS and market flows are demonstrated.

## Dependency-ordered chunks

Every dependency below is a prerequisite for claiming completion; approval gates apply in addition. BE-002 defines domain types before BE-003 maps them. The graph is acyclic.

| Task | Scope | Explicit dependencies | Status |
|---|---|---|---|
| [BE-001](tasks/BE-001.md) | Solution/tooling scaffold | User technical/dependency approval checkpoint | Verified; committed/pushed 25c9bb4 |
| [BE-002](tasks/BE-002.md) | Domain validation and matching | BE-001 | Verified; committed/pushed 4702b21 |
| [BE-003](tasks/BE-003.md) | Relational model/migrations | BE-001, BE-002 | Verified after emergency-admission correction |
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

Completion requires the task's criteria, fulfilled prerequisites, approval reference, actual check outputs, reviewed corrections and limitations. Keep prompts/docs current; use fast targeted checks and brief changes-focused review. User explicitly authorizes short descriptive milestone commits/pushes directly to main after each completed chunk. No deployment, destructive migration or real-secret operation is inferred. Unrun checks/provider/history gaps remain explicit.

## Separately approved future work

These IDs are backlog references, not dependencies of the MVP and not commitments.

| ID | Candidate scope | Entry condition |
|---|---|---|
| FUT-001 | PostgreSQL | Approve provider/migration plan; verify transfer, precision, UTC, constraints and concurrency using the canonical evolution plan. |
| FUT-002 | Real channels/destination verification/OAuth | Approve real delivery use, verified destination lifecycle, credentials, consent and channel contracts. |
| FUT-003 | Cooldown/crossing/digests/richer filters | Approve changed event/repeat semantics and additional persisted state; no accidental expansion of typed MVP predicates. |
| FUT-004 | Distributed worker leases/broker | Demonstrated contention/capacity/reliability need plus approved coordination redesign; database migration alone does not permit multiple writers. |
| FUT-005 | Frontend/tenants | Separate product approval and authorization/data-isolation design. |
