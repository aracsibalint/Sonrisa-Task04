# Processing flows and matching contracts

Status: **Approved recovery baseline; runtime unverified.** [Architecture](overview.md) owns bounds and [API](../api/backend-api-design.md) owns wire contracts. Provider choice/rights remain gated.

## First-ingestion matching
1. Fetch/authenticate/parse and validate normalized content outside the write gate. No external IO in a transaction.
2. Outer Application use case acquires the singleton nonreentrant gate once (max32 waiters,2s deadline); pass internal admission context to scoped participants without reacquisition/capture. Measure mutation disk headroom; fresh DbContext/short transaction, SQLite busy timeout1s.
3. Resolve stable source identity and dedup key. Existing input returns original outcome according to adapter policy.
4. Read active, nondeleted category rules (100 global/20 owner; paused excluded) and destination versions. Match all intents (max200/event); projected Pending+InFlight+RetryScheduled <=20,000 and retained all statuses <=250,000.
5. Insert immutable event and snapshots/intents directly, with Event/Rule/Owner/Destination relationships and no match table. Each intent includes RuleVersion, RuleNameSnapshot, Category, validated immutable FilterSnapshotJson(max2048), PredicateVersion and MatchedAtUtcMs; unique(EventId,RuleId,Channel). Update source/seen state; commit or roll back everything, even for zero matches.
6. Acknowledge only committed acceptance. Notify dispatchers optionally; durable polling is authoritative, not in-memory signals.

Optimistic Version/ETag edits and commit ordering are complementary: reject stale edits, then if a successful update commits before event acceptance use its version; if afterward, existing intents remain. Rule create/update/resume never replays stored events. Soft delete hides the rule but preserves delivery/history foreign keys.

## News
One nonempty keyword/phrase: Unicode NFC normalization, whitespace collapse and ordinal case-insensitive substring comparison against plain-text title plus summary separated by a space. Normalize both sides. No regex/stemming/full article or RSS `content:encoded`; approved AngleSharp family converts permitted title/summary content to text without fetching resources (pins delegated after checks). RSS max100 items; title500, summary4000, combined16KiB UTF-8 normalized text. Deterministically truncate RSS summary before matching/snapshotting and flag it; manual oversize is rejected. Rendering escapes HTML/Slack mention syntax.

RSS key = configured source/version + exact nonempty GUID; otherwise normalized absolute HTTP(S) article link (lowercase scheme/host, remove default port/fragment, preserve path/query). No usable key means reject item. Same key edited is ignored, not a new event; different opaque GUIDs can represent the same article and remain different entries.

Initial successful fully parsed acceptable 200 response creates an atomic **suppressed baseline**, including usable seen identities, and `BaselineCompletedAt`; no notifications. Empty valid feed establishes an empty baseline. Failed parse leaves state untouched. Initial 304 without baseline triggers unconditional fetch. Later polls accept unseen valid publication times strictly after baseline, no more than five minutes in the future. Old items are marked suppressed; malformed dates are rejected without permanently marking seen. An item corrected later can become valid.

Later polls commit per-item acceptance; only commit response validators after all valid items are processed or deterministically suppressed. Backpressure/DB failure prevents validator advancement; re-fetch repeats safely via dedup. Whole-document size/item/parse failure produces no checkpoint. Invalid-item counts/reasons are visible but payloads are not logged. First baseline and feed eviction may lose stories; no historical archive recovery promised.

## Market
Conditional candidate: Binance BTCUSDT, **USDT not USD**, rolling-24-hour open/last from one provider response, with original window epochs. Validate positive decimals, symbol/window, 24-hour interval, close time within five minutes past/one minute future of receipt. Matching uses exact scaled-integer arithmetic, not decimal products that can silently round even in checked code.

For positive threshold percent, match the exact mathematical inequality `100 * abs(last - open) >= threshold * open`. Lift each validated decimal coefficient to eight fractional places using BCL BigInteger before subtraction/multiplication. For scaled integers L/O/T and S=100000000, compare `100 * abs(L - O) * S >= T * O` without division or rounding. Both directions match, inclusive equality; signed decimal percentage is display-only and may round to decimal precision, never an input to matching. It is not change since previous poll, crossing detection or a cooldown policy.

