# SQLite local sandbox operations (BE-003)

Status: BE-003 persistence and BE-004 identity foundation implemented; independent BE-004 review pending. Canonical requirements remain [schema](data-model-and-evolution.md), [security](../architecture/security.md) and active ADR-001/002/003/004/006. [Evidence](../process/build/BE-004.md) distinguishes actual disposable tests from future operations.

## Supported envelope and ownership

- Windows, fixed local NTFS volume only. Supply an absolute ordinary drive path (`C:\...\alerts.db`) with an existing parent directory. UNC/network, device/extended paths, ADS, dot/trailing-dot/space aliases, reparse traversal and hardlinks are unsupported/refused. Final opened-handle path must equal the supplied full path (ordinal-ignore-case); native `GetFinalPathNameByHandle` and `GetFileInformationByHandle` verify final path and single link. Unsupported/unverifiable identity fails closed.
- `WindowsDatabaseOwner` first acquires `<database>.writer.lock` with `FileShare.None`, then keeps a read/write non-delete-sharing database handle for its lifetime. Existing WAL/SHM/journal siblings are checked too. The app, explicit migration command and future provisioning writers must retain the same owner lifetime. Never delete a stale-looking sidecar; after crash the OS releases the handle and the next owner opens the existing file.
- This is cooperative ownership against accidental aliases/second writers, not protection from a hostile local operator, concurrent directory/ACL/hardlink tampering, administrator/native raw-volume access or arbitrary external SQLite tools. Such operation is unsupported, not silently treated as safe. No network filesystem or PostgreSQL runtime support is claimed.

## Generate and inspect (offline/model-only)

```powershell
dotnet tool restore
dotnet ef migrations script 0 InitialPersistence --project src/Alerts.Infrastructure --startup-project src/Alerts.Infrastructure --configuration Release --no-build --output docs/data/BE-003-initial.sql
dotnet ef migrations has-pending-model-changes --project src/Alerts.Infrastructure --startup-project src/Alerts.Infrastructure --configuration Release --no-build
```

`AlertsDesignTimeFactory` installs a connection interceptor refusing **all database opens**, even a `--connection` override. Supported tools generate/compare models and scripts only, without an application-file writer. `ef database update/drop`, database-connected migration removal/listing and direct script execution are not the supported lifecycle. Future migrations still require SQL inspection; table rebuild/drop/down migration or real/shared data changes require separate approval. No `EnsureCreated` is used.

## Apply explicitly, with app stopped

Only new disposable/synthetic sandbox initialization and safe reapplication were executed for BE-003. Read the generated SQL before using the command. Do not infer approval for existing real/shared data from this example.

```powershell
dotnet restore Alerts.slnx --locked-mode
dotnet build Alerts.slnx -c Release --no-restore
# Existing parent directory, a deliberate synthetic database path:
dotnet run --project src/Alerts.Api -c Release --no-build -- --migrate 'C:\sandbox\alerts.db'
```

The command checks normal disk headroom before creating the empty file, acquires lifetime ownership, admits once through the gate, refuses unknown history/nonempty unversioned databases, enables WAL, applies known forward migrations and checkpoints. Only stable User/Admin role rows are seeded, with no accounts/passwords/destinations. Migration history and provider `__EFMigrationsLock` are infrastructure tables, not business records. The provider lock is not a substitute for process ownership; do not delete provider-lock rows/files as guessed recovery. A failed operation requires investigation, not a blind retry/destructive repair.

```powershell
$env:Persistence__DatabasePath = 'C:\sandbox\alerts.db'
$env:DataProtection__KeyDirectory = 'C:\sandbox-keys\alerts'
$env:DataProtection__ApplicationName = 'Alerts.LocalSandbox'
$env:Identity__Provisioning__Accounts__0__Id = '<stable-lowercase-guid>'
$env:Identity__Provisioning__Accounts__0__Email = '<synthetic-user-email>'
$env:Identity__Provisioning__Accounts__0__Password = '<unique-synthetic-password-from-approved-secret-input>'
$env:Identity__Provisioning__Accounts__0__Role = 'User'
# Configure a separate indexed Admin account; placeholders are not literal values.
dotnet run --project src/Alerts.Api -c Release --no-build
```

