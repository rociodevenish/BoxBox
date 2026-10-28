# 06: Resume live progression after idle or interruption

> GitHub issue: [#51](https://github.com/rociodevenish/BoxBox/issues/51) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A family race continues through bounded presentation and scheduled deadlines even when displays disconnect or the server idles, and recovers without racing ahead unattended.

**Blocked by:** 03 — Continue through deadlines and missed decisions.

**Status:** implemented locally; hosted scheduling validation pending ticket 07

- [x] Persist bounded server-owned presentation timing and open the next decision window automatically unless paused; authorised host continue/skip can advance early exactly once.
- [x] Display acknowledgements are unnecessary and playback never owns race progression.
- [x] Supply recoverable scheduled wakeups with authenticated callbacks and idempotent delivery; timers cannot depend on an open browser or a continuously running process.
- [x] Duplicate/late callbacks and restart do not create additional outcomes, change saved automatic decisions or acknowledge unsaved transitions.
- [x] Recovery closes at most an already expired decision window, then pauses for host resume before opening further windows; avoid bursts of unattended phases.
- [x] Verify overdue transitions, disconnected displays, duplicate deliveries and pause/resume using controllable clock/delivery adapters through API/SignalR and persisted emulator data.
- [x] Document scheduling assumptions and expected family-scale costs for hosted validation; SignalR remains the live transport.

Evidence: [recoverable progression](../evidence/recoverable-progression.md). Cloud Tasks/Scheduler adapter and exact OIDC caller policy are implemented; actual hosted delivery/IAM/cost checks remain explicitly pending ticket 07 and project authorization.
