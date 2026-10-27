# Source feasibility research

Status: **Proposed — pending user approval.** Documentation-only research; successful public GET probes are **not integration tests**.

Authority: [approved scope](../process/approved-scope.md), [planning approach](../process/planning-approach.md). The [research task record](../process/prompts/04-source-research-agent.md) preserves the exact received prompt, consulted URLs, check outcomes and limitations.

## Recommendation and decision gates

**INFERENCE — technically feasible:** real RSS ingestion and public market polling can supply this MVP without API credentials. Four candidate endpoints responded successfully. No provider is approved, configured or integrated.

| Candidate | Exact proposed GET endpoint | Assessment |
|---|---|---|
| BBC News Top Stories | `https://feeds.bbci.co.uk/news/rss.xml` | Simple general-news RSS; works without credentials in probes. Reuse/keyword-analysis and email/Slack redistribution need clarification under BBC terms. |
| NASA News Releases | `https://www.nasa.gov/news-release/feed/` | **Preferred RSS alternative for approval** if space/science news satisfies the category. Officially advertised RSS and comparatively permissive informational-use guidance. Feed quality/cache differences require defensive handling. |
| Coinbase Exchange BTC-USD stats | `https://api.exchange.coinbase.com/products/BTC-USD/stats` | Simple USD market candidate. Public endpoint and 24-hour stats documented, but exact opening-price/window boundaries are insufficiently specified in the consulted REST page; no window timestamps. Material reuse restrictions. |
| Binance public BTCUSDT 24-hour ticker | `https://data-api.binance.vision/api/v3/ticker/24hr?symbol=BTCUSDT` | **Preferred market alternative on window evidence**, subject to terms/access clearance and approval of BTC/USDT. Explicit rolling window, opening/closing prices and interval timestamps. USDT is not USD. |

**Proposed recommendation:** ask for approval of NASA RSS plus Binance BTCUSDT as the better-evidenced technical pair, while retaining BBC/Coinbase as conditional candidates. Binance terms could not be substantively retrieved, so this is not an unconditional live-use recommendation. If USD denomination is essential, retain Coinbase only with acceptance/clarification of its provider-24-hour semantics and applicable usage rights. Do not silently substitute providers, symbols or currencies.

**BLOCKERS before live enablement / affected contract approval:**

1. User chooses providers and approves NASA's narrower news coverage and, if selected, BTC/USDT rather than BTC/USD.
2. Establish permitted use for the actual operator and recipients: BBC metadata/computer-analysis and distribution restrictions; Coinbase internal-use/derived-work restrictions; or Binance's as-yet-unread applicable terms and geographic eligibility. Sandbox delivery does not itself establish permission.
3. Approve movement magnitude/inclusive threshold, repeated qualifying snapshots, local polling-slot identity, and RSS baseline behavior below. These user-visible semantics are proposals, not implied by selecting a provider.
4. If Coinbase is chosen, accept a named **provider-defined 24-hour statistic with unknown exact boundaries**, or obtain authoritative clarification before promising a precise rolling interval. Binance supplies stronger evidence if explicit boundaries are required.

Future adapter, persistence, matching and delivery tests remain required. This research establishes endpoint reachability and sample shape, not runtime correctness or an availability guarantee.

## Evidence classes and method

- **FACT — documentation:** statements actually retrieved from official provider pages/repositories, or the RSS specification. Evidence IDs below refer to the URL register in the task record.
- **FACT — probe:** observed HTTP responses from explicit, credential-free GETs. Payload shape, headers and values describe those responses only.
- **INFERENCE:** conclusions from that evidence, including comparative suitability.
- **Proposed:** local adapter/processing behavior awaiting approval.
- **UNKNOWN / BLOCKER:** evidence gaps, including provider permissions and implementation prerequisites.

