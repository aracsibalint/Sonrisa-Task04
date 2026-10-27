# Security design

Status: **Proposed — pending approval, not a completed security audit.** No credentials, runtime configuration or infrastructure were changed. [Approved scope](../process/approved-scope.md), [architecture](overview.md), [API](../api/backend-api-design.md) and [model](../data/data-model-and-evolution.md) define the intended boundary.

## Trust boundaries and assets

| Boundary / actor | Permitted action | Controls |
|---|---|---|
| Anonymous client -> API | Explicit login only; minimal network-restricted health | TLS, login limits, body bounds, generic failures |
| Provisioned User/Admin -> owner APIs | Manage only own undeleted rules; see own safe channel availability | Built-in bearer auth, role policy, application owner predicate, optimistic expected version |
| Admin -> operations | Inspect normalized events/source failures/delivery state; manually accept events | Admin policy; no source/destination/user management or other-user rule editing |
| Controlled producer -> earthquake webhook | Submit only simulated Mw earthquakes to its mapped source | Per-source HMAC, signed timestamp/raw body, skew and durable key dedup |
| RSS/market -> normalizer | Supply untrusted content at fixed endpoints, conditional on rights approval | HTTPS allowlist, no redirects, bounded stream/parsing/time, strict typed validation |
| Dispatcher -> capture/Slack | Send immutable authorized recipient/body snapshot | Fixed versioned targets, secret resolution, timeout/retry policy, no arbitrary redirects |
| Host/storage operator | Provision identities/config/secrets and operate one process | Restricted filesystem, separate secret/key storage, audited stopped-app maintenance |

Assets: password hashes/stamps, opaque bearer credentials and Data Protection key ring; HMAC/SMTP/Slack secrets; account email, recipient snapshots, user rule preferences and notification content; dedup/baseline/retry history. SQLite is sensitive persistent data even without raw secrets. Local file access bypasses HTTP authorization: service identity and backups require access controls.

## Identity, sessions and authorization

- Pre-provision ASP.NET Core Identity users with framework password hashing and seeded User/Admin roles. UserName=email; normalized email unique. No self-registration, recovery, MFA enrollment, user management, external login, cookie login or refresh routes. Do not invoke `MapIdentityApi`; explicitly wire login using framework sign-in/token mechanisms. Verify actual .NET10 APIs and response during BE-004, not invented OAuth/JWT behavior.
- Proposed password provisioning: >=14 characters, max128 provisioned characters, no committed defaults; operator injects unique initial secrets through approved secret store/environment. Framework validation/hashing enabled; no custom hashing/salts/token crypto. Login input max256 to bound parsing; no trimming/case changes to password. Do not reset an existing password on restart or idempotent seed. Real production password lifecycle is outside MVP and needs an explicit operational policy.
- Access token lifetime30m; built-in opaque bearer, not JWT or full OAuth/OIDC. No supported refresh/logout/revocation API. Framework refreshToken response field may exist but cannot be redeemed without a mapped endpoint; sample clients discard it. A copied bearer can survive a role/password change until expiration; do not promise immediate stamp validation/revocation on each bearer request. Emergency key invalidation is a deliberate global session action, not a normal per-user logout.
- Login: generic401 for invalid/nonexistent/locked account, same body;5 failures ->15m lockout;10 attempts/min/IP queue0. Verify timing leakage mitigations using framework paths; do not assert perfect constant response latency. Do not log credentials or tokens even on failed model binding.
- Every Identity persistence write uses the same mutation gate as rules/events. Hash verification outside DB transaction; under gate reload stamp/concurrency state before acting; failed count/reset/rehash/provisioning cannot bypass ordering. Gate wait5s and503 on exhaustion. Do not hold transaction through hashing or external I/O; serialize persistence without nested gate acquisition. Auth race tests are mandatory.
- Resolve owner from authenticated subject only. Application queries include OwnerId and DeletedAt=null; wrong-owner/deleted/nonexistent ->404 after authentication. User/Admin role OR permits owner APIs, Admin policy permits inspection/manual input. Admin does not override owner checks. Separate webhook auth scheme cannot mint user/Admin identity.
- Rule update has expected version and delete If-Match; stale edits412. Scope roles/ownership first to avoid version-based information leaks. Concurrent changes affect future event commits only; old intent snapshots still send after pause/delete. Make this visible in API docs, not a hidden cancellation promise.

