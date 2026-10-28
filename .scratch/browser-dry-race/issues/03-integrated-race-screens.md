# 03: Integrate the production race screens

> GitHub issue: [#5](https://github.com/rociodevenish/BoxBox/issues/5) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A player manages both cars using the visual foundation while a separate public display shows accurate race information without leaking private or diagnostic state.

**Blocked by:** 02 — Make submissions and recovery reliable; visual foundation 03 — Build the public timing tower; visual foundation 04 — Build the two-car decision controller.

**Status:** resolved

- [x] Integrate display and controller routes with the shared Angular components and server snapshots/notifications, without a Babylon.js dependency.
- [x] Inspect the current supplied design-asset collection and reuse suitable driver/track imagery through the foundation's data-driven asset conventions; preserve correct preset/driver identity and readable fallbacks without introducing a new track-card or race-renderer scope.
- [x] Visibly default new phase choices to Standard, Stay Out and Neutral; explain pace/battle trade-offs and next-lap-end pit timing.
- [x] Offer only server-legal contextual Battle Instructions and suppress incompatible changes for pending/ongoing stops; summarize both cars before locking.
- [x] Show exact Classification, coherent leader/ahead gaps and explicit lap deficits and units. If ahead gaps need engine observation extension, use recorded timing evidence rather than converting progress into fabricated precision.
- [x] Show public compound, age and pit state; own-team condition remains a labelled estimate in the authorised private view.
- [x] Public and private HTTP/SignalR payloads exclude exact hidden condition, form, seed, draws, diagnostic histories, future outcomes and opponents' unexecuted choices; public subscribers cannot access private groups or commands.
- [x] Verify real server-driven controls, resolving/locked/error/disconnected states, public/private filtering and representative phone/shared-display layouts through existing harnesses.

## Answer

Implemented live TimingTower and TwoCarController integration through an isolated transport/recovery adapter, explicit per-car legality and confirmed plans. Added recorded nullable timing observations without changing historical numeric snapshots or replay physics. Initial mixed-field public order now follows rolling gaps. Reviewed real phone and full shared-display captures, retained all recovery checks and documented asset identity decisions. See [verification and inspected evidence](../evidence/03-screens.md) and [local run procedure](../../../docs/browser-dry-race.md).