Official documentation was fetched using `webfetch`. Endpoint bodies were fetched with `webfetch` where noted and explicit PowerShell `Invoke-WebRequest` GET probes for HTTP status/headers. Shell probes used a 20-second timeout, disabled redirects (`-MaximumRedirection 0`), used only `Accept` as an explicit request header, and supplied no credentials, session cookies or API keys. RSS samples were parsed in memory using .NET XML with DTD processing prohibited and external resolution disabled. No packages were installed and no application files or live setup were created. Response dates below are evidence, not roadmap dates or estimates.

## Actual endpoint probes

| ID | Method / endpoint | Observed result | Limits of the check |
|---|---|---|---|
| P1 | `webfetch` text GET, BBC endpoint above | RSS 2.0 body; channel `BBC News`; `ttl=15`; items with title, description, link, GUID and publication time. | Tool did not expose HTTP headers/status; freshness and article truth were not independently checked. |
| P2 | Explicit GET, BBC | **200**, `text/xml; charset=utf-8`; 36 items; `Cache-Control: public, max-age=2`; no ETag, Last-Modified, Age or Retry-After exposed. Response Date `Tue, 27 Oct 2026 02:33:55 GMT`; channel lastBuildDate `Tue, 27 Oct 2026 02:33:46 GMT`. | One direct sample. No conditional GET, rate-limit, sustained polling or outage test. |
| P3 | `webfetch` text GET, Coinbase stats | JSON with string-valued `open`, `high`, `low`, `last`, `volume`, `volume_30day`, and RFQ volume fields. | No HTTP metadata exposed by this tool. |
| P4 | Explicit GET, Coinbase stats | **200**, `application/json; charset=utf-8`; `Cache-Control: public, must-revalidate, max-age=5`; weak ETag present; no Age or Retry-After exposed. Response Date `Tue, 27 Oct 2026 02:33:55 GMT`. | No snapshot/window timestamp in body. Cache headers do not establish trade-data freshness. |
| P5 | Explicit GET, NASA feed | **200**, `application/rss+xml; charset=UTF-8`; 10 items; `Cache-Control: must-revalidate, max-age=300`; weak ETag; Last-Modified `Tue, 27 Oct 2026 01:40:18 GMT`; response Date `Tue, 27 Oct 2026 02:34:29 GMT`. | First item's GUID appeared empty; inspected again in P7 rather than assuming parser error or stable IDs. |
| P6 | `webfetch` text GET, NASA feed | RSS 2.0; `sy:updatePeriod=hourly`, `sy:updateFrequency=1`; populated GUIDs in visible items; descriptions plus large HTML `content:encoded`; channel lastBuildDate `Mon, 26 Oct 2026 19:41:01 +0000`. | Returned a different first item from P5/P7. P5 recorded an HTTP Last-Modified header, not a channel lastBuildDate, so those timestamps are not directly equivalent. Full output was tool-truncated; the discrepancy's cause is unknown. |
| P7 | Explicit repeat GET, NASA, targeted XPath inspection | **200**; confirmed first GUID element was `<guid isPermaLink="false"></guid>`; same first link as P5; 10 items, hourly/1 hints, 275325 content characters. Response Date `Tue, 27 Oct 2026 02:35:00 GMT`. | Repeat was to resolve an identity ambiguity, not a polling endurance test. A replacement character appeared in the decoded title; byte-level encoding diagnosis was not performed. |
| P8 | Explicit GET, Binance ticker | **200**, `application/json; charset=UTF-8`; `Cache-Control: no-store, must-revalidate, no-cache`; `X-MBX-USED-WEIGHT-1M: 2`; no Retry-After exposed. Response Date `Tue, 27 Oct 2026 02:34:43 GMT`. | One symbol, one successful request; no deployment-region or quota exhaustion verification. |

Selected P4 response fields: `open="79459.45"`, `last="79844.34"`, `high="80710.04"`, `low="79255"`, `volume="6351.78583501"`, `volume_30day="149191.2154565"`. No product ID, event ID, sequence, opening time, closing time or observation time was present in the returned body.

