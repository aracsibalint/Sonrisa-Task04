# ADR-003: SQLite with explicit PostgreSQL evolution
Status: Accepted for Windows local sandbox — see approval register. PostgreSQL implementation remains deferred.

## Context
Portable initial database, exact market arithmetic, later PostgreSQL support without assuming provider equivalence.
## Decision proposal
One scoped context type including Identity. Epoch-ms integer timestamps; canonical invariant decimal text; bounded application decimal matching, no numeric SQL text sorting. Application-managed concurrency version. Separate provider migrations later.
## Alternatives considered
- Scaled integers: exact SQL ranges but require category-specific precision/range decisions and overflow handling.
- Floating point: convenient queries but unacceptable silent threshold precision changes.
- Postgres now: unnecessary service beyond approved SQLite scope.
## Consequences
Explicit query/index/constraint tests. Migration is reviewed export/import plus provider migrations, not connection-string replacement. Keys/secrets are separate portability concerns.
## Approval gate
BE-003 schema/conversion/migration review. Destructive operations need separate explicit approval.
## Revisit
Large matching sets/server-side numeric queries or PostgreSQL implementation approval.
