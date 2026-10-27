# Submission and build-session checklist

## Planning handoff
- [ ] User accepts or amends proposed architecture, contracts, matching semantics and dependencies in [approval register](approval-register.md).
- [ ] Provider choices and live permissions distinguished from technical acceptance; blocked gates stay visible.
- [ ] Future session selects a single `BE-xxx` task and verifies prerequisite completion, not just document existence.
- [ ] Markdown links, trace matrix and API/schema/flow agreement checked after changes.

## Later implementation evidence
- [ ] Each major milestone has an actual meaningful git commit (not just a suggested message).
- [ ] All available application-level prompts exported, including retries, rejected suggestions and corrections; unavailable/redacted portions identified.
- [ ] Actual tests and independent reviews have outputs, failure/fix history and scope limits.
- [ ] Demo setup is reproducible from a clean clone with synthetic credentials/config examples, no private tokens/URLs/data.
- [ ] Live RSS/market evidence is separately labelled and permission-gated. Simulation and sandbox captures are not labelled production integrations.
- [ ] No complete-MVP claim while mandatory acceptance criteria remain blocked.
- [ ] Screenshots, if useful, reflect a real run; no screenshots are required or fabricated for documentation-only work.
- [ ] GitHub remote/public visibility checked by user before authorized push; never publish secrets or vault-only private context.

## Suggested milestone groups
These are grouping suggestions, not commits already made: planning package; scaffold/domain/persistence; identity/rules; ingestion; delivery/admin; regression/demo/handoff. Per-task messages and IDs are in the roadmap. Splitting or combining commits should preserve reviewable, working milestones; never rewrite unrelated history just to resemble this plan.

## Current evidence boundary
The repository currently documents an MVP design. Framework/source documentation checks are not application tests. SQLite/PostgreSQL behavior, actual login, delivery/retry and live adapters must be demonstrated during build chunks.
