# 05: Present phase outcomes and race finish

**What to build:** A viewer can follow fixture-driven resolution, concise overtake and pit feedback, phase results and an unmistakable finished-race state.

**Blocked by:** 03 — Build the public timing tower.

**Status:** resolved

- [x] Gallery flows cover resolving, overtakes, pit entry/exit, tyre changes, phase results and race finish using reusable event and status treatments.
- [x] Reuse suitable supplied driver and track imagery for event/result context after checking the current design-asset collection; asset identity comes from data and failed imagery must not hide race information.
- [x] Intermediate phase results and final Classification are visibly distinct; quiet and crowded phases remain readable.
- [x] Event summaries use explicit public fixtures and do not expose hidden values, private estimates or locked opponent decisions.
- [x] Reduced-motion presentation preserves the same information without requiring animation.
- [x] Review representative shared-display and mobile layouts and record findings; presentation introduces no simulation rules or authoritative state transitions.

## Answer

Built reusable public phase-result and event treatments with fixture-driven resolving, quiet/crowded feedback and explicit race finish. Existing tower and compound/status treatments remain shared. Data-mapped supplied portraits have useful failure fallbacks; the asset collection currently has no track imagery. Public pit-event/Classification consistency, shared-display containment and keyboard list scrolling were verified red then green. All 30 frontend tests, production build and strict template/type checking pass. [Reviewed evidence](../evidence/phase-results-review.md) records real capture dimensions and limits. Race rules, filtering and authoritative transitions remain outside these components.
