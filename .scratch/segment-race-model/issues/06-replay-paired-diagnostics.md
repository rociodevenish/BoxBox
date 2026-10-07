# 06: Publish versioned replay and paired diagnostics

**What to build:** A designer reproduces segmented races and compares strategies using inspectable histories and valid outcome groups.

**Blocked by:** 04: Resolve pit rejoining and race-boundary events; 05: Add controlled driver form and named random channels

**Status:** ready-for-agent

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [ ] Publish readable and machine outputs and replay inputs with explicit track/tuning, drivers, strategy, seed and model/randomness contract versions.
- [ ] Add segment identities, spatial positions and encounter identities to diagnostic histories without leaking randomness into player event flows.
- [ ] Retain corresponding exogenous draws across paired runs; document strategy-dependent stop/encounter identities and opponent outcomes.
- [ ] Separate position differences from eligible equal-distance time differences and report distance/eligibility groups, sample counts and variability.
- [ ] Runner smoke tests cover validation, report outputs and exact replay; never claim cross-version historical seeded equivalence.

