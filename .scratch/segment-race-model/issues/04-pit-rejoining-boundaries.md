# 04: Resolve pit rejoining and race-boundary events

**What to build:** Stops rejoin real spatial traffic and complete segmented races retain coherent phase and finish semantics.

**Blocked by:** 03: Resolve eligible passing opportunities

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Queued Pit Instructions take effect at next lap end, retain Pit Loss and stationary service, and exit at start/finish.
- [x] Resolve exit traffic spatially and retain Pace Instructions after stops.
- [x] Drain all events at a Race Phase boundary timestamp before accepting decisions, with documented stable tie handling.
- [x] Verify lapping, simultaneous exits/crossings, stops, eligibility, chequered flag and complete 22-car races.


## Answer

Implemented pit coordinate history and physical rejoin contact handling, including different completed laps. Rejoining yields behind cars already at start/finish; contact order tracks the new defender after a stop. All same-timestamp trajectory events drain before entry opportunities are calculated, preventing overtakes against obsolete segment speed. Multi-opponent queue contact processing retains distinct encounters after simultaneous transitions. Pit instructions retain next-driver-crossing semantics, lane/service loss, compound eligibility and Pace after stops.

Evidence: full suite passes 77 tests. Nine segmented pit/boundary tests cover next-lap stops and Push persistence, clear-air gain lost to real rejoin traffic, coincident pit exit/crossing, simultaneous trajectory updates, phase boundary drain, a mixed positive-attribute 22-car race with lapping/22 stops/eligibility/monotonic timestamps/exact replay, pit exit beside a four-lap-ahead car at a phase boundary, simultaneous lapped chequered crossings, and invalid-decision rejection before mutation. Boundary/rejoin semantics documented in docs/segment-simulation.md.