Identity = source/config version + instrument + comparison kind + schedule version + UTC-minute slot captured **before request**. First accepted snapshot wins within slot even if retried response changes. Same prices next slot are a new event and can notify again. Expired/unaccepted slots are not backfilled; never relabel cached sample as fresh. One qualifying rule can notify once per minute/channel; repeated per-slot behavior is accepted, while provider choice/rights remain gated.

## Earthquake and manual events
Simulated producer supplies stable event ID, occurrence UTC, magnitude scale `Mw`, proposed magnitude range -2..10. Rule comparison is `magnitude >= minimum`, inclusive. Webhook authentication is separate from user roles. Same producer key and normalized fingerprint returns original event; same key different content is 409, never overwrite.

Admin manual input supports all categories, same normalized validation and matching, provenance `Manual`, actor-scoped idempotency key. Same key/content returns original; different content conflicts. Ingestion-time eligibility is independent of occurrence time; manual submission of an old event is intentional new input, not a historical backfill job. Manual news need not obey RSS baseline (transport state), but must obey normalized category validation.

## Dispatch state machine
`Pending -> InFlight -> Delivered | RetryScheduled | Failed`.

- Pending has count0 and due time; InFlight has count1..5, claim token and2m lease; Delivered has DeliveredAt; RetryScheduled has count1..4 and due time; Failed has count1..5, or count0 only for explicit permanent provisioning failure before any send claim (no fabricated attempt). Delivered/Failed are terminal.
- One dispatcher/channel claims oldest due Pending or RetryScheduled under gate/transaction, increments attempt count and creates attempt record, then sends outside gate with10s whole-I/O timeout, immutable payload/destination version and resolved secret reference. Check completion headroom before claiming/sending.
- Persist result in a new short transaction fenced by claim token. Delivered means receiver/transport acceptance, not human reading.
- Transient network/timeout/408/429/5xx or SMTP temporary errors retry within five total attempts; permanent rejection fails. After ordinal n use30s*2^(n-1) with0..10% nonnegative jitter and30m local cap; honor larger valid Retry-After without24h terminal cutoff. Successful channel is not resent because another fails.
- Restart waits any prior unexpired lease. Recover only expired orphaned InFlight: cancel/await any local live operation first, close attempt as `OutcomeUnknown`, schedule retry if budget remains, otherwise terminal Failed with uncertain-outcome reason. Count the claimed attempt even if process died before sending.
- Receiver acceptance followed by lost acknowledgement/local commit failure may produce an external duplicate on retry. Unique logical intent cannot prevent it. No eventual-delivery or exactly-once guarantee.
- Removed destination version fails visibly; never redirect old intents to a newly configured recipient. Secret rotation may resolve the same stable destination version, not alter its target.

## Source failures
Persist last attempt/success/valid observation, failure count, next permitted attempt and bounded reason. Transient backoff starts at cadence, doubles to one hour, honors longer Retry-After; 401/403/404/ban/region restriction blocks pending operator action. No silent provider fallback. Market stale after five minutes without valid observation; RSS fetch stale after three intervals. Do not mistake quiet news for fetch failure.

Persist SourceState.ActivatedAtUtcMs at source initialization/enable, never reset on process restart; use it when no successful observation/fetch exists. All source bookkeeping uses mutation admission, including failures.

## Storage and writer failure
Measure every mutation's growth, including zero-match events, seen rows, rules and failure history. Normal mutation needs1GiB free after16MiB conservative allowance. Only bounded send-result/recovery and essential Identity security bookkeeping may use emergency256MiB after1MiB allowance. No send without completion headroom. Below emergency, unmeasurable space or DB write/checkpoint failure: stop claims/sends/mutations, fail readiness503, and never return login credentials/acceptance success without required commit. Use bounded nonpersistent redacted diagnostics, not recursive DB failure records; no automatic deletion. Budgets are unmeasured, not OS reservations; test WAL/checkpoint/headroom in BE-003. Windows local-NTFS lifetime writer ownership precedes every writer including migration/provisioning; see security for handle/path checks.

## Fault boundaries to test
Before event commit: no partial event/intents/checkpoint. After commit: retry dedups. Rule/event race: one coherent rule version. RSS partial poll: committed items survive, validators do not skip unprocessed items. Send before/after receiver acceptance: truthful uncertainty and bounded recovery. Full disk/saturation: retryable rejection, no false acknowledgement. See [test strategy](../validation/acceptance-and-test-plan.md).
