# 01: Run a deterministic segmented solo race

**What to build:** A designer runs a complete solo race on ordered track segments through the laboratory and inspects spatial progress and tyre behaviour.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [ ] Validate nonempty ordered segments with stable identities, positive finite lengths/times, bounded independent Speed/Cornering demands, tyre demand and passing configuration before mutation; document units.
- [ ] Expose a pure public segment simulation seam shared by engine and diagnostics with immutable effective inputs and boundary-limited optional catch prediction.
- [ ] Integrate segment exits, lap crossings and five-leader-lap Race Phase boundaries while preserving simultaneous decisions and complete-race finishes.
- [ ] Distribute driver Pace, tyre penalties and Pace Instruction effects by baseline time share; integrate wear by distance/demand and warm-up across the out-lap.
- [ ] Verify solo races, phase changes inside segments, equivalent subdivision and one-segment aggregate compatibility with new variability disabled.