Selected P8 response fields: `symbol="BTCUSDT"`, `openPrice="79551.45000000"`, `lastPrice="79940.00000000"`, `priceChange="388.55000000"`, `priceChangePercent="0.488"`, `openTime=1792982083005`, `closeTime=1793068483005`, `firstId=6791254271`, `lastId=6794193037`, `count=2938767`. The observed timestamp difference is 86400000 milliseconds. Trade IDs delimit constituent trades; they are not a unique ticker-snapshot ID.

**UNKNOWN:** tools/CDNs can return cached or different representations. P6 and P5/P7 differ; no claim is made that `webfetch` and direct GET used the same origin response. No provider's news accuracy, price accuracy, SLA or continuous availability was verified.

## RSS field contract and constraints — Proposed

Documentation evidence: BBC feed guide B1, BBC terms B2, NASA feed directory N1, NASA usage guidance N2, RSS specification R1. Actual field evidence: P1/P2/P5/P6/P7.

| Field | Meaning / proposed handling |
|---|---|
| `channel/title`, configured source ID | Attribution/display name versus stable local source identity. Never use mutable title as database identity. |
| `item/title` | Human-readable headline; decode XML/HTML entities and normalize text for keyword matching. |
| `item/description` | Summary, potentially HTML or CDATA. Match normalized plain text of title plus summary; do not execute embedded markup or retrieve linked pages. NASA's `content:encoded` is excluded from matching and notifications. |
| `item/link` | Original article URL for user navigation and fallback identity. Keep original URL for attribution; do not fetch it during ingestion. Permit only HTTP(S) in output. |
| `item/guid` | Opaque publisher identity, scoped to configured source. `isPermaLink=false` means it must not be assumed navigable; retain exact nonempty decoded text. |
| `item/pubDate` | Publication timestamp with timezone; parse to UTC separately from local ingestion time. Optional in RSS, so not guaranteed by the standard. Not a sequence/cursor. |
| `channel/lastBuildDate` | Feed-content change time; not an item identity or reliable completeness watermark. |
| `channel/ttl` | Minutes of cache lifetime under RSS specification. BBC sample: 15. Not a contracted quota or publication SLA. |
| `sy:updatePeriod`, `sy:updateFrequency` | NASA sample signals hourly/1; proposed conservative refresh is hourly. These hints do not promise an item each hour. |

**Proposed polling:** BBC every 15 minutes (at most 96 scheduled requests/day); NASA hourly (24/day). Honor a longer valid publisher cache/update hint and `Retry-After`. Use conditional requests if an ETag or Last-Modified is available; a 304 means no new body/events. BBC did not expose those validators in P2; NASA did in P5. Conditional requests were not tested. These operating intervals are not effort estimates.

**Proposed identity:** `(configuredSourceId, identityVersion, identityType, identityValue)`, with nonempty GUID first, otherwise absolute article link after only deterministic URI normalization (lowercase scheme/host, remove default port and fragment, preserve path/query). If neither is usable, record a rejected item; do not fabricate an ID from title or fetch time. Apply a uniqueness constraint and persist seen identities, including baseline-suppressed items, across restarts. Same identity with edited text does not create a new event or retroactive match.

**Known BBC trade-off:** P1 contained the same article link under GUID suffixes `#1` and `#8`. Treating GUIDs as opaque can yield two distinct feed-entry events for one article; the default proposal does not promise article-level deduplication. If one alert per article is required, approve a BBC-specific secondary canonical-link identity and its tracking-parameter rules before implementation. Do not strip GUID suffixes by guessing their meaning.

**NASA constraint:** empty GUIDs really occurred in P5/P7; the link fallback is mandatory. Large HTML payloads, UTF-8/entity handling, source variants and potentially mixed editorial content need fixture coverage. The officially named News Releases feed returned science/photojournal items too; do not promise it contains only press releases.

### First fetch, backlog and rule lifecycle — Proposed

