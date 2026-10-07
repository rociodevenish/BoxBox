# 02: Resolve spatial catches and restricted following

**What to build:** Cars gain and lose locally, catch only within valid trajectory bounds and follow restricted sections without overlap.

**Blocked by:** 01: Run a deterministic segmented solo race

**Status:** ready-for-agent

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [ ] Bound predictions by the next segment exit of either car and recalculate affected predictions at trajectory-changing events.
- [ ] Verify local corner gains and straight losses, a catch before a boundary and a projected catch invalidated by a boundary.
- [ ] Resolve queues involving different segments and multiple opponents without overlap.
- [ ] Preserve physical cumulative distance, normalized Classification progress and trajectory-derived time gaps in full races.

