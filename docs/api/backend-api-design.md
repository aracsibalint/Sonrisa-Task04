# Backend API design — v1

Status: **Proposed — pending approval.** This is a build contract, not an implemented API. Level2 approval covers endpoints, authentication, status/validation behavior and DTOs.

[Scope](../process/approved-scope.md) · [Model](../data/data-model-and-evolution.md) · [Security](../architecture/security.md) · [Flows](../architecture/processing-flows.md)

## Shared wire conventions

- Base path `/api/v1`; JSON UTF-8, lowerCamelCase properties, string enum values shown below, no EF entity serialization. Breaking changes require a new major route/version, not silent reinterpretation of v1. No GraphQL, UI or external registration.
- GUID IDs are canonical lowercase D strings. Timestamps are UTC `YYYY-MM-DDTHH:mm:ss.fffZ`, exact millisecond precision, except HTTP authentication timestamp (Unix seconds). Decimal quantities are **JSON strings**, never JSON numbers or locale-formatted strings. Store canonical invariant decimal text; responses remove insignificant fractional zeroes. Input scale/range limits are in the model. Unknown properties, duplicate JSON property names, unknown enum values and properties belonging to another union case are rejected (`400`), not silently ignored.
- JSON bodies <=65536 bytes (including login/webhook); Content-Type `application/json` only, no compressed request body, unsupported media/content-encoding `415`. Oversize `413` at server/proxy and application stream boundary even without Content-Length; avoid buffering beyond cap. Parsing/validation `400`; no automatic defaults for category filters. Malformed IDs/cursors/queries `400`, route not found `404`.
- Normalized text: trim outer whitespace and NFC; keyword 1..100, name1..100, title1..500, summary optional1..4000, place optional1..200, producer externalId1..512 Unicode scalars and <=2048 UTF-8 bytes; reject controls in identifiers/name/title/destinations; allow ordinary newline in summary/place only after safe normalization. No user-supplied recipient/source URLs except optional news article navigation link; that link is never fetched.
- Successful writes are returned only after commit. Client timeout/uncertain commit is not proof of failure; retry manual/webhook with the same externalId and normalized payload. Rule create has no idempotency guarantee; after an uncertain create, list owned rules before creating another.
- Header `X-Correlation-Id` accepts a UUID or is replaced with a generated UUID, echoed on all responses. Response `Cache-Control: no-store` on authenticated APIs/auth/errors. No secret values in URLs or query parameters.

## Authentication and authorization matrix

`U` means bearer authenticated with User **or** Admin role. Provision Admin with both roles or use an explicit OR policy; never rely on accidental role inheritance. `A` means Admin role. `M` means source-specific webhook HMAC. Ordinary bearer tokens cannot authenticate M; HMAC cannot authenticate U/A.

| Method and path (under /api/v1) | Access / ownership | Success | Relevant failures |
|---|---|---|---|
| POST /auth/login | Anonymous, credential validation | 200 | 400,401,413,415,429,503 |
| GET /me | U, current principal | 200 | 401,403 |
| GET /me/channels | U, current owner's availability | 200 | 401,403 |
| GET /rules | U, own undeleted only | 200 | 400,401,403 |
| POST /rules | U, owner derived from principal | 201 + Location | 400,401,403,409,503 |
| GET /rules/{id} | U, own undeleted only | 200 + ETag | 400,401,403,404 |
| PUT /rules/{id} | U, own undeleted, expected version | 200 + ETag | 400,401,403,404,409,412,503 |
| DELETE /rules/{id} | U, own undeleted, If-Match required | 204 | 400,401,403,404,412,428,503 |
| POST /admin/events | A, manual actor scope | 201 new /200 replay | 400,401,403,409,413,415,503 |
| GET /admin/events | A only, operational inspection | 200 | 400,401,403 |
| GET /admin/events/{id} | A only | 200 | 400,401,403,404 |
| GET /admin/sources | A only, safe configuration status | 200 | 400,401,403 |
| GET /admin/sources/{id} | A only | 200 | 400,401,403,404 |
| GET /admin/sources/{id}/failures | A only | 200 | 400,401,403,404 |
| GET /admin/deliveries | A only | 200 | 400,401,403 |
| GET /admin/deliveries/{id} | A only | 200 | 400,401,403,404 |
| GET /admin/deliveries/{id}/attempts | A only | 200 | 400,401,403,404 |
| POST /webhooks/earthquakes | M, source resolved from key ID | 201 new /200 replay | 400,401,409,413,415,429,503 |

