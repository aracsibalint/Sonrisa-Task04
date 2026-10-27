# Alerts backend — build package

**Status: proposed design; not authorized for implementation until relevant approvals are recorded.** Product scope is agreed. Runtime behavior is unimplemented and untested.

## Reading order
1. [Approved scope](process/approved-scope.md) and [requirements](requirements/alerts-mvp.md).
2. [Architecture](architecture/overview.md), [processing flows](architecture/processing-flows.md), [ADRs](adr/README.md).
3. [Data model and evolution](data/data-model-and-evolution.md), [API contract](api/backend-api-design.md), [security](architecture/security.md).
4. [Implementation plan](plans/backend-implementation-plan.md) and [roadmap](roadmap/backend-roadmap.md).
5. [Acceptance strategy](validation/acceptance-and-test-plan.md), review reports and [AI build-session template](process/ai-build-session.md).

## Authority and status
- User-approved scope overrides restated vague requests. New technical proposals require approval; independent review is not approval by the user.
- ADRs explain why; API/schema/flow documents define the proposed contract; chunk files define implementation scope and acceptance. Contradictions must be resolved before coding, not guessed away.
- Roadmap/task states describe implementation readiness, not whether a Markdown file exists.
- Provider research is evidence, not legal advice or provider approval. See [source feasibility](research/source-feasibility.md).
- No estimates/dates appear in roadmap tasks. Technical intervals/timeouts are runtime behavior, not scheduling estimates.

## Working evidence
- [Planning approach](process/planning-approach.md)
- [Working log and corrections](process/working-log.md)
- [Prompt provenance and history](process/prompts/README.md)
- [Approved scope and authority](process/approved-scope.md)
- [Approval register](process/approval-register.md)
- [Submission checklist](process/submission-checklist.md)

All build artifacts needed by a future session belong in this repository; the knowledge vault is supplementary, not a hidden prerequisite. Prompts currently have disclosed historical export gaps; final assignment submission must reconcile available full conversation history. No application tests or commits are implied by documentation checks.

## Chunk index
| ID | Build scope |
|---|---|
| [BE-001](roadmap/tasks/BE-001.md) | Solution/tooling scaffold |
| [BE-002](roadmap/tasks/BE-002.md) | Domain and matching |
| [BE-003](roadmap/tasks/BE-003.md) | Relational persistence/migrations |
| [BE-004](roadmap/tasks/BE-004.md) | Seeded authentication/authorization |
| [BE-005](roadmap/tasks/BE-005.md) | Owner rules and configured channels |
| [BE-006](roadmap/tasks/BE-006.md) | Atomic event acceptance/manual input |
| [BE-007](roadmap/tasks/BE-007.md) | Simulated earthquake webhook |
| [BE-008](roadmap/tasks/BE-008.md) | RSS and market polling |
| [BE-009](roadmap/tasks/BE-009.md) | Durable sandbox delivery |
| [BE-010](roadmap/tasks/BE-010.md) | Admin inspection/operations |
| [BE-011](roadmap/tasks/BE-011.md) | Adversarial end-to-end verification |
| [BE-012](roadmap/tasks/BE-012.md) | Reproducible demo, CI and submission |

See the roadmap for dependency edges and FUT-001–FUT-005 separately approved future work. All implementation chunks are Not started.
