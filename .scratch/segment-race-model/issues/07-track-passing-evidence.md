# 07: Measure track fit and passing frequency

**What to build:** Reproducible evidence shows how track demands and passing zones affect local gains, queues, passes and strategy conclusions.

**Blocked by:** 06: Publish versioned replay and paired diagnostics

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Provide equal and mixed 22-car fixtures on two layouts with comparable lap duration and different demands/zones, including corner-strong and straight-strong cars.
- [x] Publish zero-noise controls, baseline-versus-segment comparisons and bounded passing difficulty sweeps.
- [x] Use at least 20 published paired seeds for principal claims, means, sample variability, distance/eligibility groups and failed/inconclusive hypotheses.
- [x] Record all chosen settings and inspectable segment/encounter histories; no real-world calibration claim.

## Answer

Implemented `--suite track-passing`: 20 preselected cases, 40 laps, 22 cars, paired seeds 7–26, 800 completed races. Published [readable evidence](../evidence/track-passing.md) and [machine summary](../evidence/track-passing.json) include all settings, explicit model/channel versions, sample variability, eligibility/distance groups, zero-noise and aggregate controls. Reproduction and history exports are documented in `docs/segment-experiments.md`; ignored reports contain replay inputs and final segment/encounter histories.

At difficulty 0.4, equal-all baseline actual Overtake means are 0.95 open (SD 0.83, range 0–2) and 0.50 restricted (SD 0.69, range 0–2). Early-stop means are 2.15 and 1.15. The paired layout difference interval includes zero, so that equal-all hypothesis is inconclusive. Mixed-car-fit baseline means are 39.60 open and 30.20 restricted; these artificial car strengths differ from equal-all. Attempts, queue loss, blue flags and pit-cycle position outcomes are published separately. All selected difficulty means decrease; coefficients remain hypotheses rather than calibration.

TDD added public clear-air local-gain/equal-lap behavior and four full-field legal-race cases. Full suite: 98 passed. Runner smoke: passed, including actual exact race/comparison replay and invalid suite requests rejected before outputs. All 20 cases and each counter distribution verified to contain 20 samples. Three complete pre-optimization histories are byte-identical after cached free projection, Release execution and bounded two-case concurrency. The cache refreshes at existing effective-input transitions through the shared SegmentSimulation seam; repeated projection work motivated it, while ticket 10 owns formal runtime measurement. No seed or selected case was omitted.