Global rate limits may return429 on any route. Unsupported methods405. `/health/live` and `/health/ready` are infrastructure paths, outside v1, minimal body and no credentials/configuration; 200/503, restricted to local/orchestrator network in nonlocal deployments.

Anonymous protected requests401 with `WWW-Authenticate: Bearer`; authenticated wrong role403. Wrong-owner, deleted or nonexistent rule returns identical404 after auth; admin role grants **no ownership bypass**. Admin endpoints are read-only except manual acceptance. No `/events` for ordinary users; no all-user rules, source/destination editing, registration, reset/recovery, refresh, logout, resend or cancellation endpoint. Unmapped routes return404, including framework Identity routes. Inspecting delivery outcomes does not permit changing recipients.

## Login and current account

POST `/auth/login` request:

```json
{"email":"user@example.test","password":"<provisioned-password>"}
```

Email <=256; password1..256 (do not trim/normalize password). Validate via ASP.NET Core Identity using built-in password hashing/opaque bearer issuance; no homemade token. Explicitly map only this route, **not MapIdentityApi**. Proposed30m access lifetime; no cookies and no refresh endpoint.

Expected framework-shaped200 response (verify against pinned .NET10 during BE-004):

```json
{"tokenType":"Bearer","accessToken":"<opaque-access-token>","expiresIn":1800,"refreshToken":"<framework-issued-but-unsupported-token>"}
```

Clients use `Authorization: Bearer <accessToken>` until expiry, then log in again. A framework refreshToken field is acknowledged for exact serializer compatibility, **not** an offered refresh feature; do not persist/use it in sample clients. If framework allows suppressing this field without custom token cryptography, changing the response requires an explicitly reviewed contract update. Invalid credentials, nonexistent/locked account -> identical401 `InvalidCredentials`; do not reveal existence or lockout details. Five failures trigger15m lockout; login rate10/min/IP, queue0. All Identity writes use shared gate, including counters/rehashes;503 if mutation cannot safely complete.

GET `/me`:

```json
{"id":"00000000-0000-0000-0000-000000000001","email":"user@example.test","roles":["User"]}
```

GET `/me/channels` (no address, URL, credential reference or webhook token):

```json
{"channels":[{"channel":"email","available":true,"reason":null},{"channel":"slack","available":false,"reason":"NotProvisioned"}]}
```

Always return both supported channels, stable order email/slack. Reason null if available, otherwise `NotProvisioned`, `Disabled` or `Revoked`; never operational secret text. Availability is a point-in-time view and revalidated during rule writes/event acceptance. Revocation does not redirect old intents.

## Rule DTOs and lifecycle

Create body, one of three exact filter shapes:

```json
{"name":"Space updates","category":"news","filter":{"keyword":"moon"},"channels":["email"],"enabled":true}
```

```json
{"name":"BTC movement","category":"market","filter":{"instrument":"BTCUSDT","comparisonKind":"binance-rolling-24h","thresholdPercent":"5"},"channels":["email","slack"],"enabled":true}
```

```json
{"name":"Sample quakes","category":"earthquake","filter":{"minimumMagnitude":"4.5","magnitudeScale":"Mw"},"channels":["slack"],"enabled":false}
```

`channels` is1..2 distinct strings `email`/`slack`, both provisioned and available when selected. Required name/category/filter/channels/enabled; no owner or destination fields. Per-user20 and global200 undeleted rule cap; paused counts. Over quota409 `RuleLimitReached`; unavailable selected channel400 `ChannelUnavailable`. Numeric limits: threshold >0<=1000000 with<=8 decimals; magnitude -2..10 inclusive with<=2 decimals. Keyword ordinal ignore-case substring in normalized title+separator+summary; not regex/word stemming. Market absolute inclusive threshold per accepted slot, not crossing. Category fixed after create.

