# 07: Host and verify the family race

**What to build:** A household can join and finish a hosted dry race with durable recovery, deadlines, AI and assistance, backed by measured reliability and cost findings.

**Blocked by:** 04 — Transfer control at safe phase boundaries; 05 — Offer Assisted and Guided decisions; 06 — Resume live progression after idle or interruption.

**Status:** ready-for-agent

- [ ] Deploy Angular to Firebase Hosting, ASP.NET Core/SignalR to Cloud Run and use Firestore/Auth, initially with min instances zero/max one, separate environments and emulator-based local development.
- [ ] Keep credentials server-side, enforce origins/authenticated channel membership and authenticated scheduled callbacks, and document deployment, rollback and compatible recovery procedures.
- [ ] Configure cost controls, bounded diagnostics retention and useful transition/error logging without exposing credentials or hidden data to players.
- [ ] Verify hosted join, SignalR reconnect, idle scheduled wakeup, restart, accepted-lock recovery and final-result persistence; emulators alone do not prove hosted behaviour.
- [ ] Run an actual family playtest covering missing players, shared-device deadlines, takeover/reclaim, mixed assistance and interrupted presentation.
- [ ] Record decision, resolution, presentation and recovery latency separately, plus storage/request/scheduling/runtime usage and real cost limitations; approximately zero monthly cost is a measured goal, not a guarantee.
- [ ] Retain designer-only replay inputs, concrete automatic choices and AI/assistance versions for reproducible outcomes, separately from player payloads.
- [ ] Run relevant public API/SignalR, browser, emulator restart, frontend build/type and simulation regression checks; record supported findings and remaining operational/AI/assistance limitations.
