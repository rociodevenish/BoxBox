# 05: Add controlled driver form and named random channels

> GitHub issue: [#79](https://github.com/rociodevenish/BoxBox/issues/79) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Designers isolate persistent driver form from lap noise and execution/information uncertainty without recalculation affecting outcomes.

**Blocked by:** 01: Run a deterministic segmented solo race

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Sample bounded zero-centred form once per driver/race in seconds per lap; default to zero and retain immutable base attributes.
- [x] Cache bounded lap variation once per driver/lap, using full-range magnitude and baseline-time distribution across segments.
- [x] Key named channels by seed and semantic driver/race, driver/lap, driver/stop, pair/opportunity and driver/Race Phase identities; no global stream or calculation-count keys.
- [x] Allow independent zeroing and validate magnitudes; document distributions, scopes and consumers.
- [x] Preserve bounded clamped estimate errors and repeated observation stability; exclude hidden form, exact hidden tyre condition, seeds and future draws from observations.
- [x] Verify replay, persistent form, changing lap noise, recalculation stability and corresponding paired exogenous draws.


## Answer

Implemented named pure scoped channels for form, cached per-lap variation, service, encounter passing and phase estimate error. Added RaceFormVariation (default zero) and PassingVariation (default one), independently zeroable with existing channel magnitudes. Form remains fixed per driver/race, base attributes remain immutable, and pre-resolved offsets reach the public segment seam. Segmented pit service uses driver/ordinal stop identity; aggregate compatibility retains the historical completed-stop-lap scope. PassingVariation zero uses a documented fixed 0.5 threshold while retaining chance tuning. Added public SimulationVersions constants and docs/scoped-randomness.md describing distributions, scopes, consumer units, compatibility and paired limits.

Evidence: full suite passes 89 tests. Twelve public behavior tests cover bounded persistent form, changing bounded lap offsets and subdivision, shifted corresponding stop identity, independent zero passing controls, stable bounded/clamped observations without hidden fields, magnitude validation, paired exogenous offsets under changed phase timing and extra-observation replay stability. Existing aggregate compatibility and all segmented race tests remain green.