## Data Protection and secret lifecycle

- Persist the Data Protection key ring outside the database in a dedicated durable restricted directory; stable application discriminator per environment. Set explicit at-rest key protection suitable for the actual deployment: Windows DPAPI under the service identity for a single Windows host **or** certificate-backed protection for approved portable/container deployment. Exact host choice is UNKNOWN and a blocker for nonlocal operation; do not silently leave persistent keys unprotected.
- Restrict key/database/config directories to service account and authorized operator. Back up keys securely **separately** from SQLite; encryption certificate private keys/OS protection context may also be required. Restoring only DB does not restore token/cursor continuity. Different application name/purpose or lost keys invalidates old bearers/cursors. Moving DPAPI-protected keys to another machine/account may not work; plan explicit invalidation or protected rekey, not a file-copy promise.
- Password provisioning secret, source HMAC, SMTP credential and complete Slack webhook URL never appear in source control, CLI examples, issue reports, API responses, OpenAPI examples or structured log properties. Environment injection is acceptable for local sandbox with documented exposure limitations; production needs approved protected injection. Do not place secrets in SQLite references: store only opaque secret handles/target identity.
- Versioned destination identity is immutable. Rotating credential for same target may retain reference; changing actual recipient requires a new destination version. Old work uses original snapshot; missing/revoked original target fails visibly. Do not resolve an old reference to a newly assigned recipient. Emergency sender disable is operational and may leave work pending/fail; not a user cancellation API.
- HMAC rotation: add a new key ID mapping to the same immutable source; overlap only during operator-approved window, then revoke old key. Per-source secret >=32 random bytes; keys unique across sources/environments. Rotate only through approved secret operation (Level3), never through ingestion API.

## Machine authentication and replay

