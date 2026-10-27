> [!NOTE]
> Task is not finished. Current state is documented.


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

The host serves `/health/live`, `/health/ready` and the BE-004 identity foundation on its configured loopback URL. Without configured, migrated persistence, readiness and login are 503; no database is automatically created and protected routes remain unavailable. See [BE-004 evidence](docs/process/build/BE-004.md), [local storage/identity runbook](docs/data/sqlite-local-operations.md) and [persistence evidence](docs/process/build/BE-003.md).

For an explicitly selected **new synthetic sandbox** DB in an existing local NTFS directory, review `docs/data/BE-003-initial.sql`, stop all app instances, then:

```powershell
dotnet tool restore
# Replace with your own absolute local NTFS path; no shared/real database operations are authorized.
dotnet run --project src/Alerts.Api -c Release --no-build -- --migrate 'C:\sandbox\alerts.db'
$env:Persistence__DatabasePath = 'C:\sandbox\alerts.db'
$env:DataProtection__KeyDirectory = 'C:\sandbox-keys\alerts'
$env:DataProtection__ApplicationName = 'Alerts.LocalSandbox'
# Repeat indexes 0..N with stable canonical GUIDs. Values below are placeholders, not credentials.
$env:Identity__Provisioning__Accounts__0__Id = '<stable-lowercase-guid>'
$env:Identity__Provisioning__Accounts__0__Email = '<synthetic-user-email>'
$env:Identity__Provisioning__Accounts__0__Password = '<unique-synthetic-password-from-approved-secret-input>'
$env:Identity__Provisioning__Accounts__0__Role = 'User'
dotnet run --project src/Alerts.Api -c Release --no-build
```

The migration command exits after migration/checkpoint and seeds only stable User/Admin role definitions. Runtime startup requires explicit provisioned accounts and a persistent key directory separate from SQLite; it never supplies or resets a password default. Add a separately indexed Admin account (`Role=Admin`, which receives both roles). The built-in opaque bearer is valid for 15 minutes; the serialized refresh value is discard-only because no refresh route exists. Tooling is model/SQL-only; do not run raw SQL or delete lock/WAL/key files.

Process evidence and prompt provenance are in [docs/process](docs/process/planning-approach.md). Milestone pushes go directly to `main` as authorized. Documentation/provider probes do not establish working live integrations or completed later chunks.