1. An uninitialized source completes its first fully parsed, acceptable 200 response as a **baseline only**: persist all usable current identities as seen/suppressed and record local UTC `baselineCompletedAt` atomically. Emit no events and send no alerts from this initial backlog, even if rules already exist. Initial failure leaves the source uninitialized. Initial 304 without a stored baseline requires an unconditional fetch.
2. Later successful polls ingest previously unseen, valid items from the current feed only. To avoid resurfaced pre-baseline stories, suppress and mark seen any item whose `pubDate <= baselineCompletedAt`. Missing/invalid publication dates are rejected for this proposed adapter policy; materially future timestamps are rejected as invalid pending a later valid observation. This is stricter than RSS itself.
3. Proposed future-time tolerance is five minutes against local UTC receipt time. Reject invalid items individually with an admin-visible count/reason; do not assign a fresh publication time. Whole-document parse failure must not advance the baseline, seen set or validators. Invalid items are not permanently marked seen so a publisher correction can be accepted later.
4. Rules evaluate only when a new eligible event is durably ingested; creation/update/resume never re-evaluates stored events or baseline entries. Continue source ingestion and dedup while individual rules are paused. A late-arriving item published after source baseline can match a newly created rule if it is first ingested while that rule is active: eligibility is ingestion-based, not a claim that publication followed rule creation.
5. Restart retains source identity/baseline/seen set. A source URL or identity-policy change needs an explicit new source/configuration version and fresh suppressed baseline. Do not silently reset state on restart, outage, or rule changes.

**Loss boundary:** an item published during initialization can be suppressed with the baseline; an item that appears and disappears between polls can be missed. No archive paging or historical backfill is proposed. After an outage, unseen eligible items still present in the current feed may be newly ingested; items already evicted cannot be reconstructed. Feed caching can also cause post-baseline arrivals carrying older publication dates to be suppressed by the cutoff. Approval must accept these conservative no-backlog trade-offs.

## Market fields, units and comparison window

### Coinbase BTC-USD — documentation versus inference

- **FACT (C1/C2):** Exchange market-data APIs are public. `GET /products/{product_id}/stats` has operation-level OpenAPI `security: []`, despite the surrounding schema listing authentication schemes. It supplies “30day and 24hour stats.” Required response fields are string-valued `open`, `high`, `low`, `last`, `volume`; `volume_30day` and additional volume breakdowns also appear.
- **FACT (C1):** `open`, `high`, `low` are quote-currency values and `volume` is in base-currency units. For BTC-USD these prices are USD per BTC, and volume is BTC. **INFERENCE:** `last` is the latest trade price in the same quote units; the consulted REST schema lacks a prose definition of `last` and of the exact opening trade selection. Coinbase's separate WebSocket ticker example uses `price` and `open_24h` (C5), supporting the intended 24-hour interpretation without proving identical REST boundary behavior.
- **Proposed calculation:** `signedMovementPercent = 100 * (last - open) / open`, using only a single response. Name the comparison `coinbase-provider-24h`; do not label it change since the previous poll, UTC-day change, or precisely `observedAt - 24h`.
- **UNKNOWN / selection gate:** C1 does not explicitly define rolling versus calendar boundaries or opening-trade selection. Neither schema nor P4 supplies window endpoints, trade timestamp or snapshot ID. Store `windowStart`/`windowEnd` as unknown; local receipt time and HTTP Date must not be substituted as authoritative market times. If the contract requires exact boundaries, use the stronger alternative below or obtain clarification.

### Binance BTCUSDT — stronger provider-window evidence