Startup with a configured path requires the existing file and exact known migration history; it never migrates/creates the DB. It also requires a nonempty explicit account set containing at least one Admin and a persistent Data Protection key directory/application name. Keys are protected to the current Windows user with DPAPI and must be backed up separately; another account/machine may not decrypt them. Missing/empty/ambiguous configuration and persisted ID/email conflicts fail before account mutation. The configured set is authoritative: exact User/Admin membership is reconciled, absent accounts are disabled, and later re-adding the same stable identity re-enables its original stored password. Existing configured passwords are never reset from changed startup input; user/history/password rows are never deleted. Reconciliation is one normal-headroom admitted transaction and rolls back on failure. Already-issued tokens remain valid for at most15 minutes. Without a configured path, liveness remains available but readiness/login are503.

The configured runtime maps `POST /api/v1/auth/login`, `GET /api/v1/me`, `GET /api/v1/me/channels` and the owner-only `GET/POST /api/v1/rules`, `GET/PUT/DELETE /api/v1/rules/{id}` lifecycle. There is no register/reset/refresh/logout/user/destination-management route. Login is limited to10/minute/client IP with no queue; five failed checks lock the account for15 minutes. The access token expires after900 seconds. The framework response currently includes a refresh token field, but it is unsupported/discard-only. Never log or copy passwords, access/refresh values or DPAPI key XML into evidence.

Rule writes select only already-provisioned available channel rows; provision/revoke those rows through approved offline configuration, not public destination CRUD. Example owner calls (token and IDs are placeholders):

```powershell
curl.exe -H "Authorization: Bearer <access-token>" http://127.0.0.1:5080/api/v1/me/channels
curl.exe -X POST -H "Authorization: Bearer <access-token>" -H "Content-Type: application/json" --data '{"name":"Space updates","category":"news","filter":{"keyword":"moon"},"channels":["email"],"enabled":true}' http://127.0.0.1:5080/api/v1/rules
curl.exe -X PUT -H "Authorization: Bearer <access-token>" -H "Content-Type: application/json" --data '{"name":"Space updates","category":"news","filter":{"keyword":"moon"},"channels":["email"],"enabled":false,"version":1}' http://127.0.0.1:5080/api/v1/rules/<rule-id>
curl.exe -X DELETE -H "Authorization: Bearer <access-token>" -H 'If-Match: "2"' http://127.0.0.1:5080/api/v1/rules/<rule-id>
```

SQLite availability failures during login security bookkeeping return a fixed redacted503 and never issue a bearer. The exact latched primary categories are BUSY5, LOCKED6, READONLY8, IOERR10, FULL13 and CANTOPEN14; extended codes are reduced to their low-byte primary. ERROR1 schema/SQL, NOMEM7 process resource, CORRUPT11, CONSTRAINT19, MISUSE21 and NOTADB26 are deliberately not translated to transient `StorageUnavailable` or latched by this classifier; they remain safe500/domain/startup failures requiring diagnosis. Credential states alone use the generic401. A503 can follow an uncertain post-commit checkpoint; selected storage availability faults latch mutations/readiness unavailable for operator investigation rather than claiming rollback.

## Runtime writer foundation

