# ADR-003: Durable notification intents and bounded delivery

Status: **PROPOSED — not accepted.**

## Context

**FACT:** email and/or Slack are required; sandbox capture/fake endpoint suffice. External transports do not participate in the SQLite transaction. A crash between receiver acceptance and database completion is inherently ambiguous.

## Option A: Durable database intents with local dispatch

**Pros:** survives process restart; atomic with event acceptance; independent channel outcomes; no broker dependency.

**Cons:** scan/claim/recovery state is needed; SQLite and one dispatcher limit throughput; retries can duplicate.

**Risk:** falsely describing logical intent dedup as exactly-once external delivery.

## Option B: Inline send or memory-only queue

**Pros:** minimal state and immediate attempt.

**Cons:** receiver latency affects API transactions; rollback cannot retract a message; process loss drops queued work.

**Risk:** under-engineering: accepted events can have no recoverable notification obligation.

## Option C: Broker-backed dispatch

**Pros:** mature delivery-work transport and scaling options.

**Cons:** still needs atomic database-to-broker handoff/outbox; adds service operations; external-send ambiguity remains.

**Risk:** paying for two coordination problems rather than solving the bounded one.

## Recommendation — PROPOSED

Choose A: NotificationIntent itself is the small durable outbox, not a separate generic messaging subsystem. Persist immutable payload/recipient/filter snapshots at acceptance. A single dispatcher claims due work in a gated transaction, commits, performs external I/O outside the gate, and records completion in another gated transaction.

Use Pending/InFlight/RetryScheduled/Delivered/Failed, persisted ordinal/next-attempt and a random claim token with lease. Five total attempts, 15-second external timeout and 2-minute lease; recover expired claims without resetting consumed attempts. Detailed retry/Retry-After rules and in-process lease safeguards are canonical in [processing flows](../architecture/processing-flows.md). SMTP/fake-Slack acknowledgments mean receiver acceptance, not human receipt.

## Consequences and guarantee

At-least-once-style attempts can duplicate receiver acceptance; bounded retries can finish Failed and do not guarantee eventual delivery. Crash before I/O may consume an attempt without a send. Crash after success before completion may resend. Stable intent IDs aid diagnosis/optional receiver dedup only. Terminal Failed does not prove nonreceipt when prior results were ambiguous.

Rule pause/delete does not cancel committed work; destination changes do not reroute it. Old destination revoked/unavailable fails visibly. Every selected channel has independent intent/attempt history. No automatic replay/dead-letter service or admin resend endpoint in scope. Backlog admission and telemetry are mandatory; production delivery SLA is not claimed.

## Approval / references

Approve duplicate risk, retry bounds, terminal failure and snapshots. [Overview](../architecture/overview.md) · [ADR-002](ADR-002-atomic-acceptance-and-ordering.md) · [ADR-004](ADR-004-provisioned-identity-and-trust.md)
# SUPERSEDED DRAFT — NOT A BUILD CONTRACT

This interrupted-agent intermediate draft is retained as process evidence only. Its historical ADR-003 identifier was reused by a different active decision. Its delivery topic is replaced by [ADR-006-durable-delivery.md](ADR-006-durable-delivery.md); active ADR-003 is [portable persistence](ADR-003-portable-persistence.md). If text below conflicts with current architecture or contracts, do not implement it.
