# Working log

## Actual sequence
1. Clarifier and repository explorer identified scope decisions and an empty application baseline.
2. User answered six structured questions; answers recorded in [approved scope](approved-scope.md).
3. Lead created planning approach, scope and dual-write routing. Provider researcher wrote source evidence and its exact task prompt.
4. First requirements draft was returned but not saved; architecture delegation was interrupted. User asked whether work was complete; lead explicitly reported it was not.
5. User instructed continuation. Architecture work resumed. Requirements and technical-writer subagents reported missing write tools. This is a subagent-tool limitation, not proof the whole session is read-only.
6. Lead requested a read-only BA draft, reviewed it and saved a condensed requirements document. It preserves scope, stable acceptance IDs, and approval gates without treating proposed semantics as approved.
7. Acceptance strategy saved by test engineer. Lead checked official .NET10 Identity and SQLite-provider documentation, rejecting automatic Identity route mapping and connection-string-only migration claims.
8. Resumed architect hit usage limit. Fresh architect returned a recovery draft; lead persisted architecture, processing flows and six proposed ADRs. Separate reviewers assess the final artifacts; the author's self-assessment is not independent acceptance.
9. Lead transcribed visible initial user and discovery prompts, added approval/submission registers and supplementary vault decisions. Full conversation/tool export remains a submission check, not a claimed completed export.
10. User requested sequential backend implementation and direct-main commits/pushes: documentation snapshot committed/pushed as `7cd3fbf`. Baseline intentionally included disclosed draft conflicts; not represented as fully reconciled design.
11. User approved active recovery baseline, delegated compatible dependency pin selection after license/advisory checks and Windows local sandbox, requesting cursory change-focused reviews. BE-001 delegated to backend developer; separate designer reconciles contracts/security findings before affected chunks. No mandatory review/test gates waived.
12. BE-001 independent code review approved; test engineer reran locked restore/Release build (zero warnings/errors), 19 tests and real-socket loopback/unsafe-override checks. Scaffold committed/pushed as `25c9bb4`. BE-002 delegated next; BE-001 optional coverage improvements retained as comments, not blockers.
13. Contract specialist returned corrected full documents but could not write them. A write-capable general helper is applying those specialist decisions, not inventing replacements; security corrections remain pending independent re-review.
14. User requested faster chunk development and explicitly retained documentation changes. Lead narrowed verification to builds/targeted checks and brief required changes-focused review/audit rather than exhaustive broad suites; no result is assumed without execution.
15. Six contract files reconciled by helper from specialist decisions. Independent security re-review closed S1–S4 at design level; independent consistency audit approved with roadmap-wording corrections. Local link-target check passed after repairing one superseded draft's never-delivered ADR link; whitespace check passed.
16. BE-002 initially built with99 passing tests, but focused code review reproduced a false market boundary match from decimal multiplication rounding. Rejected that shortcut despite green tests and delegated exact scaled-integer comparison plus regression. Pure-domain security check approved its limited scope; does not override correctness finding. No BE-002 commit until fix/re-check.
17. BE-002 correction1 uses BCL BigInteger common-scale coefficients, preserving supported ranges/no new dependencies. Eleven regressions added (three failed before fix); Release build zero warnings/errors,104 unit+6 integration passed. Independent correction review approved, reproduced false boundary correctly and checked coefficient/scale/sign logic. Chunk marked Verified; no later-feature claim.

## Corrections and rejected shortcuts
- **Correction:** earlier lead messages asserted the session was read-only without verified basis. Documentation writes are available and authorized. No application implementation is authorized.
- **Correction:** receiving a long draft is not equivalent to saving an artifact. Check files before claiming completion.
- **Rejected:** public/no-key HTTP access as permission to redistribute provider content. Research found provider rights and geographic-access gates.
- **Rejected:** Coinbase's advertised 24-hour statistic as proof of exact rolling-window boundaries; NASA/Binance are conditional alternatives, not silent approved substitutions.
- **Rejected:** HTTP 200 as proof of a working integration; probes establish reachability/shape only.
- **Rejected:** exactly-once delivery and a live earthquake-feed claim; sandbox receivers and simulated producer are explicit.

## Evidence boundary
No runtime implementation, application tests, migration, deployment, commit or push has occurred. Later review and validation results must be recorded in `../validation/` and `evidence/`. Do not manufacture screenshots, rejection stories or elapsed effort.
