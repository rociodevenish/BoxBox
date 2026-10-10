# Local household play

Requires the .NET SDK selected by `global.json`, Node 24.15+ (or 26+) and locked dependencies. Shared sessions use the existing dry-race engine, eleven constructor slots and twenty-two laboratory cars. Constructor selection does not change drivers or performance.

## Start on one computer

From repository root, install dependencies once:

```powershell
npm ci --prefix tools/auth-emulator
npm ci --prefix src/frontend
```

Start the auth-only emulator in one terminal:

```powershell
cd tools/auth-emulator
npm start
```

Wait for `All emulators ready`. The demo project is `demo-f1game`; Auth uses 127.0.0.1:19099, hub 19400 and logging 19450. The UI is disabled. This setup needs no Firebase cloud login or Java.

In another terminal at repository root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:FIREBASE_AUTH_EMULATOR_HOST = '127.0.0.1:19099'
$env:Firebase__ProjectId = 'demo-f1game'
dotnet run --project src/backend/Game.Api --urls http://127.0.0.1:4510
```

In a third terminal:

```powershell
cd src/frontend
npm run start:race
```

Open `http://127.0.0.1:4410/shared/new`. Create an email/password emulator account for the host. Another browser profile can use the public Join link or `/shared/join` and Join as guest. Accounts exist only in the local emulator.

## Phones, tablet and TV on the same LAN

Use the hosting computer's reachable LAN IPv4 address, for example `192.168.1.20`. All devices must reach that computer on the same trusted network; guest Wi-Fi isolation or a firewall can prevent access. Permit inbound TCP 4410 and 19099 for this local session. The API stays on loopback behind the frontend proxy.

Create an untracked file outside the repository's default/test configuration, for example `$env:TEMP/f1-auth-lan.json`:

```json
{"emulators":{"auth":{"host":"0.0.0.0","port":19099},"hub":{"host":"127.0.0.1","port":19400},"logging":{"host":"127.0.0.1","port":19450},"ui":{"enabled":false},"singleProjectMode":true}}
```

Stop the loopback emulator first. From repository root start this LAN instance:

```powershell
node tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:start --only auth --project demo-f1game --config "$env:TEMP/f1-auth-lan.json" --non-interactive
```

Keep the backend environment above and add the browser-visible emulator address before starting the API:

```powershell
$env:Firebase__EmulatorUrl = 'http://192.168.1.20:19099'
dotnet run --project src/backend/Game.Api --urls http://127.0.0.1:4510
```

From `src/frontend` start the frontend with the existing local API/WebSocket proxy:

```powershell
npm run start:race -- --host 0.0.0.0
```

Open `http://192.168.1.20:4410/shared/new` on the host device and use its generated join link/code/QR on the other devices. Do not open the loopback URL when creating the LAN lobby: generated links use the browser origin. `FIREBASE_AUTH_EMULATOR_HOST` remains the backend's loopback `host:port` without a URL scheme; `Firebase__EmulatorUrl` is the full LAN URL used by every browser. Restart the API after changing these values. Stop all three services after play.

These instructions are checked against the actual config/commands. Physical LAN reachability, QR scanning and household usability remain a human playtest task; local browser contexts cannot prove them.

## Assign teams and play

Claim one constructor per participant. The host approves guest claims or corrects assignments before Start race. Add named shared participants through Host controls and assign each to a constructor with Shared tablet or Open family. Player responsibility and device permission remain separate. Start requires at least one claimed team and all claims approved; it freezes human teams and scripted fill-ins. Later revocation does not remove a required human team.

- **Competitive:** private controllers, including private pass-and-play on a trusted shared tablet.
- **Family:** designated Open teams intentionally share their legitimate estimates, choices and accepted instructions.
- **Mixed:** choose Private, Shared tablet or Open family separately for each team.

Use each device's authorised controller link. A public race display shows Classification and Locked/Deciding readiness. Open family spectator display additionally shows designated Open team information; it has no submission authority. An Authorised TV controller link gives an assigned device the same atomic two-car submission as a private controller. Joining or knowing a code/device ID alone grants no team or host authority. Additional devices require an explicit host grant before start through the public API; the lobby does not yet provide an additional-device grant UI.

