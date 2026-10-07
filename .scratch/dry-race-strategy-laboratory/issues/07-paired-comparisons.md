# 07: Compare strategies across paired seeds

**What to build:** A designer can compare two strategies for one driver over matching seeds and inspect paired results and variation through the local runner.

**Blocked by:** 06 — Run conditional strategies using team information.

**Status:** ready-for-agent

- [ ] A documented comparison command changes one driver's strategy while holding initial state, track, tuning and opponent scripts fixed.
- [ ] Fixed opponent conditional rules may react differently to changed race conditions; reports explain this distinction.
- [ ] Each alternative uses the same seed list. Extra stops or other events do not shift unrelated random draws; distinguish repeated occurrences within a driver/lap/event identity when needed.
- [ ] Readable and structured reports preserve per-seed paired results and summarize finishing position, elapsed time and variability.
- [ ] Preserve eligibility and completed distance, distinguishing ineligible runs and differently lapped finishes rather than averaging them as equivalent race times.
- [ ] Replay metadata includes strategies, field, track, tuning and seeds.
- [ ] Causal strategy claims require paired-comparison evidence; individual reports describe observed outcomes.
- [ ] Tests verify pairing, fixed input preservation, stable random identities, repeatability, aggregation and distance/eligibility handling through existing public interfaces.
