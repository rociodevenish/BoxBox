# Ticket 06 automated household verification

The actual household playtest remains pending by explicit user direction. No physical-device observations, participant quotations, joining-friction measures or handoff times are claimed here.

Verified integration includes tickets 01–05 at `53603b778345f920bb88d2a9c3f81d133ac1b2ec`; documentation and browser verification commit is `6b244453e3aea0abc312bfca11b58bd34b7430b4`. No simulation code or inputs changed.

## Automated findings

The existing approved browser boundary covers separate authenticated private controllers, a trusted shared tablet with separate named participants, authorised Family TV decisions, Mixed private/Open isolation, independent public spectators, neutral Ready handoff, refresh, pause/resume and revoked access. Existing real-token API/SignalR checks cover claims, permissions, payload isolation, simultaneous locks, retry/revision handling and frozen required teams.

One vertical coverage slice extended the two-private-controller flow to the complete six-lap race. Its red record `.tools/reports/06-early.json` (PID 33156) failed on absent Finish confirmed because the former scenario stopped after the five-lap phase. The extension submits both teams in reverse order for the final one-lap window, verifies the early submitter still waits and cannot finish the race, then confirms both controllers' team final results, the public Lap 6 / 6 and all 22 Classification rows. This stay-out scenario correctly disqualifies the four human cars under the unchanged two-compound dry-race rule; it verifies complete session progression rather than a legal pit strategy. Existing solo/boundary/simulation checks cover pit behavior.

The focused green record `.tools/reports/06-early-final.json` passed four flows. A capture-directory correction reran only lobby/handoff: `.tools/reports/06-captures.json` passed three flows and captured public final state after Skip playback. These are repeated checks of the same cases, not additional unique test counts.

## Inspected layouts before broad verification

- [Private controller](06/multiteam-controller-360.png), viewport 360×800/full-page: both cars, pace/battle/pit choices and approval contained; no horizontal overflow. Compact timing headings remain dense.
- [Neutral handoff](06/tablet-neutral-360.png), 360×800/full-page: next team, Ready and trusted-device explanation only; no previous private cards, estimates, draft or alert behind it. Keyboard Enter/touch Ready and late private frame rejection are checked by the flow.
- [Tablet controller](06/tablet-ferrari-768.png), 768×1024/full-page: two clearly separated car columns, Ferrari's private shared-tablet label, Pass tablet and approval contained. Scrolling is required to reach all strategic controls; no horizontal overflow.
- [Paused public display](06/host-phase-1920.png), 1920×1080: clear host-resume wording, public readiness and all 22 timing rows fit. No private choices or estimates.
- [Complete public race](06/household-finish-1920.png), 1920×1080: Finish confirmed, Lap 6 / 6, both teams Locked, 22 rows and public chronological events fit. Disqualification reflects the explicit stay-out choices above.

## Setup and limitations

[Household guide](../../../docs/shared-sessions.md) now documents separate LAN auth binding at 0.0.0.0 using a temporary config, frontend 0.0.0.0, browser-reachable `Firebase__EmulatorUrl`, backend loopback auth address and existing API/SignalR proxy. Tracked default/test auth config remains loopback. Instructions were checked against package scripts, `LobbyEndpoints` auth config and frontend `connectAuthEmulator`, plus Firebase's [official Auth emulator connection guide](https://firebase.google.com/docs/emulator-suite/connect_auth).

Physical network/firewall behavior and QR scanning still need the human playtest. Shared handoff is a courtesy on one trusted device, not independent participant authentication. Browser reload recovers only while the server survives. Absent required players wait indefinitely; revocation retains required teams. No durable recovery, deadline, absent-player fallback, AI takeover or assistance exists. Additional device grants are supported by the authenticated host API but have no lobby grant UI.

## Broad verification record

The single broad run `2026-10-10T21-20-13-813Z-7a35f37e-3c3e-4e98-921d-c1491f4c4d8f` at the documentation/test SHA above passed tooling (3 cases), typecheck/build, 42 API tests and 110 simulation tests, then stopped at gallery (32/34 passed). Two obsolete presentation assertions expected a removed Selected status and a full name where the compact 360px tower intentionally shows MSU. Commit `7e8cc4e2e59d815ca8479ee593fd9927083fdeba` updates these browser assertions only. Affected/remaining run `2026-10-10T21-22-06-637Z-13a32605-b672-4152-992c-7bea4cd489ce` runs gallery, browser, boundary and shared only; no passed backend or build step is repeated. The immutable manifest and command/PID identities are under `.tools/reports/verification/runs/<run-id>/result.json`. Node 24.19 and the repository .NET SDK/cache were used. Existing initial-bundle and qrcode/dijkstrajs CommonJS warnings remain.



