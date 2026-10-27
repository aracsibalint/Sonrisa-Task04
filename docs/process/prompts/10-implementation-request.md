# Implementation request

Exact latest user instruction (repeated in conversation):

```text
@backend-developer Implement the chunks in the roadmap.

Before that, push the unstaged changes (docs).

After each chunk, push the changes to git.

Use short commit messages, but describe the change itself in the message.

We can push right to the main branch, no need for separate feature branches and PR.
```

## Readiness checks
Lead verified branch main, configured origin, installed SDK 10.0.303 and no application baseline. Explorer and backend developer independently identified draft-contract contradictions and unapproved exact dependency choices. Their actual prompts/results remain in session conversation pending export; no code/restore/install was performed in readiness checks.

## Documentation snapshot
User authorizes saving/pushing the current draft, not claiming it is internally reconciled. Security report remains Changes requested with S1–S4 open. Future technical acceptance and corrections must be recorded before affected implementation. Git whitespace check passed; a narrow textual secret-pattern check found no matches (not a comprehensive secret/security audit).

## Follow-up question/answer record
Lead asked permission to reconcile stale drafts to active six ADRs and implement: one process, atomic event/matching/intents without a Match table, 15-minute opaque bearer, five delivery states, recovery limits, first RSS fetch suppressed and qualifying market snapshots repeated rather than crossings.

Exact user answer:
> approve baseline, but the reviews should be cursory, and check the changes mostly

Lead asked permission to select and pin maintained compatible stable versions after license/advisory checks for SDK10.0.303, Microsoft EFCore/Identity/OpenAPI/testing/dotnet-ef, xUnit v3/test SDK/runner, AngleSharp, MailKit and Mailpit, introduced only when needed.

Selected answer:
> Approve selection (Recommended)

Lead asked to approve Windows-only local sandbox: loopback API/receivers, synthetic accounts, non-network local SQLite and separately persisted current-user DPAPI keys, no Docker requirement; live sources/nonlocal hosting/real delivery separately gated.

Selected answer:
> Approve local sandbox (Recommended)

## Exact BE-001 task prompt
```text
User approvals NOW received: baseline approved with instruction 'the reviews should be cursory, and check the changes mostly'; delegate exact maintained compatible stable dependency pins after license/advisory checks for approved SDK10.0.303/Microsoft EFCore Identity OpenAPI Testing dotnet-ef/xUnit v3 test SDK runner/AngleSharp/MailKit/Mailpit, introducing only as needed; approve Windows-only loopback synthetic sandbox/local SQLite/separate current-user DPAPI keyring, no Docker required. Docs committed/pushed 7cd3fbf; parent handles all git commits/push. IMPLEMENT BE-001 ONLY now. Load relevant skills, read task and architecture. May edit source/config/tests and BE001 evidence docs, but DO NOT change schema/API/security/plan or approval register (parallel designer/parent owns). Pin SDK10.0.303 rollForward disable or explained strict policy, central package versions+lockfiles, net10 four projects inward refs, minimal API host ProblemDetails/liveness loopback, no business endpoints, meaningful unit/host/architecture-reference tests. Verify exact chosen dependency licenses/advisories before restore as possible, record sources/check limitations; no fabricated checks. Only first-chunk test SDK/xunit.v3/runner/Mvc.Testing packages needed, not every deferred dep. Restore/build/test Release actual commands, inspect diagnostics. Keep git clean of generated artifacts/credentials, update ignore as required. Save exact received prompt and actual evidence under docs/process/build/BE-001.md; mark task Implemented awaiting independent review (not Verified yourself). Do not commit/push, do not implement next chunk. Return changed files, tests output summary, dependency versions/licenses/advisory results, issues and review focus.
```
