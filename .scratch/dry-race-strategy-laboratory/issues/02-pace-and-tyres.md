# 02: Make pace and tyre choices affect stint performance

> GitHub issue: [#14](https://github.com/rociodevenish/BoxBox/issues/14) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A designer can run different compounds and Pace Instructions and observe their immediate pace and later tyre consequences in a complete race.

**Blocked by:** 01 — Run a deterministic race from fixed starting conditions.

**Status:** resolved

- [x] Soft, Medium and Hard have tunable pace, warm-up and degradation characteristics, with unlimited fresh sets.
- [x] Conserve sacrifices immediate pace for reduced degradation; Push gains immediate pace at increased degradation; Standard is the baseline.
- [x] Instructions apply throughout the Race Phase. Preserve this duration when pit stops are added.
- [x] Car tyre preservation and driver tyre management affect degradation; stronger relevant attributes help across repeated runs.
- [x] Tyre age and actual condition evolve coherently; exhausted tyres incur a steep pace penalty.
- [x] Readable and structured reports expose phase tyre histories and pace effects for diagnosis.
- [x] Reproducible scenarios demonstrate an early Push gain that can be offset by later tyre costs, rather than requiring every Push strategy to lose.
- [x] Tests at the public simulation boundary cover compound differences, warm-up, instruction duration, degradation and exhaustion. Component wear, incidents and punctures remain excluded.
