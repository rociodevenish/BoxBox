# 08: Measure tyre and pit-window sensitivity

> GitHub issue: [#82](https://github.com/rociodevenish/BoxBox/issues/82) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Designers inspect how fresh-tyre payoff and stop timing vary across layouts and fields.

**Blocked by:** 06: Publish versioned replay and paired diagnostics

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Choose and record bounded degradation, exhaustion and warm-up sweep ranges before interpreting results.
- [x] Sweep multiple pit windows on both layouts and equal/mixed fields rather than one selected stop lap.
- [x] Publish reproducible settings and at least 20 paired seeds for principal claims with sample variability, eligibility/distance groups and failed/inconclusive outcomes.
- [x] Explain sensitivity using spatial traffic histories without adopting new tyre curve families or final balance.

## Answer

Implemented `--suite tyre-pit` with 36 preselected cases: seven one-factor tyre settings across both 90-second layouts and both 22-car fields, plus legal phase-3/5 target stops against phase-4 baseline stops. Paired seeds 7–26, 40 laps, 1,440 completed races. Published [readable evidence](../evidence/tyre-pit.md) and [machine summary](../evidence/tyre-pit.json) include explicit full tuning, variability, model/channel versions, every eligibility/distance group, target traffic loss, out-lap duration and matching-crossing repayment/unrepaid seeds. Replay inputs and final spatial histories are regenerable in ignored reports; `docs/segment-experiments.md` records the command and bounded ranges.

Default fresh-stop diagnostic controls are slower on average: open equal-all +3.87s and restricted equal-all +6.12s; open mixed-car-fit +2.23s and restricted mixed +5.22s. High degradation reverses open mixed payoff to −3.15s (SD 4.26) while restricted mixed remains +2.98s (SD 2.96). These no-stop baselines are ineligible; raw equal-distance payoff is explicitly separate from eligible legal strategy outcomes. Legal equal-all early stops are about +2.24s slower, late stops about +9.14s slower. Position changes and time differences remain distinct metrics.

Both layouts have distance-weighted TyreDemand 1.0 and different local demands/zones. Spatial histories expose substantial following loss after rejoin; seed 7 restricted/high-degradation gives target traffic loss 416.91s after stopping despite ten actual passes, versus 0.38s without the stop. Exhaustion-off/high at default degradation reaches zero condition only at the final crossing, so its neutral result leaves exhaustion sensitivity inconclusive. Warm-up response need not be monotonic when both initial tyres and encounters change. No new curves or final tuning adopted.

Validation: unsupported suite request observed red at the runner seam before implementation; implemented suite enforces at least 20 seeds before writing. Full test suite 98 passed; actual runner smoke passed, including exact race/comparison replay and tyre-suite sample-size rejection. All 36 cases verified with 20 race samples per counter and group sample totals of 20. Corrected ticket 07's mixed-field label to explicitly include varied base driver Pace; its measured metrics remain unchanged.
