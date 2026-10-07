# 04: Resolve pit rejoining and race-boundary events

**What to build:** Stops rejoin real spatial traffic and complete segmented races retain coherent phase and finish semantics.

**Blocked by:** 03: Resolve eligible passing opportunities

**Status:** ready-for-agent

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [ ] Queued Pit Instructions take effect at next lap end, retain Pit Loss and stationary service, and exit at start/finish.
- [ ] Resolve exit traffic spatially and retain Pace Instructions after stops.
- [ ] Drain all events at a Race Phase boundary timestamp before accepting decisions, with documented stable tie handling.
- [ ] Verify lapping, simultaneous exits/crossings, stops, eligibility, chequered flag and complete 22-car races.

