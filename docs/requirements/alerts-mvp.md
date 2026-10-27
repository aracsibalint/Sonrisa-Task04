# Problem Definition: Alerts MVP

## Objective
Provide a backend where authenticated users receive email and/or Slack alerts for newly ingested events matching their own rules. This session delivers a plan, not an implemented service.

## Problem
The original brief leaves importance, sources, detection and administration undefined. The agreed MVP replaces subjective importance with three deterministic category filters.

## Users / Actors
- User: pre-provisioned identity; manages owned rules.
- Admin: manual events and operational inspection; cannot edit others' rules.
- Centrally configured live sources: RSS news and market polling.
- Controlled producer: authenticated simulated earthquake webhook.
- Sandbox receivers: local email capture and fake Slack endpoint.

## Business Context
FACT: this is a developer-job assignment emphasizing visible AI direction, judgment, validation and milestone history. Product scope is approved; technical proposals require approval before implementation.

## Desired Outcome
A small backend demonstrable through API requests and deterministic tests, with explicit integration limitations, durable processing semantics, and individually buildable task specifications.

## Functional Requirements
| ID | Approved requirement | Acceptance |
|---|---|---|
| FR-001 | Real authentication; provisioned user/admin roles; single tenant. | AC-001 |
| FR-002 | Owner-only rule create/read/update/pause/resume/delete. | AC-002 |
| FR-003 | One category/filter per rule; email, Slack, or both. | AC-003 |
| FR-004 | News keyword, market percentage movement, earthquake minimum magnitude. | AC-004 |
| FR-005 | Only newly ingested events; no rule-change historical replay. | AC-005 |
| FR-006 | Centrally configured live RSS and market polling, conditional on provider approval. | AC-006 |
| FR-007 | Authenticated simulated earthquake webhook. | AC-007 |
| FR-008 | Admin manual events for all categories; shared downstream validation/matching/delivery. | AC-008 |
| FR-009 | Admin inspection of events, source status/failures and delivery outcomes; no editing others' rules. | AC-009 |
| FR-010 | Selected-channel sandbox delivery. | AC-010 |

## Non-Functional Requirements
| ID | Requirement | Acceptance |
|---|---|---|
| NFR-001 | Enforce authentication, role and ownership boundaries in the API. | AC-011 |
| NFR-002 | .NET 10, EF Core, SQLite; documented PostgreSQL path. | AC-012 |
| NFR-003 | Explicit testable duplicate, interruption and failure semantics; no exactly-once promise. | AC-013 |
| NFR-004 | Distinguish live, simulated, manual, sandbox and fixture evidence. | AC-014 |
| NFR-005 | Resolve provider rights/access before live enablement; never infer rights from HTTP 200. | AC-015 |

No production SLA, capacity, latency or regulatory compliance claim is approved. Proposed local operating limits belong in the design, not invented business commitments.

## Constraints
Backend only; approved stack and scope; no roadmap estimates/dates. Documentation authorization does not authorize code, deployment, migrations, secret changes or git commits/pushes. Technical choices stay Proposed pending acceptance.

## Existing System (facts gathered from the codebase)
Recorded baseline: only README.md and .gitignore, clean worktree, commit `2b46a80 Initial commit`. No prior application implementation. This requirements record relies on the orchestrator's inspection, not an independent runtime audit.

## Scope
### In Scope
The ten functional requirements and planning artifacts, including ADRs, schema evolution, roadmap and per-task AI handoff specifications.
### Out of Scope
Frontend, organizations, registration/recovery/user-management APIs, user-supplied fetch URLs, shared rules, Boolean expression engine, digests, historical backfill, live disaster integration, mandatory real Slack/email delivery, broker/microservices, production deployment and implementing PostgreSQL now.

## Assumptions
INFERENCE: sandbox delivery removes the need for live-channel credentials but does not waive provider licensing. Test identities may be synthetic. No assumptions expand approved scope.

## Open Questions
| Gate | Owner / resolution |
|---|---|
| OQ-001 — BLOCKER for live sources | User/operator approves providers, coverage, currency, jurisdiction, permitted use and attribution. |
| OQ-002 — BLOCKER for matching | User approves proposed keyword/threshold/window/repeat/baseline semantics in architecture and ADRs. |
| OQ-003 — BLOCKER for implementation | User approves technical architecture, identity, schema/contracts and dependencies. Security review is not approval. |

These gate affected implementation, not completion of the design package.

## Acceptance Criteria
All are future tests, not reported passes.

| ID | Observable criterion |
|---|---|
| AC-001 | Seeded user/admin authenticate; invalid credentials fail. |
| AC-002 | User A can manage its rules; user B cannot retrieve or mutate A's rules. |
| AC-003 | Exactly one supported category/filter and at least one available channel required. |
| AC-004 | All categories have match/nonmatch and exact boundary fixtures using approved semantics. |
| AC-005 | Rule create/update/resume does not replay stored events; paused rules do not create new matches. |
| AC-006 | Approved real RSS and market endpoints produce normalized events; users cannot inject fetch URLs. Fixtures alone are insufficient for live acceptance. |
| AC-007 | Valid authenticated sample earthquake accepted; invalid/missing credentials produce no event or notification. |
| AC-008 | Admin submits each category manually; non-admin denied; equivalent normalized data has equivalent matching behavior. |
| AC-009 | Admin can inspect events, source failures/status and delivery outcomes, but cannot edit another user's rules. |
| AC-010 | Matching event reaches only selected sandbox channels; nonmatching event creates no notification. |
| AC-011 | Anonymous, user A, user B and admin tested across authorization boundaries. |
| AC-012 | Build demonstrates .NET10/EFCore/SQLite and persistence across restart; docs specify PostgreSQL migration verification without claiming it ran. |
| AC-013 | Duplicate, interruption/restart, invalid input, source failure, delivery failure and rule-change timing agree with approved contracts. |
| AC-014 | Evidence names executed checks and limitations; simulated and sandbox tests never represented as live production integrations. |
| AC-015 | Live-source approval identifies providers, intended use/recipients and relevant permissions/restrictions. |

## References
- [Approved scope](../process/approved-scope.md)
- [Planning approach](../process/planning-approach.md)
- [Source research](../research/source-feasibility.md)
- [Architecture](../architecture/overview.md)
- [ADR register](../adr/README.md)
- [Roadmap](../roadmap/backend-roadmap.md)
