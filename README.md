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

The scaffold serves `/health/live` on its configured loopback URL. No authentication/business endpoints or integrations are implemented in BE-001. See [scaffold evidence](docs/process/build/BE-001.md) and [independent review](docs/validation/BE-001-review.md).

Process evidence and prompt provenance are in [docs/process](docs/process/planning-approach.md). Milestone pushes go directly to `main` as authorized. Documentation/provider probes do not establish working live integrations or completed later chunks.
