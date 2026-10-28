# 07: Compare strategies across paired seeds

> GitHub issue: [#19](https://github.com/rociodevenish/BoxBox/issues/19) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A designer can compare two strategies for one driver over matching seeds and inspect paired results and variation through the local runner.

**Blocked by:** 06 — Run conditional strategies using team information.

**Status:** resolved

- [x] A documented comparison command changes one driver's strategy while holding initial state, track, tuning and opponent scripts fixed.
- [x] Fixed opponent conditional rules may react differently to changed race conditions; reports explain this distinction.
- [x] Each alternative uses the same seed list. Extra stops or other events do not shift unrelated random draws; distinguish repeated occurrences within a driver/lap/event identity when needed.
- [x] Readable and structured reports preserve per-seed paired results and summarize finishing position, elapsed time and variability.
- [x] Preserve eligibility and completed distance, distinguishing ineligible runs and differently lapped finishes rather than averaging them as equivalent race times.
- [x] Replay metadata includes strategies, field, track, tuning and seeds.
- [x] Causal strategy claims require paired-comparison evidence; individual reports describe observed outcomes.
- [x] Tests verify pairing, fixed input preservation, stable random identities, repeatability, aggregation and distance/eligibility handling through existing public interfaces.
