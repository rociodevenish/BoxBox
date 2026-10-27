# Local browser dry race

Requires .NET 10 and Node 24. From repository root, start the server:

```powershell
dotnet run --project src/backend/Game.Api --urls http://127.0.0.1:4510
```

In another terminal:

```powershell
cd src/frontend
npm ci
npm run start:race
```

Open http://127.0.0.1:4410/race/new, choose preset/distance/designer seed, and open the separate public display and controller links. Select both cars, review and submit. The authoritative server broadcasts resolving, runs the existing engine to the next phase boundary and broadcasts its exact result. After resolution the next decision window opens automatically. Review and run the next phase through the final shortened phase and finish. Reliable submission and connection recovery are implemented. The fixture gallery remains runnable with npm start on port 4400. The race proxy forwards /api/** and /hubs/** to 4510. No cloud services or renderer are needed.

Game.Application owns session state and opponent policy; Game.Api supplies HTTP and SignalR. Angular holds draft instructions and presentation only. Four allowlisted segmented presets (open-equal, restricted-equal, open-mixed, restricted-mixed) support distances 6/10/20/40/50. Each has 22 cars, 11 teams, fixed rolling order with 0.5-second gaps and fresh Medium tyres. Mixed fields vary car fit and a small driver Pace offset, so do not represent equal driver attributes. The human owns team-01 and driver-01/02.

Player, team and device identities are distinct. A separate random controller capability is hashed on the server and conveyed in the controller URL fragment. Keep that link private. Session IDs authorize no private observations or commands. Private subscriptions validate that grant before joining their server-defined group.

Public and private snapshots use explicit allowlists. Public Classification includes timing, compounds, stop status and filtered overtake/pit events; private snapshots include only owned cars' legitimate tyre estimates and legal battle options. No seed, tuning, exact condition, randomness, opponent choices or raw engine result enters player snapshots. Initial timing uses known rolling gaps instead of the engine's zero-time zeros. Later unavailable timing remains unavailable; adjacent gaps are not invented by subtracting incompatible leader gaps.

External phase IDs start at zero. The first window is revision 1 awaiting-decisions; acceptance publishes resolving and phase-result, then automatically opens awaiting-decisions for the next phase, or publishes finished at the last boundary. The retained presentation identifies its resolvedPhase separately from the current decision phase. The display may play that feedback while next-phase choices are editable. Commands carry expected phase/revision and UUID; plans include both owned cars. All choices validate before mutation. Frozen dry-opponents-v1 scripts read each opponent team's legitimate phase-start observation, choose conservative pace for estimated worn tyres and a legal compound stop when due, otherwise Standard/Neutral. They freeze again when resolution opens the next phase and do not react to human choices. Complete accepted plans and script version remain outside player payloads and are available through the separate local designer replay resource.

The server holds one in-memory session. Creating another replaces it; restart clears it. Stale views report unavailable and need a new local playtest. The default run uses loopback; the trusted LAN procedure and designer-only diagnostics are documented in browser-playtest.md. This is not deployment/authentication infrastructure.

Verification:

```powershell
dotnet test F1Game.slnx
cd src/frontend
npm run typecheck
npm run build
npm test
npm run test:browser
npm run test:boundary
```

Browser smoke starts API 4510 and Angular 4410 itself; stop manual processes on those ports first. Playwright needs installed Chromium or PLAYWRIGHT_BROWSERS_PATH. API tests negotiate SignalR LongPolling through TestServer and verify exact engine outcome, ordered lifecycle updates, permission boundaries and atomic rejection. Browser test uses real server and separate pages, verifies capability denial and both-car submission, then observes the five-lap result.

The live routes reuse the production TimingTower and TwoCarController. New choices visibly start at Standard, Stay Out and Neutral. Each car receives its own allowed battle options and public stop restriction from the server. Confirmation covers both cars; rejected drafts remain editable and confirmed snapshots restore the actual accepted choices. Connection and lifecycle labels use readable player text.

