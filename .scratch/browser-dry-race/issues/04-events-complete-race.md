# 04: Present events and continue through a complete race

**What to build:** A player follows short race-event feedback, continues successive Race Phases and finishes a complete dry race with clear results and eligibility.

**Blocked by:** 03 — Integrate the production race screens; visual foundation 05 — Present phase outcomes and race finish.

**Status:** resolved

- [x] Project a bounded ordered timeline from newly resolved allowlisted events using stable event identities/cursors; prioritize overtakes, pit entry/exit and tyre changes and summarize crowded or quiet phases.
- [x] Explanations are supported by recorded facts; causal undercut claims require timing evidence and no unsupported incidents or counterfactuals are invented.
- [x] Short configurable playback supports skip, replay and reduced motion, then reconciles to the exact authoritative phase state; playback never calculates race outcomes.
- [x] An accepted reviewed team plan runs one phase and automatically opens the next decision window once; a disconnected display cannot block progress, duplicate submission does not resolve twice, and retained feedback remains independent.
- [x] Play successive five-leader-lap phases through the existing engine's final shortened phase, lapping and finish semantics without changing coefficients or physics.
- [x] Show completed distance and two-compound eligibility, explain the rule before play, retain ineligible results without automatic correction and disable further submissions after finish.
- [x] Allow explicit restart with the same or another preset. A short real-server/browser full-race test verifies presentation, progression and final results.

## Answer

Implemented serialized, authorized and idempotent continuation through the existing engine finish. Added bounded new-event timelines, stable opaque identities and factual counts; shared result components hold the previous published table during playback and reconcile exactly on completion/skip, with replay and reduced motion. New windows reset visible defaults and reject delayed acknowledgements from prior phases. Final distance and eligibility remain unchanged, with explicit restart controls. See [verification and inspected captures](../evidence/04-complete-race.md) and [local run procedure](../../../docs/browser-dry-race.md).


2026-10-09 user feedback supersedes the original separate Continue flow: review both cars, run once, then edit automatically opened next-phase defaults while retained public feedback plays. Final results stay terminal. Pending/ongoing stops are now verified through actual-engine HTTP, SignalR and browser fixtures. See [final review fixes](../review.md).
