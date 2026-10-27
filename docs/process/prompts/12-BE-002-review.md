# BE-002 focused review prompts

## Code review — exact prompt
```text
Brief changes-only read-only review BE002 vs HEAD25c9bb4 C:\Repos\Sonrisa-Task04. Focus new Domain8files and 3tests/project ref, evidence docs/process/build/BE-002.md, typed rule/payload matching, exact numeric/time boundaries, stable identity/fingerprint, Unicode/URL normalization. No broad audits, no edits. User wants fast development with docs retained. Backend99/99tests buildzero warnings. Return concise verdict and genuine correctness must-fix with paths. Exclude unrelated doc corrections. Use current approved contracts not historical variants.
```

Actual result: Changes requested. Found a false market match from decimal multiplication rounding at approved bounds; checked arithmetic catches overflow but not precision loss. Reproducer open999999999999.99999999, last100, threshold99.99999999 should not match. Backend correction delegated; independent re-review required.

## Focused input-boundary audit — exact prompt
```text
Tiny changes-focused read-only security check of BE002 new Domain files in C:\Repos\Sonrisa-Task04: untrusted input numeric/time/text/url constraints, stable actor/source identities and fingerprint encoding. Pure normalized text, no HTTP/parser/auth yet; no AngleSharp or network installed. Do not demand later-chunk ingress/auth/rendering controls in pure domain. Reference approved contracts. Return concise genuine blockers/shouldfix and limits; no code edits/tests/commits. User wants fast verification, don't expand audit.
```

Actual result: Approved pure-domain changes, no security blocker/should-fix. Static audit did not independently reproduce decimal extreme precision; code-review finding still governs correctness. No ingress/auth/parser/rendering/persistence claims inferred.

## Correction-cycle limit
This is correction cycle1 of maximum3; never overwrite the finding history with a final clean verdict or claim the initial99 passing tests caught this boundary.
