# 09: Measure persistent form and lap-noise effects

> GitHub issue: [#83](https://github.com/rociodevenish/BoxBox/issues/83) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Designers distinguish sustained driver performance from independent lap noise and assess strategy uncertainty.

**Blocked by:** 06: Publish versioned replay and paired diagnostics

**Status:** resolved

## Context

Implement the approved segment race model spec. Test through the confirmed public segment simulation seam, race/phase outputs, paired comparison runner and runner smoke boundary. Preserve locked domain decisions and infrastructure-independent simulation.

- [x] Publish zero-noise, form-only, lap-noise-only and combined experiments with bounded ranges and all selected settings.
- [x] Use at least 20 published paired seeds for principal claims and preserve corresponding exogenous draws.
- [x] Report sustained within-driver performance and strategy distributions, sample uncertainty, eligibility/distance groups and inconclusive hypotheses.
- [x] Explain that within-race persistence does not imply shared team/field factors or form across weekends.

## Answer

Implemented `--suite form-noise`: 20 preselected layout/field/profile cases, paired seeds 7–26, 40 laps and 22 cars, 800 completed races. Full form/lap ranges in seconds are zero 0/0, form-only 0.4/0, lap-only 0/0.2, combined 0.4/0.2 and combined-high 0.8/0.4. Other variability channels are zeroed to isolate modifiers; tyres/spatial traffic stay active. Legal all-driver phase-4 Hard stops are paired against driver-06's phase-3 stop. Explicit tuning, versions and fields/plans are published in [machine summary](../evidence/form-noise.json), with [readable evidence](../evidence/form-noise.md). Replay inputs and final spatial/diagnostic histories are regenerable in ignored reports.

Diagnostics pool adjacent effective modifier pairs from the same driver/race, with one correlation per independent seed. Form-only pooled correlation is 1.000; lap-only −0.011 (sample SD 0.038); combined 0.783 (SD 0.046). Individual constant series and constant driver-centered modifiers have undefined correlation, explicitly null/n/a. Driver-centering leaves combined/lap-only correlation −0.032 (SD 0.039). Mean within-driver temporal SD is numerically zero for form-only and 0.058 seconds for lap-only/combined; doubling both full ranges doubles temporal SD while preserving the relative persistence. Elapsed-lap SD includes traffic, tyres and Pit Loss and is labelled a confounded outcome.

Normal combined profile strategy time intervals cross zero in every layout/field, so those mean effects remain inconclusive. Higher combined variability changes the strategy distributions and encounter sets, with earlier-stop mean time gains in this sample; it is not a final tuning recommendation. Every eligibility/distance group, sample variability, per-seed persistence result, actual pass/attempt/traffic/blue-flag counter and matched/unmatched exogenous scope count is published. Exact corresponding diagnostic values are verified within paired plans; all mismatches are zero. Persistence belongs to one driver in one race, with no shared team/field latent factor or carryover across weekends. Mixed-car-fit explicitly includes varied base driver Pace.

Validation: observed unsupported CLI suite red before implementation; actual 20-case sweep completed and validated with 20 samples per counter/persistence series and group totals 20. Full test suite 98 passed. Runner smoke passed, including exact replay and form-suite sample-size rejection before reports. No simulation formula or base driver attribute changed.
