# Prompt provenance and inventory

This folder records application-level user/agent task prompts, not hidden system/developer instructions or private reasoning. It is **not yet a complete conversation export**. Before assignment submission export available conversation/tool history and reconcile it with this index, marking redactions and unavailable items honestly.

## Existing exact records
- [Initial user brief](01-initial-user-request.md): visible text transcription.
- [Discovery and initial attempts](00-discovery-and-first-attempt.md).
- [Requirements retries](02-requirements-agent.md).
- [Architecture recovery](03-architecture-agent.md).
- [Source research agent](04-source-research-agent.md): saved by the research agent.
- [Test strategy](06-test-strategy-agent.md).
- Architecture and subsequent design/review agent task records are saved with their artifacts when available.

## Historical gaps
- Initial clarifier/explorer and first failed/interrupted requirements/architecture task prompts have now been transcribed; full response/tool transcript is still not exported.
- Some resumed tasks ran in read-only subagents; failure to save a prompt does not make the interaction disappear.
- User brief below is a reconstructed summary; the separate initial-user-request file preserves visible original text.
- Final submission must include available full prompt history; do not label this partial curated record complete.

## User instruction history
**Reconstructed summary of initial request:** plan a job-application assignment's backend MVP from a vague world-events alert brief, using AI with visible reasoning/evidence. Email and Slack, extensible channels, admin/user, three alert categories with multiple ingestion methods plus manual admin events. .NET10, EF Core, SQLite with PostgreSQL path. Structured Markdown architecture, ADRs, schema evolution, roadmap and stable chunk IDs; user decides ambiguities. Plan only before later implementation.

**Exact later visible instructions:**
> prompt the questions

Questionnaire choices are recorded in [approved scope](../approved-scope.md), including exact custom answer:
> Do not add estimations/dates in roadmap. The 24 hours is only for me for the assignment.

> Okay, build the docs. And have a plan for all the tasks (chunks) in md files. I want to use that as a base of a build session with ai.

> Is the plan/design fully done?

> keep doing then

## Retention policy for future build sessions
Save each actual task prompt before dispatch where practical, record retries/clarifications separately, keep agent output summaries labelled as summaries, and link checks to observed results. Never reconstruct exact wording from memory and call it verbatim.
