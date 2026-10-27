# 06: Publish versioned replay and paired diagnostics

**What to build:** A designer reproduces segmented races and compares strategies using inspectable histories and valid outcome groups.

**Blocked by:** 04: Resolve pit rejoining and race-boundary events; 05: Add controlled driver form and named random channels

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Publish readable and machine outputs and replay inputs with explicit track/tuning, drivers, strategy, seed and model/randomness contract versions.
- [x] Add segment identities, spatial positions and encounter identities to diagnostic histories without leaking randomness into player event flows.
- [x] Retain corresponding exogenous draws across paired runs; document strategy-dependent stop/encounter identities and opponent outcomes.
- [x] Separate position differences from eligible equal-distance time differences and report distance/eligibility groups, sample counts and variability.
- [x] Runner smoke tests cover validation, report outputs and exact replay; never claim cross-version historical seeded equivalence.


## Answer

Published self-contained versioned race and paired replay inputs, channel metadata and designer-only resolved channel diagnostics. Runner --replay accepts exported race-replay.json or comparison-replay.json, rejects incompatible/missing contracts and malformed inputs before writing reports, and preserves exact ordered outputs. Replay snapshots explicit tuning, layout, drivers, strategy arrays/dictionaries and seeds. Machine/readable histories include segment/distance/encounter information; player events and team observations carry no raw draws or cached future passing success. Existing eligibility/distance groups retain separate position and eligible equal-distance time statistics with counts/sample variability.

Evidence: full suite passes 93 tests, including four replay contract tests. tools/verify-runner.ps1 passes aggregate and segmented runner smoke checks, full22-car spatial/encounter output, byte-identical race and paired replay, and rejection of future model/randomness versions, omitted seed and invalid layout without report creation. Documentation: docs/laboratory-replay.md. Generated full-history reports remain ignored and are not committed.