- **FACT (M1/M2):** the official repository documents `/api/v3/ticker/24hr` as **“24 hour rolling window price change statistics.”** `data-api.binance.vision` serves this endpoint without authentication. A single `symbol` uses weight 2; omitting symbols can cost weight 80 and returns a much larger result. Default response type is FULL. The documented data source is Memory; the API warns generally that asynchronous data can be delayed.
- **FACT (M1/P8):** `openPrice` and `lastPrice` are opening and closing prices of the interval; `openTime` and `closeTime` are the interval's start/end, in milliseconds by default. No microsecond header was sent. Normalize these Unix epoch milliseconds to UTC while retaining original integers. `volume` is base-asset volume; `quoteVolume` is quote-asset volume.
- **Proposed mapping:** instrument BTC/USDT; prices in USDT per BTC, `volume` in BTC, `quoteVolume` in USDT. `priceChangePercent` is a percent value (`0.488` means 0.488%, not 48.8%). Retain it as provider metadata, but calculate the canonical matching value as `100 * (lastPrice - openPrice) / openPrice`, so provider display rounding does not change boundary tests. Do not use `prevClosePrice`, bid/ask, weighted average, or the previous poll as the denominator.
- Preserve the reported interval as `binance-rolling-24h`; validate start before end and the expected 24-hour interval for the selected contract. A changed interval is a validation failure, not an invitation to silently change alert meaning. P8's 86400000-millisecond interval agrees with the documentation; one response cannot verify every boundary case or price-selection rule.

### Deterministic matching — Proposed for either market candidate

- Parse prices as invariant-culture decimal strings; require strictly positive opening and last prices. Reject missing/null, malformed, overflow, zero/negative values and unsupported symbol/window combinations; never convert them to a zero movement. Use checked decimal arithmetic and a fixed calculation order/version.
- Proposed rule: positive threshold in percent, match **`abs(signedMovementPercent) >= thresholdPercent`**. Both increases and decreases match; retain signed movement for the message. Inclusive equality and magnitude-based behavior require user approval.
- Compare before display rounding. For exact boundary handling, the equivalent nonnegative decimal comparison `100 * abs(last - open) >= thresholdPercent * open` avoids division-rounding ambiguity; overflow is a validation failure. Use this comparison consistently in implementation and fixtures.
- Label notifications with source, instrument, quote currency, provider-window kind, observed-at time and signed percent. Binance includes the reported interval; Coinbase explicitly has unknown exact window endpoints. The window is independent of polling cadence; no local 24-hour price history is required.
- Poll one centrally configured symbol every **60 seconds**: at most 1440 scheduled requests/day, excluding retries. Do not query once per user/rule. Fast polling does not turn this into real-time delivery or guarantee detection of a crossing between samples.

## Market snapshot identity and repeat behavior — Proposed

Coinbase supplies no upstream snapshot identity. Binance trade-range IDs identify trades, and even its reported interval is not an explicit immutable event ID. Adopt an intentionally local **poll-observation** identity:

`(configuredSourceId, instrument, comparisonKind, pollScheduleVersion, scheduledSlotStartUtc)`

For a 60-second schedule, the slot is the UTC minute boundary captured **before** the request. One successfully validated snapshot may be accepted per slot, enforced by a durable uniqueness constraint. Receipt time is separate. Persist the accepted normalized snapshot/event atomically; a retry after persistence reuses that event and never replaces its prices. A retry before acceptance retains the same slot identity; stop retrying once that slot expires. After restart, inspect durable state and schedule the current/next slot; never backfill missed slots. Source/symbol/schedule changes are explicit version changes.

- Save a canonical payload fingerprint for diagnostics; do not use whole raw JSON or price alone as the sole key. JSON field ordering/decimal formatting must not create duplicates. A global price hash would suppress legitimate later observations.
- The same values in a **different** slot are intentionally a new observation under this proposal. A rule above threshold can therefore notify once per minute per channel, even without a new crossing. This is **level-triggered snapshot matching**, not crossing detection or cooldown. Approval of this notification volume is a contract gate; reducing it requires an explicit policy rather than accidental dedup behavior.
- Downstream uniqueness should include `(eventId, ruleId, channel)` so reprocessing a slot cannot enqueue duplicate logical notifications. External delivery retry semantics still require their own implementation tests.
- Failed, stale, malformed or unaccepted snapshots create no market event. Never relabel the last successful stored sample as a new live observation. Coinbase cannot prove underlying data freshness; repeated identical data alone proves neither an outage nor current trading.

