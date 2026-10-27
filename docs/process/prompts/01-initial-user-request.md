# Initial user request — visible text transcription

The following preserves the initial user message from the available conversation. HTML entities are left as supplied; line wrapping is presentation only.

```text
Task: Feature Design &amp; Build from a Vague Brief

Scenario

A product manager has handed you the following brief — exactly as written, nothing
more:
&quot;We want users to be able to set up alerts so they get notified when
something important happens in the world — like breaking news, market
movements, natural disasters, that kind of thing. Should work for both email
and Slack. Make it flexible enough that we can add more channels later.
We need an admin view too.&quot;
You may use any technology, language, or framework you choose.
There is no further specification. No wireframes. No schema proposal. No agreement
on what &quot;something important&quot; means, where the data comes from, or how events
are detected.
Your Task
Using AI agents, take this brief from ambiguity to a working implementation.
Before you start building, define your own plan of attack. Decide what
deliverables you think this brief requires, what order to tackle them in, and why. Your
plan is itself part of the evaluation — we want to see how you scope and structure
work, not just how you execute a predefined checklist.
Then execute against your own plan. As you go, critically assess what the AI
produces: don&#39;t assume outputs are correct just because they look plausible.
Surface the checks you run, the things you reject, and the course-corrections you
make — that judgment is as important as the work itself.
Suggested Timeframe
Plan for 24 hours. A partial but well-reasoned solution beats a complete but
undocumented one — use the time to think, plan, and show your work, not to
maximize output.
A Note on What We&#39;re Evaluating
We are not evaluating the solution itself. We are evaluating how you arrived at it.
A working, polished implementation with no visible process tells us very little. What
we want to see is how you directed the AI: how you framed the problem, what
decisions you made and why, where you course-corrected, and how you validated
output before accepting it. The artifacts of that process — plans, decision logs,
prompt drafts, intermediate outputs — are the submission. The code is
evidence, not the point.
We pay particular attention to how you evaluated the validity of what the AI
produced — the assumptions you challenged, the hallucinations or shortcuts you
caught, the code you rewrote or rejected, and the sanity checks you applied to
generated artifacts.
AI Tooling
Use any AI tool(s) you prefer — there is no required or favored product.
What to Submit
Please share your results with us via a GitHub repository that must contain:
• commits for each major milestone, with meaningful commit messages
• your prompt history — all prompts used during the process
• your plans, working artifacts, and evidence of how you worked — design
docs, decision logs, prompt drafts, intermediate outputs, notes, scratch files,
screenshots, etc.
• your deliverables — as defined by your own plan

This is an assignment in a software developer job application process.
Plan the architecture, design, tasks, etc.
Ask, if something is not clear, and let me decide.

Create the necessary documentations (md files) in well structured separated folder.
Build ADR for the reasoning behind the architectural decisions, already in this plan phase.
Create a data model md file for the schema and its planned evolution.

The goal is an MVP for now. I want you to create a roadmap (also an md file), and that can contain future features as well. But keep the MVP simple.

Add 3 type of alerts (ideally multiple kind of data source, like webhook, polling, RSS for example), and also support adding events manually.

I will need admin and user role for now, the admin will be able to create events manually.

We should use sqlite for simplicity and portability, but it should be easily migrated to postgres.

Use EF Core ORM and .NET 10. Plan the backend part only for now.

Now I want a full plan, but after that, I will build the chunks in the roadmap one by one, each should have an ID that I can use later.
```
