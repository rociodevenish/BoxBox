# 02: Resolve spatial catches and restricted following

**What to build:** Cars gain and lose locally, catch only within valid trajectory bounds and follow restricted sections without overlap.

**Blocked by:** 01: Run a deterministic segmented solo race

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Bound predictions by the next segment exit of either car and recalculate affected predictions at trajectory-changing events.
- [x] Verify local corner gains and straight losses, a catch before a boundary and a projected catch invalidated by a boundary.
- [x] Resolve queues involving different segments and multiple opponents without overlap.
- [x] Preserve physical cumulative distance, normalized Classification progress and trajectory-derived time gaps in full races.


## Answer

Implemented physical cumulative metre trajectories, public-seam catch prediction limited by both cars' next exits, and restricted following chains. Recalculation preserves same-timestamp pending catches and follows across segment/lap/phase boundaries. Starting gaps are explicit departure events; colocated starts retain stable order. Classification uses normalized progress and trajectory-derived gaps, with precision-aware tie handling for queued finishes.

Evidence: seven public contract/race tests cover local corner gains/straight losses, invalidated catches beyond a boundary, restricted catches, three-car simultaneous queues, colocated starts, cars in different segments, and full 22-car 12-lap replay. Full suite: 58 passing tests. Passing opportunity outcome resolution remains ticket 03.
