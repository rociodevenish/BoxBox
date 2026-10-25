# S11 review remediation — 26 October 2026

Source checkpoint: `019f85f`, based on integration `a10fcbc0241d2fb637ade87a0bb55ceb7c9c7c8a`, branch `codex/s11-review-fixes`. Integration tip was merged before completion (already up to date). This folder adds evidence; historical archives and earlier ticket evidence are unchanged.

## Changes and approved seams

Authenticated campaign return now considers binding independently of device registration. A device registered during qualifying can return with the same cached identity during the race, revoke old private access and queue a grant at the next existing Race Phase boundary. Current authorised grants and repeated queued returns are idempotent. Accepted setup/practice and race locks, AI takeover/reclaim policy and qualifying lock remain intact. Next preparation can also bind an already registered device.

Write-time awards and recovery validation share the small pure frozen-entrant/finish mapping. Independent literal scoring/integrity expectations remain in existing tests. No scoring rule, archive or schema changed.

The app uses pathname branches rather than Angular Router. `@defer(on immediate)` inside the campaign, preparation and qualifying branches creates lazy page chunks only when those paths are selected. Existing navigation/authentication stays in place. Campaign-only deferral still exceeded the unchanged 500 kB initial error limit (505.59 kB); deferring the three campaign-flow pages passes at **427.34 kB**. Campaign/qualifying/preparation lazy chunks are 23.87/43.35/34.02 kB. The existing 300 kB warning and qrcode/dijkstrajs CommonJS warnings remain.

Public domain/campaign API/SignalR, real Auth/Firestore persistence/restart and focused browser interaction are the pre-approved seams. Test-owned `CampaignTestSessionScope` shares real persistence across coordinators in a scenario and registers IDs before adapter writes/on successful reads. Manual clock sweeps enumerate only those IDs, excluding campaign records. Normal xUnit parallelism remains enabled.

## Unique successful checks

| Check | Result | Durable record / launched PID |
| --- | --- | --- |
| Affected campaign API classes together, normal parallelism | **20/20**: NextEvent11, Results2, Recovery7 | [owner](parallel-pass-owner.json) PID25108; [dotnet](parallel-pass-api.json) PID34136, 54.267s |
| Literal public domain campaign scoring | **5/5** | [record](scoring-domain.json) PID37520, 3.802s |
| Published campaign archive replay | **pass**, two frozen weekends, qualifying/raw results/awards/cursor/championship rebuilds | [record](archive-replay-pass.json) PID2808, 3.259s |
| Production Angular build | **pass**, 427.34 kB initial under 500 kB error limit | [record](build-pass.json) PID17360; immutable run `2026-10-25T21-22-27-609Z-0ba04fa7-f9d0-4a28-94c2-5de773121b16` |

The seven recovery cases include the original new-device flow plus qualifying-time registration → same cached device return during race → restart → boundary grant, old HTTP/SignalR denial, accepted lock retention, pending AI takeover, explicit reclaim, repeat-return idempotency and cross-campaign denial. NextEvent11 reuses the already registered public-only device at next preparation across privacy modes and interrupted/uncertain transition matrices. They are 20 unique API cases, not the sum of all repeated runs.

## Red/green and retained attempts

- Original source: [active red owner](red-owner.json), PID29148 / [child](red-api.json): qualifying-registration case fails at old-device denial (expected403, actual200); original new-device case passes (1/2). [Initial green](green-owner.json), PID36316 / [child](green-api.json): both pass before extra idempotency assertions.
- Original source: [next-preparation red owner](red-next-owner.json), PID33900 / [child](red-next-api.json): all11 cached-device matrix cases fail at old-device denial (expected403, actual200). Restored fix: [next-event green](final-CampaignNextEventTests.json), PID36868:11/11; [result integrity](final-CampaignResultsTests.json), PID9168:2/2.
- [Combined intermediate attempt](green-final-owner.json) retained stale original incremental DLL after Copy-Item restored an older source timestamp, and also exposed cross-class manual-clock sweep interference. Source timestamp invalidated and proper scenario enumeration added. No production workaround or global parallelism disable was introduced.
- [Sequential intermediate owner](final-owner.json) retains an assertion-harness failure: a new idempotency assertion parsed the response stream twice. Parse-once correction yielded [recovery pass](recovery-pass-owner.json), PID22848:7/7. The final parallel20/20 run supersedes these intermediate attempts.
- [Campaign-only build](build-campaign-only.json) fails at505.59 kB. Final build above includes all three pathname deferrals; budget was not raised.
- [Restricted archive attempt](archive-replay.json) failed during restricted .NET build without diagnostics; narrowly escalated replay succeeded. A separate diagnostic restricted `dotnet build tools/CampaignEvidence -p:NuGetAudit=false --nologo` also failed with0 errors/0 warnings. No archive content was changed.

