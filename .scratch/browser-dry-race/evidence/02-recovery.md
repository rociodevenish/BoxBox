# Reliable submission and recovery — 2026-10-08

API accepted retry started red with 409; now returns its original acknowledgement and does not mutate Classification/time/revision. A changed payload under that UUID is a clear conflict. Concurrent distinct commands accept only one; identical commands with failing external realtime delivery return identical acknowledgements and preserve revision 3 / Lap 5.

A fresh authorized SignalR subscription originally lacked the confirmed own plan; it now recovers both own accepted choices. Invalid second-car enum, malformed/null car collection, wrong-team ownership, stale phase/revision and unavailable Attack leave the entire decision window unchanged, with no confirmed plan. Public contracts still exclude own and opponent instructions. Subscription snapshots are retrieved after validated group joining to avoid a transition falling between snapshot and membership.

Real-browser refresh initially restored Standard defaults in a locked controller; it now restores confirmed Push/Pit/Hard choices. Rejection preserves editable drafts. Failed initial negotiation initially lacked a recovery action; explicit Reconnect now fetches authorized state. Uncertain delivery retains one UUID and exact plan; reconnect checks server state and retries that same identity only if still awaiting decisions.

A lost realtime team message exposed stale observations at the HTTP acknowledgement revision; the regression started red at missing Hard observations. The fix keeps lock acknowledgement separate and retrieves a full fresh snapshot. Lost HTTP acknowledgement plus suppressed real team messages recovers the existing lock without a second POST. Automatic WebSocket reconnect during decision/result rejoins authorized groups. Delayed older and duplicated actual SignalR team frames cannot replace the final revision or reopen actions. No private component methods are used by these tests.

The local server remains in-memory; replaced/expired sessions report unavailable with a fresh-session link. No full race, continuation, durability or polished-controller integration is claimed by this ticket.

Verification complete: 108 simulation tests, 11 API/SignalR tests, 34 gallery tests and 6 real-server browser tests passed. Angular typecheck/build passed; production output 265.58 kB raw / 69.57 kB estimated transfer. Deliberate negotiation/WebSocket errors in browser logs are the tested interruption scenarios.


Reviewed [live controller](02-recovered-controller-360.png): 360 × 1108 full-page image from a 360 × 800 viewport. Connected/result state, confirmed Push/Pit/Hard choices and actual updated own tyre observations are readable, with no horizontal clipping. This is the minimal live adapter; polished component adoption remains ticket 03.
