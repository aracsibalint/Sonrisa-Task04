# ADR-006: Durable bounded delivery without exactly-once claims
Status: Accepted for local sandbox delivery — see approval register. Implementation remains unverified.

## Context
Sandbox notifications must survive restart and expose failures without a broker.
## Decision proposal
Transactional immutable intents; Pending/InFlight/Delivered/RetryScheduled/Failed states, persisted attempts and five-attempt budget. One dispatcher/channel; transport outside gate. Retry uncertainty can duplicate receiver effects. Snapshots retain destination version/content, never secret-bearing webhook URLs. Missing destination versions fail instead of redirecting.
## Alternatives considered
- Broker: independent workers/queue tooling but adds service and transactional publication coordination, still no exactly-once receiver guarantee.
- Fire-and-forget: simple but loses durable failure/restart evidence.
## Consequences
Bounded durable best-effort, not guaranteed eventual or exactly-once delivery. Delivered means transport acceptance. Unknown interrupted outcome consumes an attempt and is explicit in admin history. Selected channels progress independently.
## Approval gate
BE-009 retry/classification/state contract and sandbox dependency approval; security review for recipient PII/secrets.
## Revisit
Measured backlog, independent workers, production transports or receiver-supported idempotency requirements.
