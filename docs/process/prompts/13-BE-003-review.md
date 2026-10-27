# BE-003 focused review record

## Review scope
Fast changes-only code, security and targeted persistence verification against HEAD `4702b21`; no broad suite or later Identity/business behavior.

## Outcomes
- Code review: **Changes requested**. Emergency `SendResultOrRecovery` allowed existing NotificationIntent claim fields/status to move Pending->InFlight without a DeliveryAttempt under the emergency reserve. SQLite state CHECK did not enforce cross-row attempt creation. Also found a developer-specific absolute test path.
- Focused security audit: Approved within its scope; reviewed lock/gate/disk/secret concerns but did not identify the cross-row transition as a security blocker. It does not override the code-correctness finding.
- Test engineer: targeted locked restore, Release build,17 persistence integration+9 gate/storage tests, model parity, disposable migration/reapply and SQL hash all passed. This demonstrates why green tests alone did not close the missing emergency transition case.

## Correction prompt summary
Backend developer must reject emergency Pending/RetryScheduled->InFlight and any AttemptCount increment/fresh claim. Emergency operations may only finish/recover an already InFlight claim using existing attempt/token, with coherent terminal/retry/OutcomeUnknown updates. Add regression tests and remove the user-specific absolute test directory. This is correction cycle1 of maximum3.

Full exact agent prompts and reports remain in available conversation history. Final review report will preserve this initial finding rather than rewriting history.
