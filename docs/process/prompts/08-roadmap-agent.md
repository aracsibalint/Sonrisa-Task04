# Roadmap-agent prompt and outcome

## Exact received application-level task prompt

```text
You are a subagent spawned by another session.
Write documentation only in C:\Repos\Sonrisa-Task04. Use patch tool; if unavailable return concise artifact contents. Read docs/process/approved-scope.md, requirements/alerts-mvp.md, planning-approach.md and ai-build-session.md. Own docs/roadmap/backend-roadmap.md and docs/roadmap/tasks/BE-001.md through BE-012.md, plus docs/process/prompts/08-roadmap-agent.md (exact prompt and summary). Architecture/designer running separately; don't invent conflicting contracts, reference canonical architecture/overview.md, processing-flows.md, data/data-model-and-evolution.md, api/backend-api-design.md, architecture/security.md and adr/README.md. Architecture proposes one deployable modular .NET10 app with API/background workers, EFCore SQLite, synchronous bounded event+matching+durable intent transaction, a single in-process write gate; real seeded Identity opaque bearer; source/destination config not CRUD UI; simple typed rules. All design proposed until user approval. Fixed chunk IDs: BE-001 solution/tooling scaffold; BE-002 domain validation and matching; BE-003 relational model/migrations; BE-004 seeded identity/authorization; BE-005 owner rule APIs/provisioned channels; BE-006 atomic event acceptance/manual admin input; BE-007 authenticated simulated earthquake webhook; BE-008 RSS and market adapters/polling; BE-009 durable delivery worker and sandbox email/Slack adapters; BE-010 admin inspection/health/logging; BE-011 adversarial end-to-end/restart verification; BE-012 reproducible demo/handoff/CI/submission evidence. Each standalone md: status Not started (approval/dependency gated), objective, explicit dependencies, related requirements/AC IDs, files/components expected (proposed), approval gates, scoped steps, detailed edge case tests, completion criteria, excluded work, suggested meaningful commit (not performed), AI prompt template referencing shared build-session rules. No numeric effort estimates or calendar dates anywhere in roadmap. Runtime intervals OK but prefer references. Provider rights gates for live BE008 and final acceptance; synthetic adapters can be tested while live blocked but not declared completed integration. Dependencies acyclic: BE001->BE002/003; BE003->004; BE002+003+004->005; BE002+003+004+005->006; BE006->007/008/009; BE004+006+008+009->010; all BE001..010->011;011->012. Domain entities may be defined BE002 before relational BE003 (set BE003 dep002). Note future IDs FUT-001 PostgreSQL, FUT-002 real channels/destination verification/OAuth, FUT-003 cooldown/crossing/digests/richer filters, FUT-004 distributed worker leases/broker only with evidence, FUT-005 frontend/tenants separately approved. Include trace matrix all AC IDs to tasks, per-stage milestones, explicit user approval checkpoint before BE001 dependencies and technical design. Don't implement code or commit.
```

## Exact continuation prompt

```text
The previous response was interrupted. Continue from where you left off without repeating completed content.
```

## Result summary

- Read the required scope, requirements, planning approach and shared build-session rules; consulted architecture overview and source feasibility for consistency. Canonical architecture/design documents are authored separately and linked rather than redefined.
- Subsequently read the parallel processing-flow, data-model, API and ADR-register drafts. Added explicit reconciliation gates for their flagged state/concurrency differences and webhook protocol alignment; included canonical actor-scoped manual dedup and signature edge cases without selecting a competing contract.
- Created `docs/roadmap/backend-roadmap.md` and twelve standalone specifications, `BE-001.md` through `BE-012.md`, using the patch tool. Created this provenance record within the assigned repository scope.
- The roadmap preserves the requested acyclic dependencies, including BE-002 before BE-003; maps all AC-001–AC-015; defines stage exits, explicit user approval before BE-001, separate live-provider/final-acceptance gates and FUT-001–FUT-005.
- Every task starts Not started (approval/dependency gated), lists proposed components and approval gates, scoped steps, detailed edge cases, completion/exclusions, a future commit suggestion and a prompt referencing the shared build-session rules.
- Synthetic BE-008 work can be verified while live rights/access remain blocked, but does not close live integration acceptance or downstream completion prerequisites. No calendar schedule or effort estimate was introduced.
- A tool interruption preceded file creation; the continuation checked that no roadmap files had been written and resumed creation. No application code, tests, migration, provider integration or commit was executed by this documentation task. Documentation checks, if performed, are distinct from runtime verification.
- A read-only documentation check inspected thirteen roadmap files for eleven required task sections, initial status, relative links, AC-ID presence and date/effort patterns. Required sections/status/AC presence and scheduling-pattern checks produced no findings; the then-unwritten canonical `docs/architecture/security.md` was the only missing target, referenced by all thirteen files. Canonical drafts were still being authored independently; link completion and reconciliation require the parent session's final package check.
- After the security draft appeared, read it and added its explicit HTML-normalization approval gate and relevant edge cases. A subsequent read-only check confirmed all roadmap relative links resolve and the exact twelve-task dependency graph matches the requested edges and is acyclic. The earlier missing-link observation is resolved. Final cross-document reconciliation remains the parent session's responsibility; no security/runtime pass is implied.

