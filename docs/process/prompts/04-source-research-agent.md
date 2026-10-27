# Source research task record

Status: Completed documentation-only research; recommendations **Proposed — pending user approval**.

Output: [source feasibility](../../research/source-feasibility.md).

## Exact received task prompt

The following is the exact application-level user task received in this session, not a reconstruction or a hidden-instruction transcript:

```text
Documentation-only research in C:\Repos\Sonrisa-Task04. Read docs/process/approved-scope.md and planning-approach.md. Investigate a simple real news RSS feed and real market API suitable for deterministic % movement alerts with provider-defined comparison window, preferably no API key (e.g. BBC RSS and Coinbase public exchange stats BTC-USD; alternatives welcome if better verified). Use webfetch official docs and read-only GET probes if available, no credentials and no package installs. Distinguish documentation evidence from actual endpoint probes; do not claim integration tested. Specify exact candidate endpoints, fields/units/window semantics, polling proposal and constraints, quotas/terms attribution uncertainties, market dedup identity for polling snapshots, RSS initial backlog semantics, outage behavior, fixture strategy. Earthquake webhook is simulated controlled producer, not live upstream; no need to invent vendor webhook. Produce docs/research/source-feasibility.md and docs/process/prompts/04-source-research-agent.md recording exact received task prompt, URLs consulted, check outcomes and limitations. Candidate providers are Proposed pending user approval, no new code or live setup. Own only these files. No dates/effort estimates in roadmap. Return recommendation, factual evidence and unresolved blockers.
```

## Local inputs and scope checks

- Read `docs/process/approved-scope.md` and `docs/process/planning-approach.md` in full. Their approval/evidence boundaries govern the research.
- Inspected repository root and searched for `AGENTS.md` (no matches). Initial `git status --short` showed existing untracked `docs/` and `vault-project.md`; these were treated as existing user/session work.
- Initial reads of `docs/research/` and `docs/process/prompts/` returned not found. Created only the two requested Markdown artifacts via patch.
- Scope: public source research, bounded read-only GETs, and proposed ingestion semantics. No credentials, installs, application code, scheduled polling, accounts, webhook producer, live deployment, commit or push.

## URLs consulted and outcomes

Each documentation entry below was requested using `webfetch` in Markdown or text format. “Retrieved” means substantive content was returned, not that a provider contract was independently certified or that linked pages were all reviewed. Blank responses and failed guesses are recorded explicitly. Endpoint GETs are separately listed below.

| ID | Exact consulted URL | Outcome / evidence used |
|---|---|---|
| B1 | https://www.bbc.co.uk/news/10628494 | Retrieved official BBC feed guide. Lists Top Stories RSS, geographical editions, attribution and the old RSS terms link. This is an older guide, not sufficient current permission by itself. |
| B2 | https://www.bbc.co.uk/usingthebbc/terms-of-use/#15metadataandrssfeeds | Retrieved official current terms page presented by the site. Read RSS/metadata section, computer-analysis permission language, attribution/branding and business-use conditions. |
| B3 | https://www.bbc.co.uk/terms/additional_rss.shtml | **404 GET**, the obsolete terms URL linked by B1. Not used as evidence of permission. |
| R1 | https://www.rssboard.org/rss-specification | Retrieved RSS Advisory Board specification: TTL in minutes, optional item fields, publication times, opaque GUID semantics and isPermaLink. |
| C1 | https://docs.cdp.coinbase.com/api-reference/exchange-api/rest-api/products/get-product-stats | Retrieved official REST/OpenAPI page. 24hour/30day stats, string fields, unit note and operation-level `security: []`. Exact window endpoints/opening selection not specified. |
| C2 | https://docs.cdp.coinbase.com/exchange/introduction | Retrieved: market-data APIs are public; official link to Market Data Terms. |
| C3 | https://docs.cdp.coinbase.com/exchange/rest-api/rate-limits | Retrieved: public 10 requests/sec/IP, burst 15, 429, token bucket. |
| C4 | https://www.coinbase.com/legal/market_data | Retrieved official terms. Personal/research/internal-use limitation, derived-work dissemination restrictions, AI/automated-system clause, fee and availability caveats. |
| C5 | https://docs.cdp.coinbase.com/exchange/websocket-feed/channels | Retrieved. Ticker example has `open_24h` and `price`; contextual evidence only, not proof of REST boundary semantics. No WebSocket connection made. |
| C6 | https://docs.cdp.coinbase.com/exchange/legal/market-data-terms | **404 GET** for guessed documentation route; actual legal page found through C2 and recorded as C4. |
| N1 | https://www.nasa.gov/rss-feeds/ | Retrieved official feed directory; lists News Releases endpoint and explains headlines/summaries/links. |
| N2 | https://www.nasa.gov/nasa-brand-center/images-and-media/ | Retrieved official usage guidance: informational/factual use, acknowledgment, no implied endorsement, third-party/branding exceptions. Primarily media guidance; no blanket RSS redistribution conclusion. |
| N3 | https://www.nasa.gov/nasa-web-privacy-policy-and-important-notices/ | Retrieved privacy/security/linking notices. No numeric RSS quota or additional definitive RSS reuse licence identified. |
| M1 | https://raw.githubusercontent.com/binance/binance-spot-api-docs/master/rest-api.md | Retrieved Binance's official repository document; tool output truncated. Used targeted grep/read of the saved response for the 24hr ticker section (lines 1430–1584), plus visible general/security/limits sections. Explicit rolling window and field/time/weight semantics verified. Mutable master URL, not a pinned specification version. |
| M2 | https://raw.githubusercontent.com/binance/binance-spot-api-docs/master/faqs/market_data_only.md | Retrieved official repository page; explicitly no authentication/API key, lists `/api/v3/ticker/24hr` on `data-api.binance.vision`. |
| M3 | https://developers.binance.com/docs/binance-spot-api-docs/rest-api/market-data-endpoints | Tool returned no substantive content. No documentation claims based on this response; used M1 instead. |
| M4 | https://developers.binance.com/docs/binance-spot-api-docs/faqs/market_data_only | Tool returned no substantive content. Used M2 instead. |
| M5 | https://www.binance.com/en/terms | Tool returned no substantive content. Applicable redistribution, attribution, region and fee terms remain unresolved. |

