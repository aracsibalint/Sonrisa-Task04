# Approval register

This records authority, not design-review verdicts. **No new technical proposal below is accepted yet.** User instruction to finish documentation authorizes drafting and review, not application implementation.

| Gate | Current status | Decision required | Blocks |
|---|---|---|---|
| G-000 Product scope | Accepted by user questionnaire | See approved-scope.md; no estimates/dates in roadmap. | None for drafting |
| G-001 Architecture/dependencies | Proposed | ADR-001/002/003/006, operating limits, schema/API contracts; approve named package candidates and pin versions at build. | Affected build chunks, beginning BE-001 |
| G-002 Identity/security | Proposed | ADR-004; login-only opaque bearer, 15-minute expiry, unused refresh field, no instant revocation, HMAC producer contract, secret/key storage. | BE-004 and BE-007 |
| G-003 Matching semantics | Proposed | ADR-005; news substring, absolute inclusive market threshold per snapshot (repeat possible every minute), Mw minimum; suppressed RSS baseline, future-only rule changes with pending intents retained. | BE-002 and downstream matching |
| G-004 Provider choice | Proposed | NASA's space/science coverage; Binance BTCUSDT/USDT denomination and rolling24h window, or explicit alternatives with revised contracts. | Final provider-specific adapter contract |
| G-005 Live-source permissions | Unresolved operator/user gate | Record applicable permitted use/recipients/territory, terms and attribution. Public endpoint access does not grant these rights. | Live enablement and AC-006/015; not isolated synthetic testing |
| G-006 Milestone commits | Accepted by latest user instruction | Commit/push current docs first, then tested/reviewed chunks directly to main with short descriptive messages; no feature branches/PRs. | No git authorization blocker; technical gates remain separate |

## How to record a decision
For each gate append the user's exact approval/change instruction, affected ADR/task IDs and scope. Set ADRs Accepted only for choices actually approved. Resolve alternative provider/behavior choices across schema/API/tests before implementation. Do not accept requirements on the user's behalf.

## Latest execution authorization
The user now requests implementation by the backend-developer agent and explicitly authorizes the documentation commit/push followed by a commit/push after each chunk directly to `main`. Earlier documentation-only/no-commit statements are historical session scope, superseded for these operations. The contradictory technical drafts and unspecified dependencies still require reconciliation/approval before code; this is not provider legal clearance or production/destructive-operation authorization.

## Suggested review reply
The user may approve G-001 through G-004 together or change individual proposals. G-005 requires real operator assessment rather than a fabricated permission claim. Even after technical approval, tasks with unmet prerequisites or mandatory failed checks remain blocked.
