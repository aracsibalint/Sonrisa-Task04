# ADR-002: Atomic acceptance and single-instance mutation ordering

Status: **PROPOSED — not accepted.**

## Context

**FACT:** only newly ingested events match; users can change/pause/delete their own rules. **UNKNOWN until approved:** precise ordering when rule changes race events. Durable acceptance without durable notification intent would silently lose notifications on restart.

## Option A: Event + matching + intents in one bounded transaction

**Pros:** no unmatched accepted-event state or separate recovery phase; SQLite commits define one clear rule version; short pure matching is easy to test.

**Cons:** event acceptance cost grows with active rules/channels; requires enforced limits and a shared mutation gate.

**Risk:** slow matching or fan-out makes the critical section too long if bounds are not enforced.

## Option B: Persist event, then asynchronously match

**Pros:** faster ingress and independently throttled matching.

**Cons:** needs durable unprocessed state, leases/recovery and a defined historical rule snapshot. Evaluating current rules later can violate future-only semantics around pause/edit.

**Risk:** lost matching work or wrong rule-version selection; extra protocol for no current requirement.

## Option C: Rely only on database contention and timestamps

**Pros:** no application mutation coordinator.

**Cons:** timestamp order does not serialize rule reads against insertion; SQLite busy handling and upgrades become the implicit contract.

**Risk:** nondeterministic behavior when simultaneous acceptance and rule mutation overlap; difficult race testing.

## Recommendation — PROPOSED

Choose A with one singleton asynchronous mutation gate because bounded three-predicate matching is local and SQLite is explicitly single-instance. Serialize event/rule/source/worker-state and Identity persistence mutations. Keep source requests, sender calls and other external I/O outside the gate/transaction. Begin short operation-scoped transactions after acquiring the gate; commit before releasing it.

The successful commit is the ordering point. A paused/deleted/edited rule affects only events accepted after that mutation. Snapshot rule version/filter, positive outcome, recipient identity and rendered notification body when accepting an event. Previously committed intents remain unchanged. No rematch on create/resume or duplicate ingestion.

Enforce Event `(SourceId, ExternalKey)`, RuleMatch `(RuleId, EventId)` and Intent `(RuleId, EventId, Channel)` uniqueness in the database, not only in code. Insert all event/match/intent rows atomically; zero matches still records successful evaluation. Enforce rule/fan-out/backlog bounds before commit; overflow fails whole acceptance rather than dropping recipients.

## Consequences and revisit trigger

The gate is neither cross-process nor a substitute for unique indexes. Reads remain ungated committed reads. Timeout is retryable, not accepted work. Soft rule deletion/history retention avoids deleting intent evidence. Actual contention benchmarks and race/rollback tests are required. Revisit separate matching only if measured bounded evaluation cannot meet an approved latency/load target; that change requires rule-version snapshot and durable-work design, not a queue added alone.

## Approval / references

Approve commit ordering, bounded rules and snapshot/pause semantics. [Processing contract](../architecture/processing-flows.md) · [Overview limits](../architecture/overview.md) · [ADR-003](ADR-003-durable-delivery.md)
# SUPERSEDED DRAFT — NOT A BUILD CONTRACT

This interrupted-agent intermediate draft is retained as process evidence only. Its duplicate ADR-002 identifier is historical, not an additional active decision. The active replacement is [ADR-002-atomic-acceptance.md](ADR-002-atomic-acceptance.md). If text below conflicts with current architecture or contracts, do not implement it.
