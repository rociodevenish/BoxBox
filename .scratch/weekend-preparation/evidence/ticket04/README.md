# Ticket 04 focused evidence

Implemented against integration `7d24b27573fb540624ae2f0bce0f9db420a22b4d`, using the approved public API/SignalR and focused rendered-browser seams. This record covers S07 only. The full regression suite, hosted deployment and human acceptance remain separate.

## Red and green

The browser tracer test was extended first. Tool session 35551 exited 1 after completing setup/practice/reload, at the missing `Confirm final-ready setup` button (`toBeEnabled`, element not found). This was observed before confirmation implementation. Subsequent green runs replaced Playwright's original error context; `.tools/reports/ticket04-browser-red.txt` explicitly records a reconstruction from that observed session, not an original retained artifact.

Final browser command from `src/frontend`: `node node_modules/@playwright/test/cli.js test --config playwright.shared.config.ts preparation.spec.ts`. Node 24.19.0, Java 21, UI 4504, API 4604, Auth 19349, Firestore 19340, emulator hub 19550 and logging 19600; isolated `.tools/firebase04.json`. Both tests passed in 24.5s. The final run exercised the same confirmation GUID after an uncertain request and Refresh, persisted final-ready after reload, disabled edits, explicit reopen with fixed feedback, and shared-tablet confirmation clearing both summaries before the next Ready. Dedicated output: `.tools/reports/ticket04-browser-green.log`. Managed process identities included emulator Node 15584, UI 9752 and browser 13920; the runner exited and owned listeners were absent afterward. Angular `ngc -p tsconfig.app.json --noEmit` passed.

Focused backend command, from repository root with pinned Node/Java on PATH and `F1_RECOVERY_HOST=127.0.0.1:19340`:

```text
node tools/run-bounded.mjs --timeout-ms 240000 --report .tools/reports/04-api-final.json -- node tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:exec --only auth,firestore --project demo-f1game --config .tools/firebase04.json --non-interactive "dotnet test tests/Game.Api.Tests --artifacts-path .tools/dotnet04 -p:NuGetAudit=false --filter FullyQualifiedName~PreparationTests --nologo"
```

Twelve public API tests passed, including five confirmation/reopen save-interruption cases and three complete eleven-team freeze cases. They cover current authority, invalid/stale commands, cross-operation GUID conflict, unchanged assessment after revision/reopen, private/public projections, real Firestore restart, SignalR saved-state delivery, no success publication after failed save, before-write rollback, read-outage cached rollback, commit-then-failure receipt recovery, full-field atomic lock, post-lock mutation rejection and immutable retry receipts. Full-field fixtures assign all eleven teams to real shared participants; they do not bypass AI preparation. The first completed 12-case run recorded PID 12592, exit 0, 20.643s in `.tools/reports/04-api-final.json`; later assertion strengthening is recorded separately. Services stop after each emulator invocation.

The unmodified restore first failed NU1903 for pinned `Microsoft.AspNetCore.SignalR.Common` 10.0.12, advisory `GHSA-334f-vvp4-7g6p`. The command-local `NuGetAudit=false` override isolates behavioral verification; it does not remediate or hide the repository dependency finding, and no package versions were changed by this ticket.

The final strengthened run additionally checked partially ready-field rollback and malformed/stale confirmation rejection: twelve cases passed, PID 9200, exit 0, 20.821s in `.tools/reports/04-api-strengthened.json`. This overlaps the preceding focused runs and does not increase the unique count of twelve tests. `git diff --check` passed. `git merge --no-edit codex/weekend-preparation` reported already up to date at the required integration tip before the completion commit.

## Visual inspection

Reviewed [360px final-ready phone flow](../ticket04-final-ready-phone.png): circuit live text and supplied track image remain readable, locked choices are visibly disabled, the original assessed setup is labelled separately, both supplied driver portraits and feedback/confidence are visible, and Reopen follows the complete two-driver summary. No horizontal overflow was observed. The screenshot is a full-page capture; scrolling is expected on a phone. The existing public display flow remains covered by ticket 02/03 evidence; root owns the final current 1920×1080 review before broad verification.

The supplied collection remains driver/track imagery, using existing stable identity mappings and runtime fallbacks. No imagery or card stars were converted to physics; versions and provisional mappings remain as documented in Practice Assessment v1. No new asset category or original asset mutation is introduced.
