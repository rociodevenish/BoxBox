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

Open http://127.0.0.1:4410/race/new, choose preset/distance/designer seed, and open the separate public display and controller links. Select both cars, review and submit. The authoritative server broadcasts resolving, runs the existing engine to the next phase boundary and broadcasts its exact result. This slice ends after one phase. Continuation and resilient reconnect/retry are subsequent tickets. The fixture gallery remains runnable with npm start on port 4400. The race proxy forwards /api/** and /hubs/** to 4510. No cloud services or renderer are needed.

Game.Application owns session state and opponent policy; Game.Api supplies HTTP and SignalR. Angular holds draft instructions and presentation only. Four allowlisted segmented presets (open-equal, restricted-equal, open-mixed, restricted-mixed) support distances 6/10/20/40/50. Each has 22 cars, 11 teams, fixed rolling order with 0.5-second gaps and fresh Medium tyres. Mixed fields vary car fit and a small driver Pace offset, so do not represent equal driver attributes. The human owns team-01 and driver-01/02.

Player, team and device identities are distinct. A separate random controller capability is hashed on the server and conveyed in the controller URL fragment. Keep that link private. Session IDs authorize no private observations or commands. Private subscriptions validate that grant before joining their server-defined group.

Public and private snapshots use explicit allowlists. Public Classification includes timing, compounds, stop status and filtered overtake/pit events; private snapshots include only owned cars' legitimate tyre estimates and legal battle options. No seed, tuning, exact condition, randomness, opponent choices or raw engine result enters player snapshots. Initial timing uses known rolling gaps instead of the engine's zero-time zeros. Later unavailable timing remains unavailable; adjacent gaps are not invented by subtracting incompatible leader gaps.

External phase IDs start at zero. Revision 1 is awaiting-decisions, accepted revision 2 resolving, revision 3 phase-result or finished. Commands carry expected phase/revision, UUID and both owned cars. All choices validate before mutation. Frozen dry-opponents-v1 scripts read each opponent team's legitimate phase-start observation, choose conservative pace for estimated worn tyres and a legal compound stop when due, otherwise Standard/Neutral. They do not react to human choices. Complete accepted plans and script version remain internal for later diagnostic replay.

The server holds one in-memory session. Creating another replaces it; restart clears it. Stale views report unavailable and need a new local playtest. Local run uses loopback only; this is not deployment/authentication infrastructure.

Verification:

```powershell
dotnet test F1Game.slnx
cd src/frontend
npm run typecheck
npm run build
npm test
npm run test:browser
```

Browser smoke starts API 4510 and Angular 4410 itself; stop manual processes on those ports first. Playwright needs installed Chromium or PLAYWRIGHT_BROWSERS_PATH. API tests negotiate SignalR LongPolling through TestServer and verify exact engine outcome, ordered lifecycle updates, permission boundaries and atomic rejection. Browser test uses real server and separate pages, verifies capability denial and both-car submission, then observes the five-lap result.

The minimal live controller/display establish one phase. Ticket 03 integrates polished foundation components; ticket 04 adds full-race presentation. Current 1920 × 1080 capture partly clips row 22, so is not evidence that all rows fit. Complete table remains in the page. No living-room TV playtest is claimed.

