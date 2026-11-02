# S16-01 focused verification

Implementation base: integration `ca2529e59789d8d13c30b301a7f294263b30b017` (includes dated factual provenance and the audited .NET security prerequisite). Final implementation commit is the commit containing this document. Ticket [#21](https://github.com/rociodevenish/BoxBox/issues/21) remains open for independent review. This evidence approves the representative pilot only; full calendar/personnel/card coverage and real full-season storage/balance acceptance remain later tickets.

## Public seams and results

- **23/23 content acceptance tests passed**, normal NuGet audit, SDK 10.0.402 / runtime 10.0.13. Actual Auth/Firestore emulators, manual wakeups, unique build output. `ReleaseContentTests` covers 18 invalid-pack cases, supported pilot creation/retry/restart after deployment catalogue removal, unsupported explicit saved candidate versions without overwriting the save, lost committed-creation acknowledgement, and identical complete 22-car/6-lap public-domain race results from public preparation inputs after restart/catalogue removal. No content-parser test boundary was added.
- Invalid cases include missing circuit/constructor/asset references, wrong unit labels, numeric kilometre-to-metre conversion errors despite a metre label, invalid detailed bounds, zero travel time, inconsistent passing opportunities, unsupported/missing consumer declarations, unsupported reserve-driver traits and staff keys, invalid selected **and unselected** package effects, inconsistent source metadata, mismatched Simple group labels, insufficient unique personnel and a 95-credit affordability dead end where each individual price remains affordable.
- The actual saved initial two-event/eleven-constructor pilot is **126,062 UTF-8 bytes**, including its frozen construction/source/asset metadata. This number is captured in the passing TRX output. It is not an estimate of later season history.
- Focused legacy creation/restart/reservation/start-retry tests, historical understood source labels and unsupported/damaged save checks passed in the targeted regression run. Its aggregate was 37/38 because the newly added full-field fixture incorrectly tried to let one participant claim eleven teams; the application correctly rejected that. The fixture now uses eleven named shared participants with explicit Family/Open assignments. The corrected full-field proof passes in the final 23/23 run. Production claim rules were unchanged.
- **Browser content flow passed, 1/1**, 27.5 seconds: create permanent account, explicitly choose reviewed pilot, create campaign, claim McLaren, begin preparation, reload, assess practice, confirm Final-Ready, inspect public display, start the same first weekend, reload and open Q1. Existing originals/runtime assets loaded. Angular's browser compilation passed. The full broad regression is owned by the integration agent and was not duplicated here.

## Inspected captures

All three captures were viewed before broader integration checks. Published through the evidence tool from ignored gallery output; originals were not edited.

| Viewport | Capture | Assessment |
| --- | --- | --- |
| 360×800 phone, full document 360×4366 | [Calendar and complete preset roster](s16-01-calendar-360.png) | Two circuit images and all eleven paired rosters remain readable; no horizontal overflow. Reviewed pilot/season-opening roster/provisional tuning wording is visible. Long constructor names fit. |
| 360×800 phone, full document 360×7005 | [Complete prepared team flow](s16-01-prepared-360.png) | Setup controls, neutral package, compatibility, frozen practice and both readable driver cards remain distinct through the complete flow; names and labels fit. Portraits retain intended faces/crop, and track illustration stays separate from demand facts. |
| 1920×1080 public display | [Public preparation](s16-01-preparation-public-1920.png) | Circuit image/demand summary and all-team readiness are clear, with ample contrast and no overlap. Private choices/ratings/preferences are not shown. Illustration is not treated as simulation geometry. |

## Bounded records and isolation

The ignored records have been preserved outside the worker worktree under `.tools/reports/s16-01/`, including all failed/passing bounded JSON records, the browser verification run manifest, original captures and `.tools/reports/s16-01/trx/acceptance-api.trx` with every final test result. Named final records:

- `s16-01-acceptance-api-envelope.json`: launcher PID 6056, 27,229 ms, exit 0.
- `s16-01-acceptance-api.json`: dotnet PID 10588, 20,142 ms, exit 0; 23 passed, none failed/skipped.
- `s16-01-final-targets-envelope.json`: stronger 95-credit budget and actionable unsupported-version checks, 2 passed.
- `verification/runs/2026-10-30T11-48-33-737Z-11882626-edfb-4bc3-b2fc-06c9db317dbe/result.json`: browser launcher PID 45344, 28,084 ms, exit 0.

Expected TDD reds are retained: unknown version initially accepted; candidate initially rejected; source/consumer coverage, reserve/staff consumers, group labels, explicit saved-version compatibility, coherent-but-unsupported constructor identities, unselected package consumers and numeric unit conversion each exposed missing behavior before its corresponding fix. Early launch failures and the baseline NU1903 restore blocker are retained rather than overwritten. Temporary audit suppression was used only to isolate early behavior; all final results use normal audited restore after the integrated SDK/package security fix.

Owned ports were checked free before launch: Auth 20109, Firestore 20100, hub 20410, logging 20460, websocket 9251, UI 4491, API 4591. No listeners remained on those ports after final verification. No existing preview was reused. Browser and API fixtures explicitly use manual wakeups. API outer/inner commands are bounded to 240 seconds with process-tree cleanup; browser uses the existing bounded verification runner. The final API TRX, bounded command records and per-run browser manifest retain launched identities/timestamps.

Reproduce the browser slice with `node tools/verify.mjs content`, setting the usual `DOTNET_EXE`, Java 21 PATH, `PLAYWRIGHT_BROWSERS_PATH`, `F1_DOTNET_ARTIFACTS_PATH`, `F1_TEST_PORT` and `F1_API_PORT` for local isolated tooling. The content config uses `tools/auth-emulator/firebase.content.json`. For focused API tests use that same Auth/Firestore config, set `FIREBASE_AUTH_EMULATOR_HOST=127.0.0.1:20109` and `F1_RECOVERY_HOST=127.0.0.1:20100`, and run `dotnet test tests/Game.Api.Tests/Game.Api.Tests.csproj --filter FullyQualifiedName~ReleaseContentTests --artifacts-path <unique-path>` inside the emulator lifecycle. These are local emulator results, not hosted CI/deployment acceptance.
