# Processing flows and matching contracts

Status: **Proposed; approval required for user-visible semantics.** [Architecture](overview.md) owns bounds and [API](../api/backend-api-design.md) owns wire contracts.

## First-ingestion matching
1. Fetch/authenticate/parse and validate normalized content outside the write gate. No external IO in a transaction.
2. Acquire bounded writer admission; fresh DbContext/transaction.
3. Resolve stable source identity and dedup key. Existing input returns original outcome according to adapter policy.
4. Read active, nondeleted category rules and provisioned destination versions. Exact deterministic matching yields all intents; check projected queue/retention capacity.
5. Insert immutable event and complete set of snapshots/intents; update relevant source/seen state; commit or roll back everything.
6. Acknowledge only committed acceptance. Notify dispatchers optionally; durable polling is authoritative, not in-memory signals.

If rule update commits before event acceptance, use updated rule. If afterward, existing intents remain. Rule create/update/resume never replays stored events. Soft delete hides the rule but preserves delivery/history foreign keys.

## News
One nonempty keyword/phrase: Unicode NFC normalization, whitespace collapse and ordinal case-insensitive substring comparison against plain-text title plus summary separated by a space. Normalize both sides. No regex/stemming/full article or RSS `content:encoded`; HTML parser converts permitted title/summary content to text without fetching resources. Rendering escapes HTML/Slack mention syntax.

RSS key = configured source/version + exact nonempty GUID; otherwise normalized absolute HTTP(S) article link (lowercase scheme/host, remove default port/fragment, preserve path/query). No usable key means reject item. Same key edited is ignored, not a new event; different opaque GUIDs can represent the same article and remain different entries.

Initial successful fully parsed acceptable 200 response creates an atomic **suppressed baseline**, including usable seen identities, and `BaselineCompletedAt`; no notifications. Empty valid feed establishes an empty baseline. Failed parse leaves state untouched. Initial 304 without baseline triggers unconditional fetch. Later polls accept unseen valid publication times strictly after baseline, no more than five minutes in the future. Old items are marked suppressed; malformed dates are rejected without permanently marking seen. An item corrected later can become valid.

Later polls commit per-item acceptance; only commit response validators after all valid items are processed or deterministically suppressed. Backpressure/DB failure prevents validator advancement; re-fetch repeats safely via dedup. Whole-document size/item/parse failure produces no checkpoint. Invalid-item counts/reasons are visible but payloads are not logged. First baseline and feed eviction may lose stories; no historical archive recovery promised.

## Market
Conditional candidate: Binance BTCUSDT, **USDT not USD**, rolling-24-hour open/last from one provider response, with original window epochs. Validate positive decimals, checked arithmetic, symbol/window, 24-hour interval, close time within five minutes past/one minute future of receipt.

For positive threshold percent, match `100 * abs(last - open) >= threshold * open` before display rounding. Both directions match, inclusive equality; signed calculated percent is retained for display. It is not change since previous poll, crossing detection or a cooldown policy.

Identity = source/config version + instrument + comparison kind + schedule version + UTC-minute slot captured **before request**. First accepted snapshot wins within slot even if retried response changes. Same prices next slot are a new event and can notify again. Expired/unaccepted slots are not backfilled; never relabel cached sample as fresh. One qualifying rule can notify once per minute/channel; this volume requires explicit approval.

## Earthquake and manual events
Simulated producer supplies stable event ID, occurrence UTC, magnitude scale `Mw`, proposed magnitude range -2..10. Rule comparison is `magnitude >= minimum`, inclusive. Webhook authentication is separate from user roles. Same producer key and normalized fingerprint returns original event; same key different content is 409, never overwrite.

Admin manual input supports all categories, same normalized validation and matching, provenance `Manual`, actor-scoped idempotency key. Same key/content returns original; different content conflicts. Ingestion-time eligibility is independent of occurrence time; manual submission of an old event is intentional new input, not a historical backfill job. Manual news need not obey RSS baseline (transport state), but must obey normalized category validation.

## Dispatch state machine
`Pending -> InFlight -> Delivered | RetryScheduled | Failed`.

- Claim under gate/transaction, increment attempt count and create attempt record, then send outside gate with immutable payload/destination version and resolved secret reference.
- Persist result in a new short transaction. Delivered means receiver/transport acceptance, not human reading.
- Transient network/timeout/429/5xx or SMTP temporary errors retry within five total attempts; permanent rejection fails. Respect larger valid Retry-After. Successful channel is not resent because another fails.
- Restart inspects abandoned InFlight: close attempt as `OutcomeUnknown`, schedule retry if budget remains, otherwise terminal Failed with uncertain-outcome reason. Count the claimed attempt even if process died before sending.
- Receiver acceptance followed by lost acknowledgement/local commit failure may produce an external duplicate on retry. Unique logical intent cannot prevent it. No eventual-delivery or exactly-once guarantee.
- Removed destination version fails visibly; never redirect old intents to a newly configured recipient. Secret rotation may resolve the same stable destination version, not alter its target.

## Source failures
Persist last attempt/success/valid observation, failure count, next permitted attempt and bounded reason. Transient backoff starts at cadence, doubles to one hour, honors longer Retry-After; 401/403/404/ban/region restriction blocks pending operator action. No silent provider fallback. Market stale after five minutes without valid observation; RSS fetch stale after three intervals. Do not mistake quiet news for fetch failure.

## Fault boundaries to test
Before event commit: no partial event/intents/checkpoint. After commit: retry dedups. Rule/event race: one coherent rule version. RSS partial poll: committed items survive, validators do not skip unprocessed items. Send before/after receiver acceptance: truthful uncertainty and bounded recovery. Full disk/saturation: retryable rejection, no false acknowledgement. See [test strategy](../validation/acceptance-and-test-plan.md).