## Commands, runtime and reproduction

Node **24.22.1**: `C:/Users/sbdev/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe`; SDK **10.0.401** selected by global.json; Microsoft Java **21.0.12.1+1-LTS** from `C:/Projects/F1Game/.tools/java/jdk-21.0.12.1+1/bin`; cached Chromium under `C:/Projects/F1Game/.tools/playwright`. Existing locked frontend/auth node_modules caches are junctions; package pins are unchanged. .NET NuGetAudit suppression is invocation-only for the existing SignalR10.0.12 advisory.

All JSON command records contain exact argv, cwd, PID, start, exit and elapsed time. API owner command: `node tools/run-bounded.mjs --timeout-ms 360000 --report .tools/reports/s11-remediation/parallel-pass-owner.json -- node tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:exec --only auth,firestore --project demo-f1game --config .tools/reports/s11-remediation/firebase.json --non-interactive "node .tools/reports/s11-remediation/api.mjs"`. Set `F1_REMEDIATION_STAGE=parallel-pass` and `F1_REMEDIATION_FILTER=FullyQualifiedName~CampaignRecoveryTests|FullyQualifiedName~CampaignNextEventTests|FullyQualifiedName~CampaignResultsTests`. Set the Node/Java directories above on PATH.

The published runner/config/rules snapshots were executed from `.tools/reports/s11-remediation/`; copy them back there to reproduce their relative imports/paths. API runner supplies Auth19679/Firestore19680 and manual wakeups. Browser additionally uses `F1_TEST_PORT=4548`, `F1_API_PORT=4648`, `PLAYWRIGHT_BROWSERS_PATH=C:/Projects/F1Game/.tools/playwright`. Every server refuses occupied listeners. Config owns UI4548/API4648/Auth19679/Firestore19680/hub19960/log20010/websocket9186. Elevated named-port inspection confirmed all free before first launch. Bounded runs own cleanup; no generic process kill or personal-preview change was used.

Archive SHA256 remains `6A5CC13A5F29ABA900527D85875C55646518D879F05A89558106EEDB0B24F842`. Replay command: `dotnet run --project tools/CampaignEvidence -p:NuGetAudit=false -- --replay .scratch/campaign-championships/evidence/short-campaign.json.gz`. No current catalogue migration or historical evidence rewrite occurred.

No broad suite was repeated here. The integration owner runs remaining affected/named stages. Automated checks establish implementation behavior; household comprehension, multi-day human acceptance, hosted usage/cost and full-calendar acceptance remain unmeasured as before.

## Browser, reviewed captures and completion

The existing complete campaign flow passed **1/1**, [bounded record](browser.json), PID14448,35.006s (Playwright34.4s; test17.0s). Command: `node tools/run-bounded.mjs --timeout-ms 360000 --report .tools/reports/s11-remediation/browser.json -- node src/frontend/node_modules/@playwright/test/cli.js test --config .tools/reports/s11-remediation/playwright.config.ts`, with the runtime/ports above. It navigates shared authenticated account → campaign creation → team preparation → campaign → qualifying for both events, restores preparation in a fresh browser context, and verifies final reports/champions after reload. Production chunking is established by the production build; this interaction check uses the development server and real API/emulators.

Reviewed [360×800 actual viewport](s11-05-top-viewport-360.png) and [1920×1080 actual viewport](s11-05-top-viewport-1920.png): season completion, primary Constructors' champion and secondary Drivers' champion remain visible with readable live text and points. No clipping/overlap or horizontal overflow observed; reports/history continue below the fold. Both images were explicitly published into this new folder from ignored gallery output; earlier captures were not overwritten. The same browser flow also asserts missing-image fallbacks and report stability.

[Elevated owned-port closure](port-closure.json), checked2026-10-25T21:28:44Z, reports zero listeners across all seven assigned ports. Personal preview/default recovery and root verification ports were untouched. No preview remains owned by this worker.

Checkpoint fact: while final browser verification was underway, commit `019f85f93f4c11572c8017986d82fd3e15ffd6cf` appeared, authored/committed with configured identity `rociodevenish` at2026-10-26 08:27:49+1100. Neither this worker nor the integration owner reported creating it. Its full diff against the integration base was inspected: it contains only the intended source/test/runtime-doc changes and the intermediate remediation evidence already authored in this worktree. Tested source remained intact; the checkpoint was preserved without reset/rewrite. The completion commit adds only this README, final parallel/browser/closure records and two reviewed captures. `git diff --check` passes. The worktree is retained for independent merge.