This record preserves visible application-level prompts and summarizes output; it is not an export of hidden platform instructions or internal reasoning. Technical recommendations remain Proposed until explicit user approval.

## Exact follow-up prompt — recovery architecture alignment

```text
Follow-up: architect failed first but recovery draft is NOW saved by lead in architecture/overview.md, processing-flows.md and six ADRs. Read these final proposed choices and align ONLY your roadmap/tasks files. In particular architecture deliberately has NO separate Match table; intents contain match/rule snapshots, so BE003 references 'match records' and unique event-rule match must not require a new table. All-writer gate including Identity lockout is explicit; delivery states Delivered/RetryScheduled etc and rule Version detailed in forthcoming schema. Remove stale mention of 'earlier flow drafts' unresolved naming if canonical contracts now settle it; otherwise mark exact conflict for lead to resolve, not silently choose. Do not edit schema/API/security. Return residual discrepancies and changes; append exact follow-up prompt to 08 record. No code/tests/commits.
```

## Follow-up outcome

- Read the saved recovery overview, processing flows and all six ADRs linked by the register. Inspected current schema/API/security wording only to identify residual differences; changed only the roadmap/task files and this requested prompt record.
- Removed separate persisted match records, event/rule-only uniqueness, match-insert fault points and relational match fixtures from the task requirements. Matching/rule/filter/version snapshots now explicitly belong to intents; uniqueness is source/event identity and event/rule/channel intent identity. Two selected channels for one event/rule remain valid.
- Aligned tasks to the five named delivery states, interrupted attempt OutcomeUnknown, one dispatcher per channel, all-writer gate including Identity lockout/rehash/provisioning, active-rule creation/activation caps, bounded waiter admission and both outstanding/retained-intent admission. Removed the earlier single-coordinator-class assumption.
- Replaced vague earlier-draft naming warnings with the recovery choices plus a concrete downstream-drift checkpoint. Application-managed Rule Version and commit ordering are complementary; HMAC is selected as proposed, not an unresolved token-versus-signature choice. AngleSharp is the proposed parser, still requiring dependency/behavior approval.
- Dependencies, AC coverage, task status and provider/final-acceptance gates are preserved. No code, tests, migration, provider requests or commits were executed in this follow-up. Reads/searches and documentation edits are not runtime verification.

### Residual discrepancies observed for lead/designer

These observations describe the inspected drafts before their forthcoming revisions; they are not authorizations or alternative task contracts. Recheck after the responsible owner updates them.

| Topic | Recovery proposal | Inspected downstream draft |
|---|---|---|
| Match persistence | No separate match table | Schema RuleMatch table, unique RuleId/EventId and NotificationIntent.MatchId FK remain. |
| Delivery states/outcomes | Pending/InFlight/Delivered/RetryScheduled/Failed; OutcomeUnknown interrupted attempt | Schema/API use Sent and Pending-for-retry; attempt enum uses Ambiguous/Abandoned; security says Sent/Failed. |
| Sender concurrency | One dispatcher/channel | Schema/security require one global send. |
| Bearer expiry | 15-minute access lifetime | API/security use 30 minutes; API example expiresIn is 1800. |
| Rules/fan-out | 100 active total, 20 per owner, at most 200 intents/event | Schema/API/security use 200 undeleted total, 20 undeleted per owner and 400 intents/event. Paused-rule accounting differs. |
| Gate | At most 32 waiters, admission deadline 2 seconds | Schema/API/security retain 5-second gate wait; bounded waiter count is not reflected in those inspected contracts. |
| Intent admission | Outstanding cap 20,000; retained cap 250,000 | Schema/API/security use 10,000 nonterminal cap; retained admission cap is absent. |
| RSS/text | 100 items; 16 KiB normalized news text | Security uses 500 items; schema/API use title 500 characters and summary 4,000 characters. Byte-versus-character accounting must be reconciled explicitly. |
| Send/retry | 10-second timeout; initial retry 30 seconds, exponential/jitter cap 30 minutes; honor longer valid Retry-After | Schema/security use 15-second send timeout and 10s/30s/2m/10m retry sequence plus terminal DeferredTooLong beyond 24 hours. |
| HTML parser | AngleSharp proposed in overview | Security still calls parser selection undecided. |

Only the registered six ADRs were treated as canonical. Similarly numbered older ADR files remain outside this task's edit scope and can still contain obsolete RuleMatch/timeout guidance; the lead should ensure package links do not route readers to them.