Normative byte protocol/examples: [API webhook section](../api/backend-api-design.md#webhook-signature-contract-raw-bytes).

`HMACSHA256(sourceSecret, ASCII(canonicalUnixSeconds) || LF || rawRequestBodyBytes)`; `sha256=` plus64 lowercase hex in X-Webhook-Signature, configured X-Webhook-Key-Id. Stream body at most64KiB; reject duplicate headers, malformed timestamp/signature/key, encoding/compression ambiguity; validate timestamp abs(skew)<=300s and use fixed-time compare on decoded32-byte MAC. Do not normalize/re-encode body before MAC check. Verify auth before returning detailed business validation failures. Use trusted current UTC clock, monitor drift; HTTP clock/timeouts are not monotonic scheduling guarantees.

Timestamp narrows replay window; durable `(SourceId, externalId)` identity closes duplicate creation within it and survives restart. Exact retry200 original event; changed normalized content409, no overwrite or rematch. New current timestamp can sign unchanged old event on retry, because signature/timestamp is excluded from business fingerprint. Verify existing identity/content before relative-time checks on already committed events; schema/auth validation still applies. Poll changed content is diagnostic only. Manual IDs are additionally authenticated-actor scoped, preventing one admin from colliding with another's key.

No universal abuse prevention: a compromised producer can send new IDs; source rate limit60/min and global intent admission cap limit work. Credential compromise response is revoke/disable source, preserve forensic IDs and rotate secrets explicitly. No ordinary bearer shortcut to webhook authentication.

## Network/SSRF controls

- Source URLs and destination definitions are centrally provisioned, not from rules/events. NASA RSS and Binance BTCUSDT are conditional candidates; permissions/region/reuse remain unresolved. No fallback provider, proxy bypass or credential addition on403/418. Article URLs are inert attribution/navigation only, never fetch them.
- Nonlocal API/inbound webhook/source/destination traffic uses TLS with normal certificate/hostname validation; never disable validation. SMTP uses approved STARTTLS/implicit TLS outside sandbox; reject downgrade. Restrict source scheme/host/port/path to exact configured endpoints. Disable automatic redirects and cookies. Public source DNS resolution must not lead to loopback/private/link-local/multicast or cloud metadata addresses; enforce outbound network allowlist/firewall or address-pinned validated connection strategy, not just a one-time URL check vulnerable to DNS changes. Proxy use is explicit; no arbitrary environment proxy bypass.
- Live Slack targets restricted to explicitly approved Slack webhook HTTPS hosts/path forms; URL is itself a credential. Fixed fake-Slack target is not a public fetch proxy. Outbound redirects are failures even to another allowed-looking hostname; do not forward credentials. Email receiver is fixed configured SMTP; rule requests never supply address/headers.
- **Local sandbox exception only:** explicitly selected `Sandbox` profile may use HTTP/private network for named local API, mail-capture and fake-Slack services; bind to loopback or isolated development network, restrict inbound exposure, use synthetic accounts/data, document exactly which host/port is allowed. Reject broad wildcard/private-IP allowlists. Never apply this exception to live public source endpoints or silently enable it in nonlocal environments. No production-security claim from local capture acceptance.
- CORS off by default (no frontend); if a future UI is approved, exact origin list and token storage/CSRF review required. Do not enable wildcard CORS/credentials. Bearer-only owner API avoids implicit cookie auth, but XSS/token theft remains a client concern. Trust forwarded client IP/scheme only from explicitly configured proxy networks; prevent spoofed X-Forwarded-For bypass of rate limits/TLS policy.

## Untrusted payload, rendering and resource bounds

| Surface | Required proposal |
|---|---|
| API/webhook |64KiB raw body; no request compression; JSON max depth16; reject duplicate/unknown fields, trailing JSON/invalid UTF-8; length/decimal/date/enum bounds before mutation |
| RSS |10s whole request+read timeout; max2MiB after decompression and max2MiB compressed input; max500 items; XML reader DtdProcessing=Prohibit, XmlResolver=null, MaxCharactersInDocument<=2MiB, bounded depth64, no entity expansion; count actual stream bytes, not Content-Length |
| Market |10s whole request+read;64KiB decompressed and compressed; depth16; strict JSON symbol/window/prices/timestamps; invalid/zero/overflow values never become valid zero movement |
| Polling |One in-flight request/source; fixed hourly NASA /60s market; persisted backoff/Retry-After; no source URL per rule; no expired-slot repair/backfill |
| Rules/acceptance |20 undeleted/user,200 total; at most400 intents/event; numeric checked operations; nonterminal intents<=10000;5s gate wait; full fan-out rollback if cap/unavailable destination |
| Delivery |One global send;15s I/O timeout;2m claim lease;5 attempts;10s/30s/2m/10m delays+jitter0..10%; Retry-After never shortened, >24h terminal DeferredTooLong |
| Lists/logs |page default25/max100; cursor<=4096; local error summary<=500; no unbounded detail expansions or raw payload/error logging |

Decompression counting must apply to actual bytes as they stream; automatic HttpClient decompression alone is not a size guard. Stop reading/dispose on limit/time/cancellation. Reject HTML200 when JSON/XML expected. Never persist response validators after incomplete/failed processing; initialization fails atomically on oversized/malformed document. Item errors are sanitized counts, not arbitrary HTML logs.

News normalization is safety-critical: title+description to deterministic plain text, NFC, ordinal case-insensitive substring; exclude content:encoded and no page fetch. Escape output per destination (plain-text email headers/body; Slack text with escaping and no broad mention expansion). Disable implicit Slack markup/mentions/link unfurling in adapter payload where supported; preserve required attribution with sanitized HTTP(S) URL. Never insert provider text into subject/address without CRLF checks. Truncate RSS normalized summary to4000 **before** matching/snapshotting and flag it; user input oversize400 rather than truncation.

**Open implementation blocker:** malformed HTML-to-text extraction algorithm/library has not been selected by architecture. BCL XML is not an HTML parser, and regex stripping is not an acceptable silent substitute. Recommend a separately approved small vetted HTML parser (new dependency=Level2) or an explicit deterministic conservative text-only policy approved as a semantic change; select/version/test it before BE-002 news normalization and BE-008 RSS. Do not add a package while claiming no new dependency. Normalize script/style content out and test HTML entities, malformed tags, Unicode, encoded mentions and oversized input. This is documented uncertainty, not delegated architecture choice.

## Rate limits, abuse and operational integrity

Use built-in ASP.NET Core rate limiting, no new limiter package: login10/min/IP; pre-auth webhook120/min/IP; authenticated webhook60/min/source; authenticated owner/admin API120/min/subject, with manual acceptance30/min/admin; global request concurrency20 and per-key queues0. Limits are initial sandbox safety proposals, not tested capacity; bounded partition caches/expiry prevent unbounded unique-IP allocation.429 with Retry-After, no queued acceptance. Trust only verified proxy client IP. Host/server request-header limits (proposed16KiB total) and request/header timeouts (proposed10s upload/headers) supplement application body bounds.

One writable SQLite process only, local persistent storage. Process/host exclusivity check fails closed if another process owns database deployment lock; application SemaphoreSlim-style gate is not cross-process coordination. Migrations/provisioning are stopped-app maintenance or gated operations, never concurrent external SQLite writes. Reads no-tracking/committed; transactional matching sees a bounded consistent active-rule version set. All writes including failures/health/counters/delivery state serialize. Gate never wraps HTTP/SMTP fetch/send.

Crash after external acceptance before completion can duplicate notification; attempts count before I/O, token-fenced completions, restart respects persisted retry/lease. No claim of exactly-once or guaranteed eventual delivery. Sent/Failed terminal; Failed can follow ambiguous acceptance. Never expose admin resend. Sender errors must not kill polling and vice versa; unexpected fatal worker loss fails readiness, not silent success.

Stop accepting on local storage/backlog pressure with retryable503, preserve dedup and validators, expose health. File growth/retention is a production gate; do not silently purge history/seen keys to fix disk pressure. Consistent SQLite backup includes journal/WAL semantics through supported backup or stopped-app procedure; backup restore, key continuity and token invalidation need rehearsed evidence.

## Logging, privacy and observability

Allowlisted log fields: correlation/event/source/rule/version/intent/attempt IDs, safe code/status, byte/count/duration/backlog. Exclude password/access/refresh/HMAC tokens, request authorization/signature headers, full Slack URLs, email address, raw request/provider body, SQL parameters containing secrets and exception URL text. Production EF sensitive-data logging off; HTTP request/response logging disabled/redacted on auth/webhook/sender clients. Sanitize newline/control injection. Error handling maps to fixed ProblemDetails codes; no stack trace/internal connection strings.

Admin responses redact destination secrets and recipient addresses; `/me/channels` exposes availability only. Admin delivery body is sensitive content, accessible only to Admin. Preserve provenance labels live/manual/simulated and sandbox evidence. No personal/public event feed. No claims of privacy compliance: retention, account deletion, data subject operations, production log access/duration and disk encryption policy need user/operator approval.

Use built-in logging/Meter/Activity initially: auth/rate rejection counts (no credentials), gate timeouts, parsing rejection, source stale/blocked, queue age/backlog, retry/ambiguous/terminal failures, disk pressure. Do not export PII to a telemetry service without separate approval. Health endpoints disclose readiness only, not exception details or upstream hosts.

## Dependency and verification gates

Pin/review EFCore/Identity packages, test packages and proposed MailKit; avoid adding HTML parser/Npgsql/exporters silently. Dependency/license/vulnerability review is required at approved pins; no installed dependency is claimed now. Use built-in cryptography/XML/JSON/rate limiting; security configuration/secret operations and destructive migrations need explicit Level3 permission.

Future tests: route inventory, UserA/UserB/Admin/machine separation, lockout/rate/expiry/key restart, failed-login writes under gate, stale versions, HMAC raw-byte test vectors/skew/rotation/replay, idempotency changed content, forged forwarded headers, XML DTD/XXE/decompression bombs/redirects, target revocation and CRLF/Slack mention escaping, redaction/log capture, malicious cursors, disk/backlog rollback, same-process lease recovery, backup/restore. No penetration test, production deployment, legal clearance or runtime assertion was performed in this documentation task.

## Review notes / open questions

- **BLOCKING before affected implementation:** reconcile four-state handoff versus five-state parallel flow, and expected-version contract versus last-write-wins. No other architecture boundaries changed here.
- **BLOCKING before news implementation:** approve deterministic HTML normalization strategy/dependency.
- **BLOCKING before live polling:** approve NASA coverage, Binance BTC/USDT, exact rolling window/repeated per-slot behavior, RSS first-baseline suppression, rights/distribution/jurisdiction.
- **BLOCKING before nonlocal operation:** choose host/key-at-rest protection/TLS/proxy/egress controls and credential lifecycle; local sandbox exception is not deployable by default elsewhere.
- **Production gates:** retention/capacity/real-channel policies and privacy/deletion/restore objectives; no SLA assumed.
