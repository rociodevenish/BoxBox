# 05: Offer Assisted and Guided decisions

**What to build:** Each player receives the selected help level with understandable, executable two-car advice based only on their team's legitimate information.

**Blocked by:** 02 — Race against fair, observation-driven AI; 03 — Continue through deadlines and missed decisions, for the included timeout auto-accept flow.

**Status:** ready-for-agent

- [ ] Build structured observation-driven recommendations with plain-language reasons, phase/freshness and uncertainty; repeated reads/reconnect/recovery do not reroll advice.
- [ ] Standard retains all legal choices without strategic recommendations. Assisted retains all choices and highlights/explains the suggested plan.
- [ ] Guided may reduce a choice group to two sensible options only when confidence supports it; otherwise retain legal options and explain forced single-option states.
- [ ] Advice respects compound use, pending stops and contextual battle legality and never claims exact hidden tyre knowledge or future outcomes.
- [ ] Host-configured per-player settings apply to private/shared controllers and can change between phases without performance, championship or budget penalties.
- [ ] Add explicit optional recommendation acceptance at timeout, disclose it before play and durably reuse saved recommendations/concrete choices instead of recomputing under a changed policy.
- [ ] Advice/explanations use authorised team channels except intentional Family/Mixed openness, preserving other teams' privacy.
- [ ] Verify displayed help modes, legal recommendations, hidden-input independence, stable repeats, timeout auto-accept and Guided browser flows through existing public boundaries.
