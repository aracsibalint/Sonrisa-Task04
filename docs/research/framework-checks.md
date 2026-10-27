# Framework checks — documentation evidence only

## Identity API surface

Official reference consulted by the lead using read-only web fetch:
<https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0>

FACT: Microsoft documents proprietary opaque bearer tokens, not JWTs, as an option for simple non-cookie clients. Token-based authentication is not a full OAuth/OIDC identity service. The framework supports EF-backed Identity and configurable bearer-token expiration.

FACT: `MapIdentityApi<TUser>()` maps registration, refresh, confirmation, password-recovery and management routes in addition to login. Blindly mapping it conflicts with the approved seeded-account-only scope. Service registration is not the same as route mapping.

FACT: access-token expiration bounds the lifetime of already-issued token sessions after security-sensitive changes; do not claim instantaneous logout or role revocation without an explicit extra mechanism.

Design implication for review: use a deliberately limited framework-backed login endpoint if the opaque-bearer proposal is accepted. Test the actual endpoint inventory; do not implement new crypto or password hashing. Persist/protect Data Protection keys deliberately. Excluding refresh must be explicit even if a framework sign-in response includes a refresh-token field.

Limit: no SDK/package restore, auth prototype or API integration test was executed. Verify exact .NET10 APIs and chosen package patch versions during the scaffold/auth chunks. Documentation examples using EF InMemory are not a reason to replace relational SQLite persistence tests.

## EF Core SQLite portability

Official reference consulted through read-only web fetch:
<https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations>

FACT: SQLite provider lacks schemas, sequences and database-generated concurrency tokens. Comparison/ordering of decimal and DateTimeOffset values has provider limitations; read/write capability alone does not establish equivalent query behavior. Some schema changes require table rebuilds. SQLite cannot generate the same procedural idempotent migration scripts supported by other databases. EF migration locking can leave an abandoned lock after interruption.

Design-check implications: specify explicit UTC storage and portable concurrency semantics; do not casually convert market prices to floating point; test actual query translation and relational constraints. Generate migration SQL for known migration ranges and inspect it. A PostgreSQL switch is not merely changing a connection string. Do not run destructive lock cleanup based on this note: diagnosis, backup and explicit approval remain mandatory.

Limit: documentation checked, not actual generated SQL or provider parity. No database was created or modified.
