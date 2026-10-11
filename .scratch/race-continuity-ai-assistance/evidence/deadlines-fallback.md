# Deadline and temporary fallback evidence

Ticket 03 automated acceptance is complete. Hosted scheduler/idle wakeup, real household timing and human playtest evidence remain pending in ticket 06.

Household setup defaults to 90 decision seconds plus 30 seconds per additional human team sharing any assigned device. Hosts may configure 15–900 decision seconds and 0–120 handoff seconds before starting. The largest device workload determines the allowance; two teams sharing one tablet receive 120 seconds. Setup discloses the effective timing and fallback before play. Window opening freezes the server UTC deadline and all human fallback plans from the legitimate phase-start observation. Refresh never extends it.

Submission and due closure share the mutation semaphore. Accepted command identities are checked before expiry. A complete valid human lock must finish validation while server time is strictly before the deadline; equality is expired. Late responses return 409 plus the current public state. Closure preserves every accepted human lock, fills missing teams with saved versioned plans, saves the complete set before simulation, commits the outcome, and opens the next window. Temporary AI is recorded for that phase only; it grants no recommendations to Standard controllers.

Pause saves remaining seconds and clears the deadline. Resume creates a new deadline from that saved remainder. Recovery of an expired or pending complete window closes at most that phase and pauses before another window. Outcome-write failure returns 503, publishes no uncommitted result, and restart resolves the already saved timeout set without a fictitious human acknowledgement.

The browser estimates server clock offset from the public snapshot. It displays a compact countdown and fallback status, with the detailed policy expandable. The server remains authoritative. The shared tablet returns to neutral handoff after timeout; the heading, selected team and Ready button agree.

Focused tests used Node 24.19/.NET10.0.401/Java 21, real Auth 19109+Firestore 19100, fixture API 4583 and frontend 4483. All were owned processes; existing preview listeners remained running.

- API RED/GREEN: missing deadline, equality/after expiry accepted, pause duration not frozen, overdue restart left race waiting; each fixed in its vertical cycle.
- Seven DeadlineTests cover frozen shared allowance/refresh,119/120/121-second submissions, preserved accepted retry and choices, duplicate due delivery, real Firestore pause/restart, overdue restart, one-phase human re-entry and timeout complete-plan recovery across failed outcome write.
- Browser RED: pre-start policy absent. GREEN: shared first lock, absent second player, controlled 121-second advance, public temporary source and neutral next handoff. Final report: `.tools/reports/s06-deadline-browser-final.json`.
- Reviewed 360×800 phone before and after timeout, and 1920×1080 public display. Policy/countdown/source readable; no horizontal overflow. Phone heading/select/button all identify the same next team. All 22 public classification rows fit within 1080px after moving desktop timing into a compact strip. Root final assessment: the 360px handoff is readable, heading/select/button all Ferrari, Ready visible within 800px height; remaining policy details scroll without horizontal overflow. The 1920×1080 public display shows all 22 rows, last row ending1031px; the inline timing/fallback keeps table and events legible. Exact refreshed files are linked below. The existing playback/identity imagery is separate ticket 06 presentation work.

Reviewed captures: [phone before](deadline-phone-before-360.png), [phone after](deadline-phone-after-360.png), [public display](deadline-display-1920.png).

## Parallel feature ownership

Session and Lobby are private partial types. Both expose a durable ModuleState dictionary for namespaced JSON values: leaf partial modules own typed records and serialization (e.g.control04,assistance05,progression06); the central recovery contract captures/restores the dictionaries automatically. Absent keys mean the feature default. Do not overwrite another module's key.

Control04 implements ApplyControlBoundary(Session) before required-team freezing. Assistance05 implements ConfigureTimeoutPlan(Session,teamId,ref decisions,ref source) once at window opening; concrete results are persisted. Progression06 implements PhaseCommitted(Session) before outcome persistence and WindowOpened(Session) before window persistence, including resumed deadlines. It may set DeferNextWindow to retain the durable result/presentation boundary. AdvanceDue is the idempotent expected-phase/window-revision entry for production scheduling; automatic wakeups belong06. A stale delivery returns current state without advancing another window.

Leaf partial files, typed module records and leaf endpoints are independent. Root owns final composition of shared DTO projections, frontend templates, dependency registration and hosting/scheduler wiring where edits overlap. No production clock-control route was added.

Final affected API report `.tools/reports/s06-deadline-api-final02.json`: 19 passed (seven deadline, nine durable recovery, three host control checks). Host idempotence comparisons exclude only the dynamic serverTimeUtc field and still compare every saved state field.
