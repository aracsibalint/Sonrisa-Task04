# Starting an AI build session

Use one stable task ID at a time. This template is not blanket permission to implement unapproved architecture or operate infrastructure.

Current authority: the user has explicitly approved sequential implementation of the active recovery baseline and direct-main commit/push after each tested/reviewed chunk. Reviews should be brief and changes-focused. Named dependency pins may be selected after checks; Windows local sandbox is approved. Consult the approval register for remaining provider/nonlocal/destructive-operation gates. Earlier no-commit instructions in this generic template are superseded for these specifically authorized milestone commits, not for arbitrary unrelated operations.

```text
Implement task BE-XXX from docs/roadmap/tasks/ only.

Read docs/README.md, docs/process/approved-scope.md,
docs/plans/backend-implementation-plan.md, docs/roadmap/backend-roadmap.md,
the selected task, its dependencies, referenced ADRs, API and data-model contracts.
Inspect repository instructions and git status. Preserve unrelated changes.

Before coding, list the task's approval gates and unmet prerequisites.
Ask me to accept proposed choices/dependencies/contracts that are not approved.
Do not treat a review verdict or this template as user approval.
Do not enable live sources until provider permissions/access gates are satisfied.

Work only on the selected chunk. Keep requirements/contracts in sync.
Record the exact application-level prompts and actual outcomes in docs/process/.
Use specialist agents; critically assess their output.
Implement -> test -> independent code review -> risk-relevant audit -> fix/re-test/re-review.
Maximum three automatic correction cycles; stop and report remaining blockers.

Use synthetic fixtures and isolated test databases; no secrets in repository/evidence.
Do not deploy, change secrets, run destructive migrations, or commit/push without authorization.
Report changed files, command/results, reviews, unresolved risks and unexecuted tests.
Update task status only when its acceptance criteria are demonstrably satisfied.
Propose a meaningful milestone commit, but do not claim it exists before creating it.
```

## Completion record per chunk
- ID and approval reference; dependencies verified.
- Files changed and behavior delivered.
- Prompts used, agent result summaries, rejected suggestions and why.
- Exact checks with observed results; test counts only from actual output.
- Review/audit verdicts, corrections and scope limits.
- Remaining blockers and next eligible chunk.
- Commit hash if explicitly authorized and actually created.

The assignment requests milestone commits and all prompts. Export the available conversation before final submission; [prompt provenance](prompts/README.md) identifies current gaps. Preserve redactions rather than publishing credentials or hidden platform instructions.
