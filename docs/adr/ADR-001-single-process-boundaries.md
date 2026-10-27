# ADR-001: One deployable with explicit code boundaries

Status: **PROPOSED — not accepted.**

## Context

**FACT:** approved scope asks for a backend-only .NET 10 application with EF Core/SQLite, three event categories and sandbox delivery. No application implementation existed in the supplied baseline. **UNKNOWN:** production load/availability targets. A service estate would solve an unrequested problem.

## Option A: One API process with BackgroundService

**Pros:** one lifecycle/deployment; local transactions; straightforward debugging; framework hosting and cancellation. Four logical projects/boundaries keep domain behavior testable without distributing it.

**Cons:** shared resources and failure domain; polling/delivery contend with request work. One writer instance limits scale.

**Risk:** an unbounded/faulted worker can harm the API; explicit bounds, independent loops and fatal-health handling are necessary.

## Option B: API plus separately deployed worker

**Pros:** isolate worker resource/failure behavior; independent restarts and scaling.

**Cons:** shared SQLite coordination becomes fragile; deployments, credentials and ownership multiply; database-backed coordination is mandatory.

**Risk:** accidental multiwriter races or network-share SQLite while claiming a local-lock guarantee.

## Option C: Microservices and broker

**Pros:** independent ingestion/matching/delivery scaling and buffered inter-service traffic.

**Cons:** more services, contracts, operations and distributed failure modes; does not remove external duplicate delivery.

**Risk:** over-engineering without demonstrated workload or team-boundary need.

## Recommendation — PROPOSED

Choose A because the fixed, small sandbox scope needs one coherent transaction and no independent scaling requirement has been supplied. Domain owns pure rules; Application owns use cases; Infrastructure owns EF/Identity/adapters; Api owns transport/composition/host lifecycle. One BackgroundService supervises polling and delivery loops; no external network I/O within database transactions.

## Consequences and revisit trigger

No general source plugin framework, mediator or broker. Use three explicit category handlers and narrow ports, not a reusable automation platform. Test worker lifecycle and cancellation; do not capture scoped DbContext in a singleton. Revisit separate workers only after measured contention, required availability isolation or an approved multi-instance requirement. A PostgreSQL move alone is not permission to split deployments.

## Approval / references

Approve single-instance/failure-domain limits and dependency inventory before build. [Overview](../architecture/overview.md) · [ADR-002](ADR-002-atomic-acceptance-and-ordering.md) · [Scope](../process/approved-scope.md)
# SUPERSEDED DRAFT — NOT A BUILD CONTRACT

This interrupted-agent intermediate draft is retained as process evidence only. Its duplicate ADR-001 identifier is historical, not an additional active decision. The active replacement is [ADR-001-single-process.md](ADR-001-single-process.md). If text below conflicts with current architecture or contracts, do not implement it.
