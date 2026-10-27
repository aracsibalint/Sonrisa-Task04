# Clarification questionnaire — questions and selected answers

These are the exact question texts and selected answer labels from the structured question tool. The full option-description payload remains in conversation history; this is not a full tool export.

## 1
> Which three alert categories and matching rules should the MVP support?

Selected: **Proposed three (Recommended)** — news keyword, market percentage movement, earthquake minimum magnitude.

## 2
> What ingestion scope is acceptable? Sources would be centrally configured/admin-controlled, not arbitrary user URLs. Specify any preferred providers or available API credentials in a custom answer; do not paste secrets.

Selected: **Two live, one simulated (Recommended)** — news RSS and market polling live subject feasibility; simulated controlled earthquake webhook.

## 3
> Approve this rule model: users manage and pause only their own rules; each rule selects one category, its supported filter, and email and/or Slack; only newly ingested events are evaluated; manual events follow the same matching rules; no shared rules, historical backfills, digests, or general Boolean rule builder?

Selected: **Approve model (Recommended)**.

## 4
> What identity/admin scope should we plan? The recommended admin API supports manual events, inspection of events/ingestion failures/delivery outcomes, and source status; it does not grant editing of users' rules. No frontend is included.

Selected: **Seeded accounts (Recommended)** — real authentication, single tenant, no registration/recovery/user-management API.

## 5
> Which delivery scope should the MVP require? Recommended Slack integration uses a preconfigured incoming webhook rather than OAuth workspace installation. Email recipients are pre-provisioned/verified, not unrestricted arbitrary addresses.

Selected: **Sandbox sufficient**.

## 6
> How should the assignment's suggested 24 hours constrain the roadmap?

Exact custom answer:
> Do not add estimations/dates in roadmap. The 24 hours is only for me for the assignment.

## Interpretation
These decisions approve scope, not the later provider selection, polling-repeat semantics, specific libraries or authentication implementation. Those stay in the approval register.
