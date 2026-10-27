# ADR-004: Provisioned Identity with login-only opaque bearer
Status: Proposed

## Context
Real auth, seeded roles and API clients; registration/recovery/management excluded.
## Decision proposal
Framework Identity password hashing/stores and opaque bearer protection. Explicit login only; no blanket MapIdentityApi. Proposed 15-minute access token, no refresh endpoint. Built-in bearer response includes unused refresh field: document discard-only and test route absence. Persist/protect Data Protection keys. Admin never bypasses rule ownership.
## Alternatives considered
- External OIDC: mature sessions/federation but another service/configuration burden.
- Cookie sessions: appropriate future browser option but no frontend currently; would require CSRF/session decisions.
- Homemade JWT issuer: needless cryptographic/session implementation risk.
## Consequences
Re-login after expiry; role/account changes do not instantly revoke issued claims. Do not promise logout revocation. Lockout/login rate limiting required. Seed credentials outside repo; no default password.
## Approval gate
BE-004; explicitly accept token lifetime/unused refresh behavior and no immediate revocation. Security review required.
## Revisit
Public deployment, browser UI, federation, immediate revocation or a requirement forbidding unused refresh fields.