RuleResponse (used for list/get/create/update):

```json
{"id":"10000000-0000-0000-0000-000000000001","name":"Space updates","category":"news","filter":{"keyword":"moon"},"channels":["email"],"enabled":true,"version":1,"createdAt":"2026-01-01T00:00:00.000Z","updatedAt":"2026-01-01T00:00:00.000Z"}
```

Illustrative timestamps are payload fixtures, not roadmap scheduling. POST returns201, `Location: /api/v1/rules/{id}`, `ETag: "1"`. IDs/owner/version/timestamps are generated by server. No deletedAt or owner ID needed in owner response.

PUT is a complete replacement of mutable fields; identical shape to create **plus required `version` positive integer**, category must equal existing category:

```json
{"name":"Space updates","category":"news","filter":{"keyword":"moon"},"channels":["email"],"enabled":false,"version":1}
```

Under gate compare expected version, apply change, increment to2 and return200/ETag `"2"`. Stale version412 `VersionConflict` (no update); missing version400. Category change400 `ImmutableCategory`. Even an otherwise identical successful PUT increments version; replay with old version412, not another mutation. Pause/resume is enabled=false/true, no extra endpoint. Client fetches current state and explicitly reconciles stale edits; no automatic overwrite/retry.

DELETE requires `If-Match: "2"`, exactly one strong positive-integer ETag; missing428 `PreconditionRequired`; weak/multiple/*/malformed400; stale412. Soft-deletes, disables, increments version and returns204; repeated delete404. Ownership is checked before version to avoid data leakage. Rule creation/update/resume never rematches history; deletion/pause never cancels already committed delivery snapshots.

**Reconciliation gate:** parallel processing-flows initially says last-committed edits without optimistic concurrency. Handoff explicitly requests version concurrency. This contract proposes version checks; architect must align before BE-005.

## Manual events and webhook payloads

Manual POST `/admin/events` uses `externalId` for idempotency, scoped to **manual category source + authenticated actor**. No Idempotency-Key header required or interpreted. Same ID may be used by another admin without collision; same actor/source/key+same normalized body returns original200, changed normalized body409 `IdempotencyConflict`. Do not include transport receipt time in fingerprint. New externalId is a deliberate new event and may notify again.

Three category-specific requests:

```json
{"externalId":"manual-news-001","category":"news","occurredAt":"2026-01-01T00:00:00.000Z","data":{"title":"Moon mission update","summary":"A synthetic example.","articleUrl":"https://example.test/article"}}
```

```json
{"externalId":"manual-market-001","category":"market","occurredAt":"2026-01-01T00:00:00.000Z","data":{"instrument":"BTCUSDT","quoteCurrency":"USDT","comparisonKind":"binance-rolling-24h","windowStart":"2025-12-31T00:00:00.000Z","windowEnd":"2026-01-01T00:00:00.000Z","openPrice":"100","lastPrice":"105"}}
```

```json
{"externalId":"manual-quake-001","category":"earthquake","occurredAt":"2026-01-01T00:00:00.000Z","data":{"magnitude":"4.5","magnitudeScale":"Mw","place":"Synthetic test location","simulated":true}}
```

Use a fake receipt clock matching these examples for tests; a real sender must use current market windows. Manual news permits null/omitted summary/articleUrl, earthquake null/omitted place; all other fields required. News occurredAt cannot be >5m future; no RSS baseline suppression for manual events. Market requires positive prices<=1e12, scale<=8, exact24h reported window, occurredAt=windowEnd, close within receipt-5m/+1m, configured symbol/window; canonical signed percent computed server-side. Never accept client percent as authority. Earthquake magnitude -2..10, scale<=2, Mw, simulated=true, occurrence<=receipt+5m. No upstream URL/recipient/owner/match override fields.

Webhook POST `/webhooks/earthquakes`:

```json
{"externalId":"sample-quake-001","occurredAt":"2026-01-01T00:00:00.000Z","magnitude":"4.5","magnitudeScale":"Mw","place":"Synthetic test location","simulated":true}
```

Category/source inferred; not supplied. Same validation/normalization/application acceptance as equivalent manual earthquake. Source provenance `simulated`; manual provenance `manual`; no claim of live seismic data.

### Webhook signature contract (raw bytes)

Required headers:

```text
X-Webhook-Key-Id: sample-earthquake-key-v1
X-Webhook-Timestamp: 1767225600
X-Webhook-Signature: sha256=<64 lowercase hex characters>
Content-Type: application/json
```

Key ID is1..64 ASCII `[A-Za-z0-9_-]`, resolves a configured >=32-byte random HMAC secret and immutable earthquake SourceId. Signature is lowercase hex HMAC-SHA256 of **ASCII timestamp + single LF byte(0x0A) + exact raw UTF-8 HTTP body bytes**. Timestamp grammar is canonical nonnegative integer Unix seconds (no plus/space/leading zero except0); it is the exact signed header string. Decode signature and compare using constant-time byte comparison. Do not reserialize JSON or include BOM/transcoding before signature verification. Reject duplicate signature headers, invalid UTF-8, BOM and compressed body. Unknown key, signature mismatch, missing/malformed auth headers or absolute server-clock difference>300s ->generic401 `InvalidWebhookAuthentication`; no event. Pre-auth size/rate enforcement precedes full buffering; authenticate before detailed schema diagnostics.

A captured valid request can be replayed inside skew but can create only one durable event because source+externalId dedup persists across restarts. Exact replay returns200; changed canonical content with reused externalId409 even when legitimately re-signed. After skew expiry, sender re-signs the **same** event with current timestamp; fingerprint excludes signature/timestamp. Unknown/revoked key401. Multiple active rotation keys may map to the same SourceId; dedup does not reset. Producer must never generate a new externalId merely to retry. No standalone nonce store needed for this bounded idempotent protocol; clock synchronization remains required.

### Acceptance response and transaction failure semantics

New input201 plus `Location: /api/v1/admin/events/{eventId}` for Admin callers only. Webhook omits Location because machine credential cannot GET admin detail. Replay200, same event ID. Response for both:

```json
{"eventId":"20000000-0000-0000-0000-000000000001","duplicate":false,"acceptedAt":"2026-01-01T00:00:00.100Z","matchedRuleCount":1,"deliveryIntentCount":2}
```

Webhook response omits `matchedRuleCount` and `deliveryIntentCount` to avoid cross-user activity disclosure; exact machine DTO is `{ "eventId": "20000000-0000-0000-0000-000000000001", "duplicate": false, "acceptedAt": "2026-01-01T00:00:00.100Z" }`. Replay returns original counts/time and duplicate=true. 201 means event/matches/intents committed, **not sent**; no202 because evaluation is synchronous. Same key duplicates are looked up before time-relative freshness revalidation of already accepted immutable content, so later legitimate retries remain idempotent; first validate schema/auth/canonicalization, then key equality. New events enforce current-time checks. Changed-body conflicts never replace the original.

All accepted inputs share one gated transaction: event, applicable seen entry, bounded active rule evaluation, positive matches and every selected intent. Gate5s timeout, SQLite busy/unavailable, backlog>10000 or unavailable referenced destination ->503 with safe code and `Retry-After: 5`; no partial event/identity commit. Arithmetic/schema invalidity400; DB uniqueness race re-reads canonical existing event, does not produce500 or rematch. Clients retry same key with bounded backoff; do not flood during backpressure. Pollers preserve their source identity and validators instead of inventing a new event.

## Pagination, filtering and inspection DTOs

List envelope:

```json
{"items":[],"nextCursor":null}
```

`pageSize` default25, integer1..100; `cursor` optional opaque protected string<=4096 ASCII. Keyset pagination, no offset/count promises. Rule/event/delivery/failure lists order by created/accepted/occurred UTC time DESC then ordinal ID DESC; sources by logicalName then configurationVersion then ID ASC; attempts by ordinal ASC (max5). Cursor protected with purpose/version-bound Data Protection, binds route, principal ID, filters, pageSize and last sort key. Reject tampering/changed filters or owner400 `InvalidCursor`; key loss invalidates cursors. Inserts after the first page may not appear in traversal; not a stable snapshot. No total count or user-controlled sort/numeric range query.

Allowed query fields (in addition to pageSize/cursor); unknown fields400:

| List | Filters |
|---|---|
| /rules | `category=news|market|earthquake`, `enabled=true|false`; implicit undeleted/owner |
| /admin/events | `category`, `sourceId`, `provenance=live|simulated|manual`, `acceptedFrom`, `acceptedTo` (UTC inclusive lower/exclusive upper) |
| /admin/sources | `kind=rss|market|earthquakeWebhook|manual`, `enabled` |
| /admin/sources/{id}/failures | `from`, `to` UTC half-open |
| /admin/deliveries | `status=pending|inFlight|sent|failed`, `eventId`, `ruleId`, `ownerId`, `channel=email|slack`, `createdFrom`, `createdTo` |
| /admin/deliveries/{id}/attempts | none; still envelope |

Malformed filter400; valid nonexistent filter ID yields empty list; nonexistent parent resource for nested route404. Validate from<to; no unbounded payload expansion or include parameter. Queries project AsNoTracking, page max100; combine composite indexes in model with measured query plans before adding new indexes. Decimal range/sort not offered.

### Events

EventListItem:

```json
{"id":"20000000-0000-0000-0000-000000000001","sourceId":"30000000-0000-0000-0000-000000000001","category":"market","provenance":"manual","occurredAt":"2026-01-01T00:00:00.000Z","acceptedAt":"2026-01-01T00:00:00.100Z","evaluatedRuleCount":2,"matchedRuleCount":1,"deliveryIntentCount":2}
```

EventDetail contains all list fields plus `schemaVersion:int`, `receivedAt:UTC`, `submittedByUserId:GUID|null`, `predicateVersion:int`, `data:category union`. No external composite key, payload hash, credentials or raw provider XML. News `data` is manual-news data plus `summaryTruncated:bool`; earthquake data same as manual-quake data. Market detail data:

```json
{"instrument":"BTCUSDT","quoteCurrency":"USDT","comparisonKind":"binance-rolling-24h","windowStart":"2025-12-31T00:00:00.000Z","windowEnd":"2026-01-01T00:00:00.000Z","openPrice":"100","lastPrice":"105","signedMovementPercent":"5","providerMovementPercent":null,"scheduledSlot":null}
```

Nullable union fields emitted as null; provider percent/slot present only when applicable. No expanding all matches/attempts; query deliveries by eventId. No ordinary-user event feed or notification-history API added.

### Sources and failures

SourceResponse used for both list/detail; all fields exact:

```json
{"id":"30000000-0000-0000-0000-000000000001","name":"binance-btcusdt","displayName":"Binance BTC/USDT","configurationVersion":1,"kind":"market","category":"market","provenance":"live","enabled":false,"health":"disabled","stale":false,"pollIntervalSeconds":60,"baselineCompletedAt":null,"lastAttemptAt":null,"lastFetchSucceededAt":null,"lastValidObservationAt":null,"nextPollAt":null,"consecutiveFailures":0,"consecutiveSchemaFailures":0,"lastErrorCode":null,"lastErrorSummary":null,"lastPollRejectedItems":0,"lastPollConflictingItems":0}
```

Health values uninitialized/healthy/degraded/blocked/disabled; disabled/nonpolled stale=false, active market stale if no valid observation after5m from activation or last valid; RSS after3 scheduled intervals without successful fetch. Activation time must be defined by source initialization/config enable operation, not process restart. GET exposes no URL, ETag, Last-Modified, secret reference or headers. PollIntervalSeconds nullable for nonpolled kinds; baseline only RSS. Provider is conditional, disabled until approvals.

SourceFailureResponse: `{id:GUID,sourceId:GUID,occurredAt:UTC,stage:"fetch"|"parse"|"normalize"|"accept"|"schedule",code:string,summary:string,httpStatus:int|null,rejectedItemCount:int}`; safe locally classified summary<=500, not upstream body. Example code `ResponseTooLarge`, stage `fetch`, status null, count0.

### Deliveries and attempts

DeliveryListItem:

```json
{"id":"40000000-0000-0000-0000-000000000001","eventId":"20000000-0000-0000-0000-000000000001","ruleId":"10000000-0000-0000-0000-000000000001","ownerId":"00000000-0000-0000-0000-000000000001","channel":"email","status":"pending","attemptCount":1,"createdAt":"2026-01-01T00:00:00.100Z","nextAttemptAt":"2026-01-01T00:00:11.000Z","sentAt":null,"failedAt":null,"lastErrorCode":"TransportTimeout"}
```

DeliveryDetail adds `ruleVersion:int`, `ruleNameSnapshot:string`, `filterSnapshot:category filter`, `predicateVersion:int`, `destinationConfigurationVersion:int`, `recipient:string` (literal `"[redacted]"` for both channels), `subject:string|null`, `body:string`, `lastErrorSummary:string|null`, `leaseExpiresAt:UTC|null`, `payloadSchemaVersion:int`. No secret reference/URL/claim token. Body excludes credential/recipient secret but contains admin-inspectable normalized event content; protect as sensitive. Rule deletion does not hide admin history. Pending includes scheduled retries; sent means receiver accepted, not human receipt. Four states are a pending architect reconciliation, not an implicit fifth retry status.

AttemptResponse: `{id:GUID,ordinal:int,startedAt:UTC,leaseExpiresAt:UTC,finishedAt:UTC|null,outcome:"inFlight"|"succeeded"|"transientFailure"|"permanentFailure"|"ambiguous"|"abandoned",errorCode:string|null,errorSummary:string|null,httpStatus:int|null,smtpStatus:int|null,retryAfterMilliseconds:long|null}`. Ordinal1..5. No full remote error/body, claim token or secrets. Failed does not guarantee nonreceipt after ambiguous attempts. No admin resend route.

## Error contract

All application errors use `application/problem+json` ProblemDetails; gateway-level failures may be empty and clients must handle status alone. Central handlers map validation/auth/rate/concurrency/domain exceptions to safe codes, never stack traces. Example:

```json
{"type":"urn:alerts:problem:version-conflict","title":"The rule has changed.","status":412,"detail":"Read the rule and retry with its current version.","instance":"/api/v1/rules/10000000-0000-0000-0000-000000000001","code":"VersionConflict","traceId":"00000000-0000-0000-0000-000000000010","errors":null}
```

`type=urn:alerts:problem:<kebab-case-code>` stable; title/detail safe fixed/localized message, instance route path without query, traceId correlation UUID. `errors` null except validation map of field paths to string arrays (`{"filter.thresholdPercent":["Must be greater than zero."]}`); never echo password/token/destination. `ValidationFailed`400; `InvalidCredentials`/`InvalidWebhookAuthentication`401; `Forbidden`403; `NotFound`404; `RuleLimitReached`/`IdempotencyConflict`409; `VersionConflict`412; `PayloadTooLarge`413; `UnsupportedMediaType`415; `PreconditionRequired`428; `RateLimitExceeded`429; `WriterBusy`/`BacklogCapacityExceeded`/`DestinationUnavailable`/`StorageUnavailable`503; `UnexpectedError`500. Validation special codes ChannelUnavailable/ImmutableCategory/InvalidCursor remain400.

429 includes whole-second `Retry-After`;503 transient admission default5 seconds. Rate limits and body validation cannot turn a rejected request into an accepted event. No detailed locked-account or key-existence responses.

## Contract verification / remaining gates

Future tests must enumerate routes to prove absence of MapIdentityApi features; check anonymous/A/B/admin/machine matrix, hidden ownership, stale PUT/DELETE, exact JSON numeric strings and unions, changed-payload conflicts, actor scopes, HMAC raw-byte vectors and skew boundaries, page cursor isolation, no secrets, limits, accepted-but-unsent response and duplicate recovery.

BLOCKERS: approve contracts/limits/dependency pins; reconcile concurrency/state names with architect; confirm .NET10 login response/runtime behavior; approve provider/window/repeated-snapshot/RSS-baseline semantics and permissions before live enablement. No frontend implementation is included; API consumer examples and OpenAPI generation belong to backend tasks.
