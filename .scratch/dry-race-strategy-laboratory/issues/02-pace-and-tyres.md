# 02: Make pace and tyre choices affect stint performance

**What to build:** A designer can run different compounds and Pace Instructions and observe their immediate pace and later tyre consequences in a complete race.

**Blocked by:** 01 — Run a deterministic race from fixed starting conditions.

**Status:** ready-for-agent

- [ ] Soft, Medium and Hard have tunable pace, warm-up and degradation characteristics, with unlimited fresh sets.
- [ ] Conserve sacrifices immediate pace for reduced degradation; Push gains immediate pace at increased degradation; Standard is the baseline.
- [ ] Instructions apply throughout the Race Phase. Preserve this duration when pit stops are added.
- [ ] Car tyre preservation and driver tyre management affect degradation; stronger relevant attributes help across repeated runs.
- [ ] Tyre age and actual condition evolve coherently; exhausted tyres incur a steep pace penalty.
- [ ] Readable and structured reports expose phase tyre histories and pace effects for diagnosis.
- [ ] Reproducible scenarios demonstrate an early Push gain that can be offset by later tyre costs, rather than requiring every Push strategy to lose.
- [ ] Tests at the public simulation boundary cover compound differences, warm-up, instruction duration, degradation and exhaustion. Component wear, incidents and punctures remain excluded.
