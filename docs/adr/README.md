# Architecture decision register

The user has **accepted the active recovery baseline for the Windows local sandbox**, including derived architecture and matching semantics. See [approval register](../process/approval-register.md). Provider selection/rights and nonlocal operation remain separately gated. Implementation tests/reviews remain mandatory; accepted design is not verified runtime behavior.

| ID | Proposal | Gate |
|---|---|---|
| [ADR-001](ADR-001-single-process.md) | Single layered API/worker process | BE-001 |
| [ADR-002](ADR-002-atomic-acceptance.md) | Atomic event/matching/intents and all-writer ordering | BE-003, BE-006 |
| [ADR-003](ADR-003-portable-persistence.md) | Exact portable representation and provider evolution | BE-003 |
| [ADR-004](ADR-004-seeded-authentication.md) | Provisioned Identity opaque bearer/login only | BE-004 |
| [ADR-005](ADR-005-source-and-rule-semantics.md) | Explicit providers, filters and first-ingestion semantics | BE-002, BE-007, BE-008 |
| [ADR-006](ADR-006-durable-delivery.md) | Durable bounded dispatch, honest uncertainty | BE-009 |

Exact maintained compatible pins for the named dependency families are delegated after license/advisory checks and recorded at introduction. New dependency families/material changes require approval. Acceptance comes from the explicit user response in the register, not this document's existence.

## Superseded intermediate drafts
The interrupted initial architect wrote partial artifacts before hitting its limit; these were discovered during review, correcting the earlier unconfirmed assumption that none were saved. They are retained for process evidence with prominent superseded banners, not active ADRs:
- [Old boundaries draft](ADR-001-single-process-boundaries.md)
- [Old ordering draft](ADR-002-atomic-acceptance-and-ordering.md)
- [Old delivery draft](ADR-003-durable-delivery.md)

Only the six records in the active table govern the proposal. Do not treat duplicate historical identifiers as multiple accepted decisions.
