# Sonrisa-Task04 — Alerts backend MVP

Backend MVP for a software-development assignment. **Implementation in progress:** the layered API scaffold is implemented and independently verified; business features are not yet complete.

Start with [the documentation index](docs/README.md), then the [roadmap](docs/roadmap/backend-roadmap.md) and [AI build-session template](docs/process/ai-build-session.md).

- Agreed stack: .NET 10, EF Core, SQLite; PostgreSQL evolution documented.
- Categories: news, market movements and earthquakes; admin manual input.
- Scope: real RSS/market ingestion subject to provider approval; simulated earthquake webhook; sandbox email/Slack; seeded user/admin roles.
- The active architecture baseline is accepted for a Windows-only local sandbox. Live-provider rights/selection and nonlocal deployment remain gated.
- Roadmap contains stable task IDs, not estimates or delivery dates.

## Build and test

Requires pinned .NET SDK **10.0.303** on Windows. From the repository root:

```powershell
dotnet restore Alerts.slnx --locked-mode
dotnet build Alerts.slnx --configuration Release --no-restore
dotnet test --solution Alerts.slnx --configuration Release --no-build --no-restore
dotnet run --project src/Alerts.Api --configuration Release --no-build
```

The host serves `/health/live` and `/health/ready` on its configured loopback URL. Without configured, migrated persistence, readiness is 503; no database is automatically created and no workers or business/authentication endpoints start. BE-003 persistence is implemented **awaiting independent review**. See [persistence evidence](docs/process/build/BE-003.md), [local storage runbook](docs/data/sqlite-local-operations.md), [scaffold evidence](docs/process/build/BE-001.md) and [scaffold review](docs/validation/BE-001-review.md).

For an explicitly selected **new synthetic sandbox** DB in an existing local NTFS directory, review `docs/data/BE-003-initial.sql`, stop all app instances, then:

```powershell
dotnet tool restore
# Replace with your own absolute local NTFS path; no shared/real database operations are authorized.
dotnet run --project src/Alerts.Api -c Release --no-build -- --migrate 'C:\sandbox\alerts.db'
$env:Persistence__DatabasePath = 'C:\sandbox\alerts.db'
dotnet run --project src/Alerts.Api -c Release --no-build
```

The migration command exits after migration/checkpoint, holds exclusive sidecar ownership throughout, and seeds only the User/Admin role definitions (no accounts/passwords). Tooling is model/SQL-only; `dotnet ef database update/drop` is deliberately refused. Do not run raw SQL against an application DB or delete lock/WAL files. Account provisioning and Identity runtime write integration belong to BE-004.

Process evidence and prompt provenance are in [docs/process](docs/process/planning-approach.md). Milestone pushes go directly to `main` as authorized. Documentation/provider probes do not establish working live integrations or completed later chunks.
