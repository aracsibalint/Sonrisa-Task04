# BE-005 focused review record

## Initial outcomes
- Code review: Changes requested. Minimal-API JSON binding failures occurred before handlers, returning framework-default ProblemDetails without canonical code/traceId/errors/type/detail. Channel availability read selected highest version while write admission accepted any available version, causing contradictory results.
- Security audit: Approved owner/cursor/quota design within static scope. It did not override API contract consistency findings.
- Test verification: Release build and2 focused tests passed. Found malformed/oversize/non-ASCII cursor mapped to ValidationFailed instead of required InvalidCursor; listed broader cursor/quota/replay coverage gaps.

## Correction cycle 1
Add reusable canonical JSON-binding failure handling while preserving413/415/auth behavior; ensure unknown owner/destination fields and malformed JSON produce safe ValidationFailed ProblemDetails. Resolve both read/write channel selection through the same available/unrevoked destination semantics, with newest historical row only explaining unavailable reason. Map malformed cursors to InvalidCursor. Add targeted regressions. Event-history replay/snapshot tests remain BE-006, not falsely marked complete. Cycle1 of maximum3.

## Correction cycle 1 re-review
Security re-review approved. Code re-review found empty or repeated `cursor` query values still passed through generic InvalidQuery/ValidationFailed instead of InvalidCursor; other JSON/channel/cursor fixes were accepted.

## Correction cycle 2
Use cursor-specific single-value validation so empty/repeated cursor parameters return canonical InvalidCursor, while unrelated malformed query parameters retain ValidationFailed. Add exact regressions. Cycle2 of maximum3.

Exact prompts and full reports remain in conversation history.
