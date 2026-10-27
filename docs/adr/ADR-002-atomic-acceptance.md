# ADR-002: Atomic ingestion, matching and notification intents
Status: Accepted for Windows local sandbox — see approval register. Implementation remains unverified.

## Context
New-event semantics and rule-edit races must be deterministic; accepted events must not lose queued notifications on restart.
## Decision proposal
Synchronous bounded event/matching/complete intent set in one short DB transaction, serialized with all other mutations by one bounded async gate. No external IO under gate. Unique source identity and event/rule/channel keys. Rule changes affect future event commits; committed snapshots persist after pause/delete.
## Alternatives considered
- Async matching: shorter ingress tx but requires durable matching state and rule snapshots/history to avoid evaluating changed rules.
- Inline sending/in-memory queue: loses atomic durability and couples upstream latency.
## Consequences
Maximum fan-out and explicit capacity rejection required; every writer including Identity lockout joins ordering. No distributed safety implied. RSS whole-poll validator checkpoint only advances after item processing.
## Approval gate
BE-003/BE-006; approve ordering, rule lifecycle and queue bounds. Test rollback, races and retries against real SQLite.
## Revisit
Measured acceptance latency exceeds agreed needs or larger fan-out/async matching is approved.
