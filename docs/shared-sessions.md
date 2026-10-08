# Local household lobby

Requires .NET 10, Node 24 and the installed Playwright Chromium cache for browser verification. This ticket adds authenticated lobby creation, joining, eleven constructor claims, host approval/correction and shared-device participant assignments. Starting freezes the human/scripted grid and initializes the existing race engine. Multi-team decision submission belongs to ticket 02; this build deliberately offers no shared race lock endpoint yet. Existing solo laboratory routes remain playable.

## Start locally

Install the pinned auth-only emulator once:

```powershell
cd tools/auth-emulator
npm ci
npm start
```

Wait for All emulators ready. The demo project is `demo-f1game`, Auth listens on 127.0.0.1:19099, the emulator hub on 19400 and logging on 19450. No Firebase login, cloud project or Java is required for this auth-only setup. The UI is disabled. These ports avoid the gallery and existing race previews.

In another terminal, from repository root:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:FIREBASE_AUTH_EMULATOR_HOST = '127.0.0.1:19099'
$env:Firebase__ProjectId = 'demo-f1game'
& 'C:/Program Files/dotnet/dotnet.exe' run --project src/backend/Game.Api --urls http://127.0.0.1:4510
```

Then:

```powershell
cd src/frontend
npm ci
npm run start:race
```

Open `http://127.0.0.1:4410/shared/new`. Create an email/password account or sign in, choose a name, preset, distance and privacy mode, then create a lobby. This creates only emulator-local accounts. Another browser/profile can open the Join link or `/shared/join`, enter the code and join anonymously. The QR encodes only that public join link. Hosts use a permanent email/password identity; guests can sign out on setup to switch accounts.

Claim one constructor per participant. Guest claims await host approval. The host may correct an assignment, return a team to scripted control, or add a named shared participant and assign that participant to the host's device using Shared tablet or Open family. Approving/correcting records the device assignment separately from the responsible player. Start requires at least one claimed team and approval of every claimed team. Unclaimed teams receive frozen phase-start scripted choices. Repeated start returns the same frozen grid; later claims/corrections cannot change it. A public race display shows the initialized 22-car order.

Competitive permits private and shared-tablet arrangements; Family defaults claimed teams to intentionally Open; Mixed supports each arrangement per team. Tablet handoff, active open controls and the required multi-team decision barrier follow in tickets 02–04. No private strategic information is exposed by this lobby.

## Identities and recovery

Firebase UID identifies an authenticated account. Each lobby has its own opaque player identities and server-issued device identities. Constructor identity, responsible player and device assignment are separate. Named shared participants have their own player identity rather than becoming the host or device. Join codes and links locate a lobby and request membership; neither grants host or team permission.

Every lobby mutation and SignalR subscription verifies the Firebase ID token and current lobby/device membership or host identity. The backend uses FirebaseAdmin 3.7.0; browser auth uses Firebase 13.0.0. Invalid, expired and wrong-project tokens reject. Local unsigned emulator verification is restricted to Development with a demo project; a real deployment must configure credentials and its client API key and is not supplied here. Request URL information logging is suppressed so WebSocket query tokens do not enter ordinary request logs.

Firebase maintains browser authentication; the app retains only the server-issued device ID keyed by account/session and a display name. Refresh retrieves current membership and a revisioned lobby snapshot, then rejoins the authorized SignalR group. Older lobby updates cannot replace a newer snapshot. Public roster DTOs contain names and assignments, not auth UIDs, email addresses, private grants, seed, tuning or driver estimates. Server restart loses in-memory lobbies and races. Shared-device handoff is not yet implemented and no durable recovery is claimed.

The host account and a guest account should use distinct browser profiles/contexts while checking separate identities. Actual phones require a trusted LAN emulator URL and bindings; the multi-device procedure and household privacy checks are completed in ticket 06. This local ticket does not claim physical-device or household usability findings.

## Fixture and asset mapping

The eleven stable constructor IDs match the reference team IDs reviewed in `.docs/data/f1_strategy_driver_cards_2026.json` (schema 1.0.0, data 2026.10.08-initial-balance). The pinned display names map to synthetic simulation teams 01–11. Claiming McLaren or Ferrari does not assign its real drivers, alter attributes or derive simulation values from provisional stars. A complete constructor/livery catalogue remains authoring work. The car and track inputs retain their reviewed provisional versions and are not imported as new coefficients.

The current supplied portrait and circuit asset directories were inspected, including Albert Park imagery. They do not represent constructor identity or the synthetic OpenSegment/RestrictedSegment laboratory layouts. This lobby uses readable constructor text and existing charcoal/coral panel patterns. No real portrait/circuit image is misleadingly assigned to a synthetic driver or track; originals are untouched.

## Verification

Keep the emulator running for the real-token API suite:

```powershell
dotnet test F1Game.slnx
cd src/frontend
npm run typecheck
npm run build
npm test
npm run test:shared
npm run test:browser
npm run test:boundary
```

Browser suites use ports 4410/4510 and run sequentially. `test:shared` can start the pinned auth emulator when it is absent; its readiness timeout is 180 seconds, not a fixed sleep. The other browser suites preserve the solo/recovery and actual pit-boundary checks. Gallery tests use 4400 independently. The browser QR library is pinned qrcode 1.5.4 and loaded lazily; its CommonJS build warnings are understood. Firebase Auth is loaded only on household routes. A patched grpc-js override 1.13.6 removes the umbrella Firebase dependency's audit issue; Firestore is not imported by the game. The frontend audit is clean.

See [ticket 01 evidence](../.scratch/shared-sessions/evidence/01-lobby.md) for actual checks and inspected capture dimensions.

Official references: [Authentication emulator and project configuration](https://firebase.google.com/docs/emulator-suite/connect_auth), [server ID-token verification](https://firebase.google.com/docs/auth/admin/verify-id-tokens).
