# 01: Run a deterministic race from fixed starting conditions

**What to build:** A designer can run a complete local race from fixed starting conditions and inspect reproducible Classification and finish results.

**Blocked by:** None (can start immediately).

**Status:** resolved

## Completion evidence

Six public-boundary checks pass, including exact phase gaps and simultaneous lapped finishes. A seven-lap 22-car runner smoke check produced both report formats and two phase snapshots. Standards review found one naming issue; Spec review found gaps, simultaneous finish, report and validation issues, all addressed before completion.

## Context

First slice of the dry-race strategy laboratory. Preserve the separate Game.Domain, Game.Simulation and SimulationRunner responsibilities and infrastructure-independent simulation. The approved primary test boundary accepts initial state, tuning, seed and phase decisions and returns authoritative state and events; compose phases for complete-race tests. Include one small runner smoke check.

- [ ] One documented local command runs a race without cloud services.
- [ ] Provide equal-performance and mixed-performance fixed rosters of 11 teams / 22 cars, rolling order, explicit gaps and fresh starting tyres.
- [ ] Provide two contrasting track profiles and configurable race distance and numerical tuning, labelled as initial hypotheses.
- [ ] Initial pace uses car speed and cornering, driver pace and track performance weights. Stronger relevant attributes help in controlled comparisons.
- [ ] A shared race clock tracks each driver's actual progress. Normal boundaries follow five leader laps; the final phase is shorter when needed.
- [ ] Classification is unique and complete, ordered by completed laps and progress, with coherent time gaps and lap gaps.
- [ ] The leader finishes at race distance; other drivers finish at their next crossing, retaining completed laps and finish time. Equal-time cases have stable ordering.
- [ ] Identical inputs, tuning and seed reproduce state and ordered event history. Random draws use stable driver/lap/event identities rather than a globally consumed stream.
- [ ] Invalid initial state and configuration produce clear errors.
- [ ] Readable and structured reports retain replay inputs, phase gaps and final results.
- [ ] Public-boundary tests cover phase timing, progress, Classification, replay and lapped finishes. Later slices add strategic effects; this slice does not claim strategy validation.
