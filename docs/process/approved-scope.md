# Approved scope and authority

## FACT — user-approved requirements

The user selected these options in the clarification questionnaire:

| Topic | Approved choice |
|---|---|
| Categories | News keyword matching; market percentage-movement threshold; earthquake minimum magnitude. |
| Ingestion | Live news RSS and live market API polling, subject to feasibility; authenticated earthquake webhook from a controlled sample sender. |
| Source control | Centrally configured/admin-controlled, not arbitrary user-supplied fetch URLs. |
| Rules | Users create/update/pause/delete their own rules. One category, its supported filter, email and/or Slack. Only newly ingested events; no historical backfill, sharing, digests, or Boolean rule builder. |
| Manual events | Admin-only creation; same downstream validation/matching/delivery pipeline as external events. |
| Identity | Real authentication; pre-provisioned admin/user accounts; single tenant. No registration, password recovery, or user-management API. |
| Admin | Inspect events, source status/ingestion failures and delivery outcomes. No UI and no editing other users' rules. |
| Delivery | Sandbox acceptance: local email capture and fake Slack endpoint are sufficient. |
| Stack | .NET 10, EF Core, SQLite; documented route to PostgreSQL. |
| Deliverables | Backend plans, architecture, ADRs, data model/evolution, roadmap and separate task Markdown files suitable for future AI builds. |
| Roadmap scheduling | Exact user instruction: "Do not add estimations/dates in roadmap. The 24 hours is only for me for the assignment." |

## FACT — repository baseline

At planning start the repository had only `README.md` and `.gitignore`, no application code, and a clean worktree. `git log --oneline -10` returned `2b46a80 Initial commit`. No existing implementation constrains the design.

## Approval boundaries

- User-approved scope above is accepted; technical design written in this session is **proposed**, not silently approved.
- Documentation creation is authorized by: "Okay, build the docs. And have a plan for all the tasks (chunks) in md files. I want to use that as a base of a build session with ai."
- No application implementation, infrastructure operation, migration, deployment, secret changes, commit, or push is part of this session.
- Future task execution requires acceptance of its proposed ADRs/contracts/dependencies. A future explicit instruction to implement a task may approve the specific clearly disclosed choices in that task; otherwise ask before proceeding.
- Reviews can establish design readiness, not user approval or runtime correctness.
