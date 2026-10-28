# 05: Offer Assisted and Guided decisions

> GitHub issue: [#50](https://github.com/rociodevenish/BoxBox/issues/50) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Each player receives the selected help level with understandable, executable two-car advice based only on their team's legitimate information.

**Blocked by:** 02 — Race against fair, observation-driven AI; 03 — Continue through deadlines and missed decisions, for the included timeout auto-accept flow.

**Status:** resolved

- [x] Build structured observation-driven recommendations with plain-language reasons, phase/freshness and uncertainty; repeated reads/reconnect/recovery do not reroll advice.
- [x] Standard retains all legal choices without strategic recommendations. Assisted retains all choices and highlights/explains the suggested plan.
- [x] Guided may reduce a choice group to two sensible options only when confidence supports it; otherwise retain legal options and explain forced single-option states.
- [x] Advice respects compound use, pending stops and contextual battle legality and never claims exact hidden tyre knowledge or future outcomes.
- [x] Host-configured per-player settings apply to private/shared controllers and can change between phases without performance, championship or budget penalties.
- [x] Add explicit optional recommendation acceptance at timeout, disclose it before play and durably reuse saved recommendations/concrete choices instead of recomputing under a changed policy.
- [x] Advice/explanations use authorised team channels except intentional Family/Mixed openness, preserving other teams' privacy.
- [x] Verify displayed help modes, legal recommendations, hidden-input independence, stable repeats, timeout auto-accept and Guided browser flows through existing public boundaries.

**Answer:** [Focused acceptance and capture evidence](../evidence/assistance.md). Recommendations and explicit timeout choices persist at the phase-start boundary; Guided retains uncertain legal choices. Root owns combined verification.