## Subsequent focused diagnosis and checks

The remaining browser run exposed outdated solo presentation assumptions (closed Display settings, paragraph decision labels, compact board headings/status, lap times outside the nearby table and hidden controls while confirmation is pending). Assertions now follow actual visible UI while retaining request/revision, lock, playback, refresh, accepted-plan, private/public separation and final eligibility checks. Retry coverage compares both submitted car plans as well as command IDs, proving the hidden pending Push draft survives. The lapped finish still asserts all 22 timing rows fit at 1080px; the preset link below development facts is checked by scrolling to it instead of requiring it above the fold.

One actual accessibility regression was repaired at `719a081`: inline timing tables lost their accessible Classification name when their visual caption was omitted. An aria-label restores it without layout changes. A second actual existing regression was repaired at `9af6bf6`: a next-phase SignalR snapshot clears the old pending command but formerly left busy set until an older HTTP response arrived. The snapshot now clears obsolete busy, and the old response's finally block is constrained by current private generation and command phase. The existing delayed-ack browser test went red on a hidden next window and passes with this fix.

Run `2026-10-10T21-22-06-637Z-13a32605-b672-4152-992c-7bea4cd489ce` passed 34 gallery checks, then its obsolete transformed browser cases were stopped using only the recorded launched browser PID 1136 and owned process tree. The run records failure, not a completed browser pass. Run `2026-10-10T21-27-46-211Z-9ff6a943-0145-4499-91bb-78de6cc4d3e2` passed typecheck/build and 17/22 browser cases; five failed cases supplied the exact focused correction/diagnosis above. Only affected files are rerun in `.tools/reports/06-browser-final.json`; no passed full suite is repeated.

Run `2026-10-10T21-28-06-199Z-f9cf52fe-67b9-4322-b0d3-071f9cd7bcf8` passes all six household browser flows at `b5172b0`, including complete six-lap two-controller progression, neutral shared-tablet/late-frame/revocation flow, host phone pause/resume/revoke and both Family/Mixed authorised Open flows. These use UI/API 4472/4572 and the exclusive auth lease. All five owned ports were checked clear after completion. These six unique cases are counted once, not again for earlier focused invocations.

Independent review of the busy fix identified a related shared-tablet risk: a late previous-phase HTTP acknowledgement can reach the handoff branch before its phase check and clear a newer draft. The integration owner has assigned that separate correction and browser regression to the final review worker; this worker does not claim that correction is already verified. Human playtest findings remain separately pending.

Final focused `.tools/reports/06-browser-final.json` (PID 43108) passes 13 of its 15 cases: all three complete-race checks including delayed acknowledgement, five 50-lap exact-replay scenarios, both integrated cases and three recovery cases. The two remaining cases still used transformed old pending-control assertions loaded before their correction; `.tools/reports/06-recovery-final.json` (PID 26264) passes both at `5719761`. Combined with the 17 passed cases in the preceding browser run, all **22 unique solo-browser cases** pass. Counts are deduplicated by case identity.

The final compile/build record `2026-10-10T21-31-48-895Z-f027f3ea-a789-4287-94c8-adc2df828edf` pins `57197617c3659211e90f836185cfa124ec700df4` (includes aria-label and busy fixes) and passes typecheck/build. Its first boundary startup failed because a concurrent owned API held the same worktree's build DLLs; the boundary suite is rerun sequentially. Its two cases then exposed the same obsolete decision heading/summary assumptions; corrected checks retain selected pace/pit and unavailable Neutral explanation, unchanged public/team snapshots after rejected input and queued/ongoing-stop restrictions. No simulation implementation changed.

Final boundary-only run `2026-10-10T21-34-57-624Z-d9aee27e-13db-4a8e-be30-334afa226e35` passes both cases at `d58b0fc`. Unique passing counts: tooling 3, API 42, simulation 110, gallery 34, solo browser 22, pit boundary 2, household browser 6 (**219 total**), plus frontend typecheck/build. This is one broad verification attempt followed only by affected corrections/remaining steps, not repeated full passes.

[Completion manifest](06/household-verification.json) preserves integrated base, code revision, unique counts, reviewed captures and every retained run's recorded command PID/status/exit/elapsed identity. UI/API ports 4471/4571, 4472/4572, attempted 4473/4573 and Auth/hub/logging 19099/19400/19450 were checked with no listeners after completion. No preview or emulator service is retained. Integration tip was merged into this worker branch (already up to date); this worker does not merge to integration. Human findings are the only unchecked ticket acceptance item.
