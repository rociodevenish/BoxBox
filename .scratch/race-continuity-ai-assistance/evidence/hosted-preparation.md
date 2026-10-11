# Ticket07 local preparation evidence

Date: 2026-10-11. No cloud project supplied, no cloud deployment performed, no family playtest or cloud usage measured.

Implemented Docker build recipe, Firebase static SPA Hosting and deny-all Firestore rules configs, separate production env example, public API-origin writer, provisioning/deployment/compatible rollback commands, distinct task/sweep numeric OIDC identities and one-minute independent repair with bounded queue retries/concurrency. Production startup refuses missing Firebase/persistence/wakeup/origin settings, emulator config, unsafe origins, wrong callback audience and shared task/sweep identities. CORS and explicit Origin enforcement cover API/hub upgrades. Membership and existing loopback/key designer diagnostics remain enforced.

SharedLobby, OpenDisplay, RaceSession and RacePage use one runtime-origin helper for every REST and SignalR connection. Config contains no credentials. Optional F1_DOTNET_ARTIFACTS_PATH passes native flags to verification launchers; fixture content root receives F1_REPO_ROOT. Root preview PID 22668 was preserved.

Focused checks:

- 12 public API configuration/origin tests passed (`.tools/reports/hosted-config-final.json`): rejected API/hub/negotiate origins, accepted preflight, fail-fast variants and safe public auth config.
- 1 real Chromium direct-origin smoke passed (`.tools/reports/hosted-browser.json`), frontend 4497/API 4597, no frontend proxy. Real session POST and SignalR WebSocket used API origin; 22-row public display and refresh recovery passed. Local HTTP/WS does not prove hosted HTTPS/WSS, scale-to-zero or 60-minute timeout recovery.
- Angular type check passed (`.tools/reports/hosted-type.json`). Production build passed (`.tools/reports/hosted-build.json`); 395.27 kB initial exceeds 300 kB warning budget but below 500 kB error budget; QR CommonJS warnings remain.
- API Release publish passed (`.tools/reports/hosted-publish.json`). Docker image pull/build was not executed; hosted rollout remains pending.
- Smoke servers terminated and 4497/4597 have no listeners. No emulators needed for this public smoke. Root owns full verification and combined 360/1920 visual evidence.

First origin-test run failed because late ConfigureAppConfiguration injection missed startup capture. WebHost UseSetting tests the actual early config boundary and passes. No emulator value was presented as hosted evidence.

Runbook covers canonical URL bootstrap, SA uniqueId resolution, Scheduler baseline costs, budgets, bounded diagnostics, retention gaps, compatible rollback and family/latency/usage collection. Retention is operational policy, not automatic Firestore deletion. Rollback never rewinds Firestore. All hosted acceptance and measured costs remain pending.