- `IMutationGate` is singleton; max32 queued callers, deadline2s, nonreentrant execution-context guard, cancellation-safe release. Outer Application use cases acquire **once** and pass `MutationAdmission`; it expires on callback exit. Participants never reacquire/capture it. Create a DI scope/context/unit of work per operation; discard it after success or failure.
- `PersistenceUnitOfWork` requires that capability plus live process ownership, checks disk, binds the scoped context, starts a short transaction, saves and commits, then explicitly checkpoints before returning success. All connection strings enable FK enforcement, busy timeout1s and disable pooling. Network/SMTP/password hashing and other external work stay outside admission/transaction.
- `AlertsDbContext.SaveChangesAsync` refuses unadmitted writes; synchronous saves and physical deletion are unsupported. It guards immutable event/seen evidence, intent matching/destination/payload snapshots, destination versions, rule category and terminal attempt/intent evidence. New intents validate filter schema and referenced category/rule/destination snapshot agreement. Rule/source/intent `Version` is an application-managed concurrency token; business use cases must increment and enforce their accepted preconditions.
- Normal growth: >=1GiB **after16MiB** allowance; emergency: >=256MiB **after1MiB**. Emergency purpose is not a free-form low-space bypass: only modified intent/attempt completion/recovery fields or essential Identity counter/lockout/stamp fields qualify; creation/claims, unrelated records and password rehash require normal admission. Reads do not acquire the mutation gate.
- Headroom is measured before admission/save/commit/checkpoint, not reserved by the OS. Unknown space, relevant physical SQLite write faults and failed/busy checkpoint latch the service unavailable until operator investigation/restart. Low measured space rejects work without deleting data. Completion checks must also precede sending in BE-009.
- Emergency delivery writes must atomically finish one existing InFlight attempt for each modified InFlight intent: preserve AttemptCount/claim ordinal, verify original and persisted token/lease/version evidence, clear the intent claim/lease and increment its Version once. Delivered requires Succeeded; RetryScheduled requires TransientFailure/OutcomeUnknown before attempt5; Failed requires PermanentFailure, or TransientFailure/OutcomeUnknown at attempt5. New claims, Pending/RetryScheduled transitions, in-place claim rotation, unpaired or fabricated tracked claims and new attempt rows are refused. Recovery lease-expiry/local-operation coordination still belongs to BE-009.
- A precommit exception/cancellation rolls back. A checkpoint error **after commit** is an uncertain acknowledgement: caller receives no success, stored data can already exist, readiness/writes latch off. Never pretend that commit rolled back; later acceptance retries must retain their stable idempotency key. Tests exercise both cases without filling a real disk.
- Identity and rule writers share the admitted scoped context. Rule create/activation enforces active enabled+undeleted limits of100 global/20 per owner under the same outer gate; paused rules do not count. Acceptance fan-out200/outstanding20,000/retained250,000 caps and attempt/token state-machine cross-row consistency belong to later Application use cases. No public repository or destination CRUD is introduced. Raw SQL/external writers are not alternative supported mutation paths.

## Backup, restore and evolution (instructions, not executed evidence)

No backup/restore/provisioning CLI is implemented in BE-003. Before touching an existing real DB, obtain separate authorization, stop all writers and use a reviewed maintenance process that holds the same owner throughout a consistent SQLite backup/checkpoint operation. Do **not** copy only the main DB from a running or crash-left WAL database; do not delete WAL/SHM/journal. Use the SQLite backup API or a stopped, successfully checkpointed and closed file set with integrity/FK checks, recorded migration history/counts and a rehearsal on another disposable location. Keep backups outside the DB directory and protect PII.

Restoring/replacing real files, applying Down migrations or accepting data loss requires separate Level3 approval. Stop all writers and retain exclusive maintenance ownership while restoring the reviewed consistent set; verify schema, rows/FKs/canonical representations and pending claims before resuming. Preserve Data Protection keys/DPAPI identity/configuration/secrets separately: copying SQLite is not key/credential portability and does not guarantee bearer continuity. No restore drill, hardware power-loss durability, disk-exhaustion test or production backup guarantee is claimed.

PostgreSQL remains the dedicated, separately approved provider/migration/export-import path in the canonical schema. No Npgsql provider, PostgreSQL install, connection-string-only migration or real-data cutover was performed.
