# ADR-001: Single-process layered application
Status: Accepted for Windows local sandbox — see approval register. Implementation remains unverified.

## Context
Small backend-only sandbox MVP, no existing implementation or scale requirement.
## Decision proposal
One .NET10 deployable with Domain/Application/Infrastructure/Api boundaries and hosted poll/dispatch workers. One process/local SQLite file; second instance refused. Scoped DbContexts; inward dependencies.
## Alternatives considered
- Separate API/workers: independent restart/resources, but interprocess claims/order and SQLite contention before justified scale.
- Microservices/broker: operational complexity with no approved requirement.
## Consequences
Simple local operation, shared failure/resource domain. Explicit bounds avoid pretending this is horizontally scalable. No generic repository/mediator framework by default.
## Approval gate
Accept before BE-001 with named dependency candidates. Documentation is not authorization to install libraries.
## Revisit
Measured sustained contention, independent worker availability or approved multiple instances.
