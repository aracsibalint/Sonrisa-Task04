# Approval register

This records authority, not design-review verdicts. The user has now approved the active recovery baseline for implementation, dependency version selection after checks, and the Windows local sandbox. Live-provider permissions and nonlocal operation remain separately gated.

| Gate | Current status | Decision required | Blocks |
|---|---|---|---|
| G-000 Product scope | Accepted by user questionnaire | See approved-scope.md; no estimates/dates in roadmap. | None for drafting |
| G-001 Architecture/dependencies | Accepted baseline; exact compatible pins delegated | Active ADR-001/002/003/006 and recovery limits; reconcile stale drafts. Check named library versions/licenses/advisories and introduce only when needed; new dependency families/material departures still require approval. | No baseline approval blocker; reconcile affected contracts before implementation |
| G-002 Identity/security | Accepted for Windows local sandbox | ADR-004; login-only opaque bearer, 15-minute expiry, unused refresh field, no instant revocation; local DPAPI key ring separate from SQLite; HMAC producer. No real secrets/production changes authorized. | Implementation checks/reviews still required |
| G-003 Matching semantics | Accepted recovery baseline | ADR-005 substring/inclusive per-snapshot market repeat/Mw, RSS suppressed first fetch and immutable pending intents. | No semantics approval blocker |
| G-004 Provider choice | Proposed | NASA's space/science coverage; Binance BTCUSDT/USDT denomination and rolling24h window, or explicit alternatives with revised contracts. | Final provider-specific adapter contract |
| G-005 Live-source permissions | Unresolved operator/user gate | Record applicable permitted use/recipients/territory, terms and attribution. Public endpoint access does not grant these rights. | Live enablement and AC-006/015; not isolated synthetic testing |
| G-006 Milestone commits | Accepted by latest user instruction | Commit/push current docs first, then tested/reviewed chunks directly to main with short descriptive messages; no feature branches/PRs. | No git authorization blocker; technical gates remain separate |

## How to record a decision
For each gate append the user's exact approval/change instruction, affected ADR/task IDs and scope. Set ADRs Accepted only for choices actually approved. Resolve alternative provider/behavior choices across schema/API/tests before implementation. Do not accept requirements on the user's behalf.

## Latest execution authorization
The user now requests implementation by the backend-developer agent and explicitly authorizes the documentation commit/push followed by a commit/push after each chunk directly to `main`. Earlier documentation-only/no-commit statements are historical session scope, superseded for these operations. The contradictory technical drafts and unspecified dependencies still require reconciliation/approval before code; this is not provider legal clearance or production/destructive-operation authorization.

## Follow-up approvals
- Exact baseline response: **"approve baseline, but the reviews should be cursory, and check the changes mostly"**. The question disclosed one process, atomic event/matching/intents without a Match table, 15-minute opaque bearer, five delivery states, recovery limits, RSS suppressed first fetch and repeated market snapshots rather than crossings.
- Dependency response: **"Approve selection (Recommended)"**. Authorized maintained compatible stable pins after license/advisory checks for SDK10.0.303, Microsoft EFCore/Identity/OpenAPI/testing/dotnet-ef, xUnit v3/test SDK/runner, AngleSharp, MailKit and Mailpit, introduced as needed. Stop for material incompatibility/license/security problems or new dependencies.
- Runtime response: **"Approve local sandbox (Recommended)"**. Windows-only loopback API/receivers, synthetic accounts, local non-network SQLite and separately persisted current-user DPAPI-protected keys, no Docker requirement. No nonlocal deployment, live-provider rights clearance, real-secret provisioning or destructive operations inferred.
- Reviews are focused on actual changes and kept brief; mandatory tests/code review/risk-relevant security audit still apply. Baseline approval is not a passed runtime test.
- Later user requested development/fast verification over exhaustive tests/reviews, then explicitly clarified **"But not skip the documentation changes"**. Keep build and targeted checks plus brief required changes-focused reviews; preserve per-chunk status/contracts/prompts/decisions/results. Do not fabricate or silently waive verification evidence.

## Suggested review reply
The user may approve G-001 through G-004 together or change individual proposals. G-005 requires real operator assessment rather than a fabricated permission claim. Even after technical approval, tasks with unmet prerequisites or mandatory failed checks remain blocked.
