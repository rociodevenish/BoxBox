# 08: Demonstrate and explain the three strategy stories

> GitHub issue: [#20](https://github.com/rociodevenish/BoxBox/issues/20) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A designer can run a documented set of experiments and inspect evidence that pace, tyres, stops and traffic produce meaningful strategic trade-offs.

**Blocked by:** 05 — Influence nearby battles with Attack and Defend; 07 — Compare strategies across paired seeds.

**Status:** resolved

- [x] Integrate all earlier behaviours in complete 22-car races with both field fixtures and both track profiles.
- [x] Supply reproducible examples and bounded multi-seed comparisons demonstrating Push's early gain and possible later tyre cost, fresh tyres repaying Pit Loss, and traffic or track characteristics changing strategy performance.
- [x] Use the comparison runner to support claims; report sample counts, seed lists, tuning, variability and eligibility rather than relying on one lucky outcome.
- [x] Numerical coefficients remain explicitly initial hypotheses. Record experimental tuning and findings without claiming final balance or real-world calibration.
- [x] Complete readable and machine-readable histories cover phase gaps, tyres and estimates, stops, passes, traffic/Pit Loss, decisions, final Classification and replay inputs.
- [x] Explain supported observable outcomes, including when a fresh-tyre gain repays stop cost. Separate observations from causal comparison findings.
- [x] Document one-command race execution, comparison execution and report interpretation, with no cloud prerequisites.
- [x] Run the rule checks and runner smoke checks across the integrated model, including phase timing, lapping, finish handling, information boundaries and replay.
- [x] Record remaining tuning questions and limitations for the subsequent browser playtest. No browser, cloud or season systems are required for this ticket.

Validation: 42 tests passed; runner smoke checks passed; seven comparisons across 20 paired seeds plus integrated fixtures and benchmarks completed 294 races. Spec and standards reviews found no remaining blocking findings. Evidence and replay summaries are saved alongside the spec.
