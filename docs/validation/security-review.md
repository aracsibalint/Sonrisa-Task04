# Audit Report: Alerts MVP design

## Verdict: Changes requested
Initial independent read-only security review. No demonstrated exploitable implementation vulnerability: no application exists. User approval gates G-001–G-005 remain separate from design findings.

## Findings
### Blocking
No demonstrated security vulnerability classified Blocking. Technical/provider approval gates remain prerequisites for affected builds/live enablement.

### Should-fix
| ID | Finding and evidence | Required correction |
|---|---|---|
| S1 | Overview/active ADRs specify 15-minute access expiry; API/security draft specifies 30 minutes. Limits, retry/state names and older duplicate-ID ADRs also conflict. | Align all canonical files; mark historical ADRs superseded; one contract for token exposure, delivery and resource bounds. |
| S2 | All-writer gate includes Identity, but exact acquisition owner/DI boundary unspecified. Nested acquisition could stall writes; separate gate instances would break ordering. | Singleton gate, one outer owner per mutation, no nested acquisition; scoped stores/context not captured; include failed counts, resets, rehash and provisioning. Verify password outside transaction and revalidate under gate before sign-in decision. |
| S3 | "Refuse second instance" lacks a mechanism/target-platform contract. Different textual paths could create distinct locks for the same DB. | Explicit local platform support, stable database deployment identity, OS-backed exclusive lifetime ownership acquired before all writers; crash release and alias/startup tests. Avoid thread-affine mutex across asynchronous execution unless explicitly handled. |
| S4 | Intent counts do not bound zero-match events, seen keys, soft-deleted rules or failure history. Disk exhaustion can block security bookkeeping and send-result persistence. | Explicit disk-pressure admission threshold/reserve, monitoring of all growing tables, fail-closed storage behavior; no automatic deletion/replay weakening. |

### Nit / optional
None. Do not expand a local sandbox MVP into production architecture to close these findings.

## Checklist summary
Ownership/admin separation, framework crypto, HMAC freshness plus durable dedup, fixed outbound destinations, safe XML/content escaping, secret references, no public Identity endpoint bundle and honest uncertain-send behavior are appropriate proposals. Implementation/dependency vulnerability, actual TLS/bind/key permissions and runtime enforcement remain untested. Sandbox exceptions must be narrow and synthetic; provider permission is not supplied by a security review.

## What was NOT reviewed (scope limits)
No code, endpoints, installed packages, generated SQL, tests, exploits, infrastructure bindings, file permissions, backups, capacity measurement or provider legal clearance. Companion API/schema/security files were compared for alignment, not initially approved. Full raw review is in the session conversation; this saved report is a faithful finding summary, not a verbatim transcript.

## Correction and re-review
Initial report recorded. S1–S4 correction evidence and independent re-review verdict will be appended; no closure is claimed by this initial record.
