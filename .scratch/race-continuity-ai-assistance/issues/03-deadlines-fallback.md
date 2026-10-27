# 03: Continue through deadlines and missed decisions

**What to build:** A live family race uses visible deadlines and fair missing-player fallback while preserving every timely accepted team lock.

**Blocked by:** 02 — Race against fair, observation-driven AI.

**Status:** resolved

- [x] Configure server-owned deadline instants and explicit shared-device handoff allowance before decisions begin; refreshing never extends the deadline.
- [x] Show countdowns using estimated server-clock offset and disclose fallback policy before play.
- [x] Save remaining duration on host pause and create the resumed deadline from that duration; browser countdowns never govern authority.
- [x] Serialize deadline closure and submissions under session revision; only complete valid locks processed before the deadline are accepted, with documented exact-boundary behaviour.
- [x] Preserve accepted human locks, fill missing teams with frozen/versioned temporary AI choices and persist the complete set before resolution. Reject late commands with current state.
- [x] Temporary timeout fallback lasts one phase and lets the player act in the next open window; Standard mode does not reveal recommendations merely because fallback exists.
- [x] Use a controllable server clock through API/SignalR checks for before/at/after deadline submissions, duplicate closure, pause/resume and shared-device timing; include a browser shared-device timeout flow.

Automated evidence: [deadlines/fallback](../evidence/deadlines-fallback.md). Hosted scheduling and real household timing evidence remain pending in ticket 06.
