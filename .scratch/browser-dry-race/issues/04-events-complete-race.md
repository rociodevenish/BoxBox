# 04: Present events and continue through a complete race

**What to build:** A player follows short race-event feedback, continues successive Race Phases and finishes a complete dry race with clear results and eligibility.

**Blocked by:** 03 — Integrate the production race screens; visual foundation 05 — Present phase outcomes and race finish.

**Status:** ready-for-agent

- [ ] Project a bounded ordered timeline from newly resolved allowlisted events using stable event identities/cursors; prioritize overtakes, pit entry/exit and tyre changes and summarize crowded or quiet phases.
- [ ] Explanations are supported by recorded facts; causal undercut claims require timing evidence and no unsupported incidents or counterfactuals are invented.
- [ ] Short configurable playback supports skip, replay and reduced motion, then reconciles to the exact authoritative phase state; playback never calculates race outcomes.
- [ ] Keep the next decision window closed until an authorised controller continues; a disconnected display cannot block progress and repeated continue requests do not advance twice.
- [ ] Play successive five-leader-lap phases through the existing engine's final shortened phase, lapping and finish semantics without changing coefficients or physics.
- [ ] Show completed distance and two-compound eligibility, explain the rule before play, retain ineligible results without automatic correction and disable further submissions after finish.
- [ ] Allow explicit restart with the same or another preset. A short real-server/browser full-race test verifies presentation, progression and final results.