## Endpoint checks and method

Executed explicit PowerShell GETs with `Invoke-WebRequest -Method Get -TimeoutSec 20 -MaximumRedirection 0 -SkipHttpErrorCheck`, plus `Accept: application/json` for market or `Accept: application/rss+xml, application/xml` for RSS. No authentication parameters/headers or credential/session options were supplied. Inspected status, selected response headers and response-body fields in memory. RSS XML parsing prohibited DTDs and external resolution. These are probe summaries, not a verbatim terminal transcript; the received task prompt above is verbatim.

| Probe IDs | Exact endpoint | Actual checks and outcomes |
|---|---|---|
| P1/P2 | https://feeds.bbci.co.uk/news/rss.xml | One `webfetch` body fetch and one explicit GET. Explicit GET: **200**, XML parsed, 36 items, TTL 15 minutes, public/max-age=2 cache header; no ETag/Last-Modified in exposed headers. GUID/title/link/pubDate/description inspected. |
| P3/P4 | https://api.exchange.coinbase.com/products/BTC-USD/stats | One `webfetch` body fetch and one explicit GET. Explicit GET: **200**, JSON body, decimal-string price/volume fields, weak ETag and max-age=5. No body timestamp/window ID. |
| P5/P6/P7 | https://www.nasa.gov/news-release/feed/ | Two explicit GETs and one `webfetch` fetch. Explicit GETs: **200**, RSS parsed, 10 items, empty first GUID; second used explicit XPath to confirm `<guid isPermaLink="false"></guid>`. First response had weak ETag/Last-Modified and max-age=300; hourly/1 syndication hint confirmed. Webfetch returned a different first item, populated GUID examples and large HTML content; output was truncated. Its channel build time is recorded separately from the direct response's HTTP Last-Modified; these are not equivalent fields. Body difference is unresolved. |
| P8 | https://data-api.binance.vision/api/v3/ticker/24hr?symbol=BTCUSDT | One explicit GET: **200**, JSON with rolling-interval fields, open/last prices, provider percentage, trade IDs, and used-weight-1m=2. Observed window difference 86400000 ms. |

The [research report](../../research/source-feasibility.md#actual-endpoint-probes) records response Date headers, selected payload values and cache headers for reproducibility context. Transient payloads were not saved as separate fixture files.

## Research findings and recommendation

- **FACT:** all four exact candidate endpoints returned 200 to credential-free direct GETs in this environment. This establishes limited reachability/shape evidence only.
- **FACT:** Binance's official documentation explicitly specifies a rolling 24-hour window and interval endpoints; Coinbase's consulted REST documentation is less precise and the response omits them.
- **FACT:** BBC and Coinbase publish substantive restrictions relevant to the proposed use. Binance terms could not be read through the available fetch result. NASA usage guidance is comparatively permissive but has qualifications.
- **INFERENCE / Proposed:** prefer NASA RSS and Binance BTCUSDT on technical evidence, conditional on user approval, actual usage rights/access and acceptance of narrower news coverage/USDT denomination. BBC/Coinbase remain proposed alternatives, not rejected or approved providers.
- **Proposed contracts:** suppressed initial RSS baseline; durable GUID/link dedup; 60-second market polling slots; provider-window price comparison with inclusive magnitude threshold; repeat qualifying snapshots explicitly allowed under the proposed level-triggered policy; bounded recovery and synthetic fixture-first verification.
- **BLOCKERS:** provider and behavioral approval; applicable provider terms and recipient/territory fit; Coinbase's exact window clarification or acceptance if selected. A public endpoint is not a licence or proof that the future application works.

## Check outcomes and limitations

- Reviewed the two artifacts for agreement with approved scope, explicit Proposed labels, exact endpoints, field units/window evidence, identity/baseline/recovery behavior and fixture strategy.
- Read back both files and checked their local reference targets. `git diff --no-index --check -- /dev/null "docs/research/source-feasibility.md"` and the equivalent command for `docs/process/prompts/04-source-research-agent.md` reported no whitespace errors; Git emitted only LF-to-CRLF working-copy notices. `git status --short --untracked-files=all` showed the two new artifacts alongside the existing scope/planning/vault-project files.
- Documentation claims and actual probes are separately labelled; no integration-test claim is made. No failure injection, quota exhaustion, conditional GET/304, sustained polling, intended-runtime deployment check, rate-limit enforcement or availability study was performed.
- No database, adapter, rules engine, authentication, earthquake webhook, email capture or fake Slack integration was built or exercised. No fixture tests were executed; proposed fixtures are future verification work.
- NASA's empty GUID, differing retrieved bodies and replacement character in decoded title remain concrete reasons to test identity/encoding/cache behavior. The causes were not established. Provider cache headers and HTTP Date are not proof of underlying data freshness.
- No total current Binance rate-limit allowance was probed; the documentation's exchangeInfo sample must not be treated as a measured quota. Terms/license interpretations and regional availability are unresolved, not legal clearance.
- Only the two requested files are owned by this task. No roadmap was edited or dates/effort estimates added to one. Response timestamps in research are evidence metadata.
