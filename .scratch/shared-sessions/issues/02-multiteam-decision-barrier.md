# 02: Lock multiple teams and resolve together

**What to build:** Several players privately manage both cars and the race resolves exactly once after every required team locks.

**Blocked by:** 01 — Join a household lobby and claim a team.

**Status:** resolved

- [x] Authorised private controllers receive their legitimate team observations; spectator displays receive public Classification, lifecycle and per-team readiness only.
- [x] Validate current authenticated membership and assignment on every command/private subscription; prevent cross-session or wrong-team access.
- [x] Preserve atomic two-car commands, command identities and expected phase/revision. The first valid team lock is final and visible across authorised devices.
- [x] Freeze scripted choices from phase-start observations before human locks; resolve only when the complete frozen required set is ready.
- [x] Concurrent/repeated/stale/conflicting submissions neither replace accepted locks nor resolve twice; rejected input changes no lock or driver state.
- [x] Missing players leave the phase waiting; membership or revocation cannot silently shrink the required set. Deadlines and fallback remain deferred.
- [x] Exercise differing lock orders and concurrent locks through API/SignalR, plus a real-browser two-private-controller flow and public/private payload checks.

## Answer

Extended the existing race coordinator and engine with a frozen required-team barrier, atomic validated two-car locks, immutable per-team accepted commands, and public readiness. Household HTTP commands and private SignalR subscriptions verify current authenticated membership plus explicit device assignment. Additional joined devices require an explicit host grant before start; knowing another device ID grants no access. Private streams carry team and assignment revision, and only the authorised team receives its choices and estimates. Snapshot revisions advance for readiness while commandRevision identifies the unchanged decision window. Scripted plans remain frozen from phase-start observations. Missing teams leave the phase waiting.

## Evidence

- API/SignalR vertical TDD: the initial public-boundary test failed with 404 before implementation; after implementation, forward/reverse/concurrent lock-order tests pass. Assertions cover one resolving/outcome publication, retry identity, conflicting locks, stale phase/revision, malformed second car, wrong team/private subscription, ungranted devices, cross-device accepted state and private/public payload filtering. Existing legacy session and simulation regressions also pass.
- Final affected verification run: `.tools/reports/verification/runs/2026-10-10T11-32-02-540Z-711126a2-d6b2-46c8-97f5-34179b29a40c/result.json`: frontend typecheck/build, 33 API tests, 110 simulation tests and 2 shared browser tests passed. Production build retains the existing initial bundle and qrcode CommonJS warnings.
- Real browser: two separately authenticated private controllers edit their own two cars; first lock waits, refresh retains final choices, the second lock resolves and both automatically receive the next window.
- Inspected 360px phone controller complete flow (`evidence/multiteam-controller-360.png`): both cars, strategic controls and confirmation remain contained with no page overflow; private state belongs to the selected team. Existing compact timing headings remain dense.
- Inspected 1920x1080 public display (`evidence/multiteam-waiting-1920.png`): all 22 timing rows fit, readiness distinguishes Locked/Deciding, no private choices or estimates appear. Corrected a readiness separator encoding issue before final capture.
- Local verification owns UI/API ports 4471/4571 and authentication ports 19099/19400/19450; recorded command PIDs and cleanup are in the run manifest. No services intentionally retained.
- Actual human household playtest feedback remains pending under ticket 06 by user direction; browser coverage does not substitute for that feedback.