The display offers Gap (to leader) or Interval (to the car ahead), compound names, tyre ages and stop status. Recorded engine timing supplies each reference independently; missing evidence is unavailable and lap deficits are separate from seconds. The initial public order follows known rolling gaps, without changing historical engine Classification or replay physics. All 22 rows fit in reviewed 1920 × 1080 initial, phase and final views. No living-room TV playtest is claimed. See [screen integration evidence](../.scratch/browser-dry-race/evidence/03-screens.md) and [complete-race evidence](../.scratch/browser-dry-race/evidence/04-complete-race.md).

## Feedback and finish

Each result contains only newly resolved allowlisted events, selected from the cumulative engine-history suffix. Opaque stable event IDs and a presentation cursor keep refreshes and duplicate notifications from generating new facts. A maximum of 12 chronological highlights prioritizes overtakes; all pit entries, exits and tyre changes contribute to factual counts, including omitted crowded events. Quiet phases are stated plainly. No diagnostic condition value, causal undercut claim or invented incident is projected.

The display holds its previous published Classification during feedback, then reconciles to the exact received result. Playback defaults to 3 seconds, is configurable from 0–10 seconds, and supports skip/replay. Reduced motion reconciles immediately. A refreshed result without an earlier local snapshot starts with the current authoritative result. Playback only reveals recorded highlights; it does not calculate physics, control the server clock or delay a disconnected display's controller. Refresh/replay uses the same bounded result timeline.

An accepted complete plan runs one phase and automatically opens the next decision window, clears the confirmed plan and visibly resets Standard/Stay Out/Neutral. There is no separate Continue action. The original accepted acknowledgement retains its resolved phase/revision; retries return it without advancing again or relocking newer defaults. The last result cursor and bounded events remain available during the next window. Finish disables further plans and does not restart automatically. Final rows retain completed laps and two-compound eligibility, including ineligible finishes; first across the line is not automatically described as an eligible winner. The rule is explained before choices. Restart same preset creates a new session with the original preset/distance/designer seed; choose another preset returns to explicit designer setup.


## Submission and connection recovery

The server serializes creation and phase mutations. Each accepted command identity has a normalized, driver-sorted payload fingerprint and recorded acknowledgement. A retry with the same plan returns that acknowledgement before stale-phase checks; reusing the identity with different choices is a conflict. Identical concurrent retries share one result; different concurrent commands permit only one acceptance. Delivery failure does not undo accepted choices or allow another resolution. Malformed, incomplete, wrong-team, stale and unavailable instructions reject without locking either car.

A private snapshot includes only the controlled team's confirmed plan, never opponents' choices. Refresh and authorized SignalR rejoin recover those accepted instructions, current observations and lock/result state. Subscriptions join validated server groups before retrieving their latest snapshot. Clients reject older/duplicate revisions. An HTTP acknowledgement confirms the lock, but does not turn old tyre observations into new ones: the controller fetches full current snapshots afterward.

Connection status is visible. During disconnection or reconnect, instructions are unavailable. Automatic reconnect reauthorizes group membership and fetches current snapshots; initial connection failure has an explicit Reconnect action. If the HTTP outcome is unknown, the controller retains the original command UUID and exact two-car plan, checks the server on reconnect and permits an explicit retry only if the server is still awaiting decisions. Definite rejection retains editable choices. If the server already accepted the plan, recovery retrieves the newer editable window or the confirmed final plan and removes the pending retry. Drafts and uncertain command identities survive within the current page only; refreshed pages retrieve server state. No durability is claimed across server restart.

Focused browser checks deliberately interrupt negotiation/WebSockets, drop real team notifications, lose an HTTP acknowledgement, resend delayed real SignalR frames and alter one malformed submitted car at the HTTP boundary. They verify visible recovery against the real server, not component methods. Browser tests run serially because the local server supports one active session. Generated smoke images now go into ignored .tools/reports/browser-race; reviewed historical evidence remains in .scratch/browser-dry-race/evidence.

See [playtest and designer replay](browser-playtest.md) for duration sources, separate diagnostic credentials, accepted-plan replay and local multi-device instructions.

The boundary suite runs the actual API on Kestrel with a server-only test preset catalogue. Initial driver gaps place an owned car in a queued or ongoing stop at a real engine phase boundary. HTTP/SignalR and rendered browser checks verify stop restrictions, atomic replacement rejection and recovery. Production presets and player configuration allowlists are unchanged. Run this suite separately from ordinary browser tests because both use ports 4410/4510.
