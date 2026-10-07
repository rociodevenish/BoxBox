# 01: Run a deterministic segmented solo race

**What to build:** A designer runs a complete solo race on ordered track segments through the laboratory and inspects spatial progress and tyre behaviour.

**Blocked by:** None (can start immediately)

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Validate nonempty ordered segments with stable identities, positive finite lengths/times, bounded independent Speed/Cornering demands, tyre demand and passing configuration before mutation; document units.
- [x] Expose a pure public segment simulation seam shared by engine and diagnostics with immutable effective inputs and boundary-limited optional catch prediction.
- [x] Integrate segment exits, lap crossings and five-leader-lap Race Phase boundaries while preserving simultaneous decisions and complete-race finishes.
- [x] Distribute driver Pace, tyre penalties and Pace Instruction effects by baseline time share; integrate wear by distance/demand and warm-up across the out-lap.
- [x] Verify solo races, phase changes inside segments, equivalent subdivision and one-segment aggregate compatibility with new variability disabled.


## Answer

Implemented ordered validated layouts, immutable engine layout snapshots, the shared pure `SegmentSimulation` public seam with bounded catch prediction, segment exit scheduling/history and cumulative distance-derived Classification. Solo races preserve five-leader-lap phase boundaries, pace changes inside segments, distributed warm-up and distance/demand wear. Aggregate inputs retain their compatibility path; equivalent explicit one-segment clear-air controls match aggregate outputs.

Evidence: `dotnet test --no-restore` passed all 51 tests, including 9 segment contract/integration tests. Validation covers empty layouts, identity collisions, nonfinite values/totals, independent demands and snapshot stability. Documentation: `docs/segment-simulation.md`. Spatial following and passing remain assigned to dependent tickets 02–03.
