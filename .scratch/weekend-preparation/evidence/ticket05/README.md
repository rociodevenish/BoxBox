# Ticket 05 focused evidence

Base: integration `c44351643e90161caf6344d62a2323b9a1520310`. User-approved seams: public API/SignalR, stateless public simulation policy and a small rendered Guided flow. No broad suite, hosted deployment or human playtest is claimed.

## Red and green

The API test was written first. `.tools/reports/05-api-red.json` records PID 18276, exit 1, 14.330s: expected ten final-ready unclaimed teams, actual zero. The subsequent initial green passed the same complete AI/human/recovery/freeze path before adding record replay and household privacy assertions.

Final affected API command, from repository root with pinned Node 24.19.0/Java 21 on PATH and `F1_RECOVERY_HOST=127.0.0.1:19340`:

```text
node tools/run-bounded.mjs --timeout-ms 180000 --report .tools/reports/05-api-final.json -- node tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:exec --only auth,firestore --project demo-f1game --config .tools/firebase05.json --non-interactive "dotnet test tests/Game.Api.Tests --artifacts-path .tools/dotnet05 -p:NuGetAudit=false --filter FullyQualifiedName~Preparation --nologo"
```

Fourteen affected API cases passed: twelve existing confirmation/freeze/recovery cases plus two new guidance cases. PID 25456, exit 0, 21.854s. New coverage proves ten automatic teams prepared once while the human stays provisional, versioned provisional/final records replay exactly and survive real Firestore restart, feedback is preserved, duplicate BeginPreparation does not rerun AI, the full field freezes after human confirmation, and ordinary/public projections cannot retrieve automatic diagnostic records. A changed candidate makes the human's exact compatibility diagnostics differ while the legitimate policy observation/recommendation stays identical. Separate shared participants retain their own help settings; settings delivery via authorised SignalR preserves setup/team revision, and device revocation prevents further private GET/subscription/delivery.

Public policy command: `node tools/run-bounded.mjs --timeout-ms 90000 --report .tools/reports/05-policy.json -- dotnet test tests/Game.Simulation.Tests --artifacts-path .tools/dotnet05-policy -p:NuGetAudit=false --filter FullyQualifiedName~PreparationDecisionPolicyTests --nologo`. Two independently authored examples passed: contrasting straight/corner demands and opposing driver preferences produce known legal compromises, and Low versus High-confidence own feedback controls one cautious final revision. PID 23676, exit 0, 3.974s. The report preserves PowerShell's actual argument representation.

The known pinned `Microsoft.AspNetCore.SignalR.Common` 10.0.12 NU1903 finding (`GHSA-334f-vvp4-7g6p`) remains root follow-up. Command-local `NuGetAudit=false` isolates behavior checks and changes no dependency.

Browser test was also written first: missing **Preparation guidance** region caused the red run (session 32147, exit 1), retained in `.tools/reports/ticket05-browser-red.log`. The implemented flow then passed (session 94141, one test, 20.8s). Exact command from `src/frontend`: `node node_modules/@playwright/test/cli.js test --config playwright.shared.config.ts preparation-guidance.spec.ts`. UI 4505/API 4605/Auth 19349/Firestore 19340/hub 19550/logging 19600; isolated config `.tools/firebase05.json`, pinned Node/Java and existing Chromium cache. It verifies Guided → Standard → Assisted → Guided live settings, preserved manual drafts, explicit recommendation use, all fifteen legal controls, and no advice/driver content on the public display. Angular typecheck and `git diff --check` passed. A final affected browser rerun after adding direct private settings notifications is recorded below.

## Visual inspection

Final affected browser rerun against direct private settings notifications passed the same single case in 18.7s (session 68166, exit 0), retained in `.tools/reports/ticket05-browser-final.log`. Process identities in that log: emulator Node 17804, UI Node 24436, browser test Node 13144. The latest capture was re-reviewed and republished. These overlapping reruns do not increase the unique browser count. All owned services stopped and ports were checked clear afterward.

Reviewed [360px Guided preparation](../ticket05-guided-phone.png): readable circuit/driver identities, moderate setup suggestion labels, both-driver preferences/Adaptability, uncertainty and compromises, explicit apply button and all legal axis choices. The phone page scrolls; no horizontal overflow was observed. Existing portraits and track imagery/fallbacks are retained. The supplied collection still contains driver and track imagery only; originals and runtime mappings are unchanged. Root owns the final current 1920×1080 public-display inspection before its broad pass.