## Quotas, attribution and terms

| Source | Evidence and constraints | Unresolved matters |
|---|---|---|
| BBC | B1 requests prominent “BBC News” or `bbc.co.uk/news` attribution and links to an obsolete terms URL (B3 returned 404). Current terms B2 section 15 permits certain website/social RSS uses with attribution/preservation conditions, requires business permission, and restricts extracting metadata. Section 8 also addresses computer analysis; section 12 concerns credits/tracking. | Matching extracted text and sending summaries by email/Slack is not expressly cleared by the website-feed permission. Older and current branding guidance differ; do not treat B1 alone as authoritative permission. No numeric API quota or SLA found in consulted pages. |
| NASA | N1 explicitly publishes the feed. N2 allows factual/informational use under its guidance, requests NASA acknowledgment, prohibits implied endorsement, and identifies third-party copyright/branding exceptions. | N2 is chiefly media guidance, not a bespoke RSS service agreement. Confirm the intended text redistribution/territory and any third-party rights; NASA logos/images are not automatically licensed for arbitrary use. No numeric polling quota or SLA found. |
| Coinbase | C3: public REST limit **10 requests/second/IP**, bursts up to **15**, with 429 on excess. C4 limits use to personal/research and the operator/entity's officers/employees; external dissemination includes derived works and needs prior express written consent. It also contains AI/automated-system restrictions, fees clauses and no availability/timeliness guarantees. | Determine applicability to this assignment/operator and recipients, including how live samples may be used in an AI-assisted build workflow. Public/no-key access is not unrestricted redistribution or a perpetual free-service promise. Plain source attribution does not cure licence restrictions; no trademark grant. |
| Binance | M1: shared IP request-weight limits, 429 and possible 418 bans for repeated violations; Retry-After is in seconds. Weight 2 documented and used-weight header 2 observed. Published exchangeInfo example shows rate-limit fields, not a guarantee of this deployment's current allowance. | Terms URL M5 yielded no substantive content. Allowed redistribution, attribution obligations, regional eligibility and any fees remain **UNKNOWN**. No live exchangeInfo probe was made; do not claim a verified total quota from an example. |

**Proposed attribution:** plain source name plus original article/source link, preserve relevant notices and currency/window labels, and avoid logos or endorsement language. For BBC, resolve preservation versus transformed text/notification use before enabling. For NASA, omit images/full article bodies from the notification contract and review third-party exceptions. Attribution proposals are not provider approval.

## Outages, stale data and bounded recovery — Proposed

- One in-flight poll per configured source; 10-second request timeout for the future adapter. Use HTTPS, fixed admin-configured endpoints, bounded response size (proposed 2 MiB RSS / 64 KiB market after decompression) and safe XML parsing. Treat redirect/HTML/error bodies and schema mismatch as failures; a changed destination needs admin review. These limits were not load-tested.
- On timeout/network failure/5xx/429, record failure and schedule bounded exponential backoff with jitter. Start at the source interval, double up to one hour, and honor a longer valid Retry-After. No tight retry loop. A market retry scheduled beyond its minute is a new future slot, not a repair of the expired slot.
- On 401/403/404, Binance 418, persistent schema failures or region restrictions, mark the source degraded/blocked and expose the reason for admin action. Do not add credentials or switch providers/hosts automatically. Respect a ban's Retry-After and avoid repeated requests while blocked.
- Persist last attempt, last successful fetch, last valid observation, consecutive failures, next permitted attempt and a bounded error summary. Expose data freshness separately from HTTP reachability. Mark market source stale after five minutes without a valid observation; RSS fetch health stale after three scheduled intervals without success. RSS age of the latest article alone does not mean the source is down.
- Binance freshness proposal: reject new observations whose `closeTime` is more than five minutes behind receipt or more than one minute in the future; record clock/data anomaly. Coinbase's HTTP Date/cache metadata can flag transport-cache anomalies but cannot verify price age. Surface underlying timestamp as unavailable rather than claiming freshness.
- Do not move RSS seen/baseline state on failed fetch; commit response validators only alongside successful processing, to avoid a later 304 hiding unprocessed items. Do not send alerts from stale cached market data. Already accepted events continue downstream independently of source availability.
- Recovery resumes current feed/current snapshot polling. No synthetic “all clear,” no zero-valued market sample, no archive fetch, and no reconstructed missed observations. Expose gaps; do not promise exactly-once external delivery or complete upstream capture.