For each team, choose pace, battle and pit instructions for both cars, Approve Plan, then Confirm plan. The first accepted team lock is final for that Race Phase. Early teams wait for every required human team. The final lock resolves once and opens the next decision window automatically; outcome playback or Skip playback reveals it locally. A disconnected public display cannot stall progression. Continue until Finish confirmed and team final results.

A shared tablet starts at Pass to team. Hand over the tablet, then press Ready for that team. Locking moves to the next neutral handoff. Pass tablet discards unsubmitted drafts and clears private cards, notifications and the previous subscription. Reloading returns shared tablets to neutral before Ready. This is a privacy courtesy on a trusted household device; it does not authenticate each participant against another device user. Private responses use no-store, and the frontend has no service-worker/offline private cache.

## Host controls and recovery

The host returns to the Household lobby to Pause play, Resume play or Revoke controller access for a listed device. Pause retains accepted plans and required teams. A pause during resolution lets the committed outcome finish and holds the next opening. Resume opens the held window once. Revocation clears obsolete private authority/subscriptions and explains lost permission on the controller; another explicitly authorised device can retain access. It cannot invent choices or take over an absent team.

Refresh retrieves current membership, assignment, readiness and lifecycle revisions. Accepted locks remain final. Firebase retains browser authentication; the app retains the server device ID keyed by account/session and display name. Use distinct browser profiles for separate identities; named shared participants remain distinct player identities on one authorised tablet. Current permissions are checked on every command/private subscription. Public payloads exclude auth UIDs, email, private grants, seed and hidden simulation diagnostics. Open views expose only designated teams' legitimate observations.

Recovery is in-process only: a server restart loses lobbies and races. An absent required player leaves play waiting. There are no decision deadlines, fallback choices, AI takeover, assistance or durable recovery. Production hosting/account management is outside this implementation. Local emulator token acceptance is restricted to Development with a demo project.

## Household playtest still pending

On actual devices, complete one six-lap dry race with two private controllers, another with several tablet participants, and Family/Mixed sessions with an authorised TV controller plus a separate spectator. Check joining by QR/link/code, both-car decisions, handoff clearing, refresh, pause/resume and revocation. Record device/browser/network details, failed join attempts, participant explanations of privacy, observed handoff times and difficulties managing both cars. Ask participants what was intentionally Open and what remained private. Record measurements and quotations only after observation. Do not count automated timings or viewport emulation as household findings.

## Automated verification

Stop the manual services first: checks reject occupied emulator/test ports. Install Chromium once with `node src/frontend/node_modules/@playwright/test/cli.js install chromium`, then from root:

```powershell
node tools/verify.mjs
```

The single broad pass runs tooling, frontend typecheck/build, real-token API/SignalR and simulation regressions, gallery, solo browser, pit-boundary and household browser checks. Set `DOTNET_EXE` and `PLAYWRIGHT_BROWSERS_PATH` only when using non-PATH SDK/cache locations. Test UI/API ports default to 4471/4571, independently of manual 4410/4510. Auth ports remain 19099/19400/19450. Commands own and clean up their process trees; immutable run manifests and command/PID records live under `.tools/reports/verification/runs/`. After a fix rerun affected named steps only.

See [ticket 06 automated evidence](../.scratch/shared-sessions/evidence/06-household.md) and [earlier lobby evidence](../.scratch/shared-sessions/evidence/01-lobby.md). Existing initial-bundle and lazy qrcode CommonJS warnings remain. Firebase Auth loads on household routes. Constructor names map stable reference IDs to synthetic simulation teams; existing portraits retain their reviewed driver mappings. No track/driver/car design coefficients or original assets changed.

Official references: [Auth emulator connections and token behavior](https://firebase.google.com/docs/emulator-suite/connect_auth), [ID-token verification](https://firebase.google.com/docs/auth/admin/verify-id-tokens).
