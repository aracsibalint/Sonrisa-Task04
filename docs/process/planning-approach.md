# Planning approach

Status: In progress — documentation only; no application implementation authorized by this session.

## Objective

Turn the agreed MVP scope into a reviewable, self-contained backend build package. The deliverable is a plan and task specifications, not working application code. The assignment evaluates reasoning and validation; claims of execution must have evidence.

## Ordered work

1. Record the user's approved scope and inspect the repository.
2. Independently investigate provider feasibility while drafting the architecture and requirements.
3. Reconcile provider findings with contracts, processing semantics, security, schema, and proposed ADRs.
4. Produce an implementation plan, dependency-ordered roadmap, and one specification per stable task ID.
5. Independently review architecture/completeness and security. Record findings, make corrections, and re-review blocking findings (maximum three correction cycles).
6. Check links, task coverage and agreement between artifacts; produce the AI build-session handoff and honest evidence log.

## Direction and judgment

- Approved product constraints are not permission to silently approve new architecture, authentication, persistence, provider, or dependency choices. Mark these **Proposed**, with explicit build gates.
- Reject scope inflation: no frontend, organizations, public registration, general expression engine, microservices, broker, or production-grade reliability claims without a new requirement.
- Define market comparison semantics, new-event semantics, deduplication, failure recovery, and delivery guarantees precisely enough to test.
- Sandbox delivery and a simulated earthquake producer must never be represented as verified live integrations.
- Do not invent command results, screenshots, prior prompts, approval, or rejected AI output.
- No roadmap dates, durations, effort estimates, or inferred assignment deadline. The user's personal assignment timebox is outside this plan.
- No git commit or push in this planning session; provide milestone suggestions for later explicit use.

## Agents and responsibilities

- Business analyst: approved requirements and observable acceptance criteria.
- Architect: proposed structure, alternatives, ADRs and boundaries.
- Research agent: upstream provider documentation and unresolved feasibility constraints.
- Solution designer: API/schema/flows and executable chunk specifications against the proposed architecture.
- Independent auditor and security auditor: read-only review, not self-approval.
- Engineering lead: reconcile reports, enforce approval gates and verify evidence/link completeness.

## Evidence rules

Store session/task prompts in `prompts/`, review results in `../validation/`, and operational checks in `evidence/`. Prompt history is the application-level user/agent task record available in this session, not an export of hidden system instructions or internal reasoning. If an earlier item is reconstructed or an output is summarized, say so explicitly. Never store credentials or private delivery URLs.
