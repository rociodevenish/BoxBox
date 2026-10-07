# 05: Add controlled driver form and named random channels

**What to build:** Designers isolate persistent driver form from lap noise and execution/information uncertainty without recalculation affecting outcomes.

**Blocked by:** 01: Run a deterministic segmented solo race

**Status:** ready-for-agent

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [ ] Sample bounded zero-centred form once per driver/race in seconds per lap; default to zero and retain immutable base attributes.
- [ ] Cache bounded lap variation once per driver/lap, using full-range magnitude and baseline-time distribution across segments.
- [ ] Key named channels by seed and semantic driver/race, driver/lap, driver/stop, pair/opportunity and driver/Race Phase identities; no global stream or calculation-count keys.
- [ ] Allow independent zeroing and validate magnitudes; document distributions, scopes and consumers.
- [ ] Preserve bounded clamped estimate errors and repeated observation stability; exclude hidden form, exact hidden tyre condition, seeds and future draws from observations.
- [ ] Verify replay, persistent form, changing lap noise, recalculation stability and corresponding paired exogenous draws.

