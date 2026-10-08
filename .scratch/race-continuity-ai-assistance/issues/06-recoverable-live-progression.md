# 06: Resume live progression after idle or interruption

**What to build:** A family race continues through bounded presentation and scheduled deadlines even when displays disconnect or the server idles, and recovers without racing ahead unattended.

**Blocked by:** 03 — Continue through deadlines and missed decisions.

**Status:** ready-for-agent

- [ ] Persist bounded server-owned presentation timing and open the next decision window automatically unless paused; authorised host continue/skip can advance early exactly once.
- [ ] Display acknowledgements are unnecessary and playback never owns race progression.
- [ ] Supply recoverable scheduled wakeups with authenticated callbacks and idempotent delivery; timers cannot depend on an open browser or a continuously running process.
- [ ] Duplicate/late callbacks and restart do not create additional outcomes, change saved automatic decisions or acknowledge unsaved transitions.
- [ ] Recovery closes at most an already expired decision window, then pauses for host resume before opening further windows; avoid bursts of unattended phases.
- [ ] Verify overdue transitions, disconnected displays, duplicate deliveries and pause/resume using controllable clock/delivery adapters through API/SignalR and persisted emulator data.
- [ ] Document scheduling assumptions and expected family-scale costs for hosted validation; SignalR remains the live transport.