## Fixture strategy and future verification — Proposed, not executed

Use small, hand-authored synthetic XML/JSON shaped like documented/observed responses, clearly labelled synthetic. Avoid committing full copyrighted feeds or live market datasets until their use is cleared. Fixed clocks, explicit source state and deterministic scheduler inputs should make expected results independent of internet access. No fixture or test code was created in this task.

| Scenario | Required future assertion |
|---|---|
| RSS baseline and restart | Initial A/B creates only durable seen entries; later A/B/C creates C once; restart and unchanged feed do not replay A/B/C. Failed initialization stays uninitialized. |
| RSS identity variants | Opaque GUID, empty GUID with valid link (NASA), no identity, same GUID edited, duplicated GUID, BBC same link/different GUID, escaped tracking query, malformed date and UTF-8/entity cases have explicit outcomes. |
| RSS backlog/rules/outage | Pre-baseline resurfaced item is suppressed; paused/new/resumed rules do not replay stored events; current-feed arrivals after outage follow baseline cutoff; source outage is reported as a potential capture gap without inventing IDs/counts of unseen items. |
| Market exact threshold | Synthetic open 100, last 105 gives +5%; last 95 gives -5%; threshold 5 matches both; last 104.999 does not. Test zero/negative/open overflow, invalid decimal strings, locale independence and arithmetic overflow rejection. |
| Provider-window distinction | Coinbase has no invented window timestamps; Binance stores exact epoch milliseconds and rejects wrong/stale/future intervals; USDT is never relabelled USD; previous-poll price never changes the denominator. |
| Market replay/slots | Same slot retried/concurrently inserted produces one event; changed retry body cannot overwrite accepted values; same values in next slot produce a new event by the proposed policy; restart creates no missed-slot backlog. |
| HTTP failures | 304, 429 with/without Retry-After, 418, 403, 5xx, timeout, malformed XML/JSON, oversized/decompression payload and HTML-200 cases emit no fabricated source events. Validators/state are not advanced past unprocessed data. |
| Delivery | Synthetic accepted events traverse matching and local email capture/fake Slack. Assert logical dedup independently of upstream transport. This future sandbox check is not live email/Slack integration evidence. |

After provider/behavior approval, run explicit adapter smoke checks from the intended runtime, recording status, content shape, version and limitations separately from fixture tests. Recheck terms and public limits before enablement. A successful smoke check remains narrower than an end-to-end integration test.

## Earthquake source boundary

**FACT — approved scope:** earthquake input is an authenticated webhook from a **simulated, controlled sample producer**, not a discovered vendor or live earthquake upstream. No vendor webhook is needed or researched. Proposed fixtures should carry a stable producer event ID, occurrence time, magnitude and an explicit simulated-source label; retries reuse the event ID. Magnitude is a scale value, not percent. The controlled producer's exact magnitude scale/schema/authentication belongs in the separately approved contract. No webhook, authentication setup or sender was operated here.

## Handoff summary

- **Verified:** official BBC/NASA RSS publication; no-key Coinbase/Binance market documentation; explicit 200 GET responses for all four candidates; concrete field/cache/window evidence and documented limitations.
- **Proposed:** NASA/Binance technical preference, comparison rules, polling intervals, snapshot identity, RSS suppressed baseline, attribution and failure/fixture strategy.
- **Unresolved blockers:** user provider/behavior approval, applicable usage permissions and region/access conditions, and Coinbase's exact window semantics if selected. Implementation and integration validation remain future work.
