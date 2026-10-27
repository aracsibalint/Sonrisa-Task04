# BE-004 focused review record

## Initial outcomes
- Code review: Changes requested. Storage-class SQLite write/busy/checkpoint failures could bypass fixed unavailable mapping and return500 instead of503.
- Security audit: Changes requested. Provisioning only added roles/accounts; removing an account or downgrading Admin in configuration did not revoke fresh-login privileges.
- Test verification: focused restore/build7 Identity tests/model check passed; identified additional optional coverage gaps. Green tests did not include the two blocking scenarios.

## Correction decision and scope
Correction cycle1 makes an **explicit provisioning operation** declarative and authoritative: validate a nonempty unambiguous set with at least one Admin before mutation, then disable absent accounts and reconcile exact roles in one admitted transaction. It never deletes historical user rows or resets existing passwords; empty/missing configuration fails before disabling anyone. Existing issued tokens retain the accepted15-minute lifetime; fresh login observes reconciled state.

Storage-class persistence failures are mapped to safe503 StorageUnavailable; unrelated programming failures must not be hidden. Add fault/regression tests for both findings. Full prompts/reports are retained in conversation history. Cycle1 of maximum3.

## Correction cycle 1 re-review
Security reviewer approved provisioning reconciliation and token-after-commit behavior. Code reviewer found the SQLite filter still overbroad: every non-constraint code, including `SQLITE_ERROR` schema/programming faults, became503 and latched storage unavailable.

## Correction cycle 2
Narrow translation/latching to explicitly documented storage availability primary codes (busy/locked/readonly/I/O/full/cannot-open, with any additional code justified). `SQLITE_ERROR`, corruption/not-a-database, misuse and constraints must not be hidden as transient storage503. Use the same predicate through wrapped EF exceptions. Keep real-lock503 regression and add nontranslation test. Cycle2 of maximum3.
