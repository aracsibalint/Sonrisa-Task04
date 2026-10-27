# Review Report: BE-004 identity

## Verdict: Approved after correction cycle 2

## Findings
### Blocking
1. Initial review: SQLite/storage failures could escape as500. Initial fix became overbroad and mapped schema/programming errors to503. Final correction centralizes primary-code classification and translates only BUSY5, LOCKED6, READONLY8, IOERR10, FULL13, CANTOPEN14. ERROR1, NOMEM7, CORRUPT11, CONSTRAINT19, MISUSE21 and NOTADB26 are not transient storage503 and do not latch it. Final code review: Approved.
2. Initial security review: provisioning only added accounts/roles, so removed/downgraded Admin retained fresh-login access. Correction validates a nonempty unambiguous set with at least one Admin before mutation, atomically disables absent accounts/reconciles exact roles and stamps, preserves rows/passwords, and supports re-add. Security re-review: Approved. Existing issued tokens retain accepted15-minute lifetime.
### Should-fix
Focused test review listed broader malformed-token/key-loss/channel-state/log-redaction cases. Deferred under user-approved fast verification; not represented as passed. No required blocker remained.
### Nit / optional
N/A.

## Verification evidence
Initial full suite158 passed. Final targeted checks: Release build zero warnings/errors; Identity12 and persistence33 pass; real writer lock returns safe503/no token; direct SQLITE_ERROR1 and EF-wrapped CONSTRAINT19 remain untranslated/unlatched; model parity unchanged. Provisioning tests cover downgrade, absent disable, empty/ambiguous/no-admin validation, rollback, re-add and password preservation.

## What was NOT reviewed (scope limits)
No production OIDC/MFA/immediate token revocation, nonlocal key portability, exhaustive malformed bearer/log-redaction/available-channel matrix, load or real credentials. Full prompts/results remain conversation and build evidence. Correction cycle2 of3.
