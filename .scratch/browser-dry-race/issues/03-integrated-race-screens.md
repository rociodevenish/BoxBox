# 03: Integrate the production race screens

**What to build:** A player manages both cars using the visual foundation while a separate public display shows accurate race information without leaking private or diagnostic state.

**Blocked by:** 02 — Make submissions and recovery reliable; visual foundation 03 — Build the public timing tower; visual foundation 04 — Build the two-car decision controller.

**Status:** ready-for-agent

- [ ] Integrate display and controller routes with the shared Angular components and server snapshots/notifications, without a Babylon.js dependency.
- [ ] Inspect the current supplied design-asset collection and reuse suitable driver/track imagery through the foundation's data-driven asset conventions; preserve correct preset/driver identity and readable fallbacks without introducing a new track-card or race-renderer scope.
- [ ] Visibly default new phase choices to Standard, Stay Out and Neutral; explain pace/battle trade-offs and next-lap-end pit timing.
- [ ] Offer only server-legal contextual Battle Instructions and suppress incompatible changes for pending/ongoing stops; summarize both cars before locking.
- [ ] Show exact Classification, coherent leader/ahead gaps and explicit lap deficits and units. If ahead gaps need engine observation extension, use recorded timing evidence rather than converting progress into fabricated precision.
- [ ] Show public compound, age and pit state; own-team condition remains a labelled estimate in the authorised private view.
- [ ] Public and private HTTP/SignalR payloads exclude exact hidden condition, form, seed, draws, diagnostic histories, future outcomes and opponents' unexecuted choices; public subscribers cannot access private groups or commands.
- [ ] Verify real server-driven controls, resolving/locked/error/disconnected states, public/private filtering and representative phone/shared-display layouts through existing harnesses.
