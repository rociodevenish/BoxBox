# S07 review remediation

Base: `b618faabf583c6aef681a3a000db4323c1b52d8a`. One implementer addressed the independent Spec P1 and both Standards duplication findings. The original reports are preserved outside the repository as `s07-spec-review.md` and `s07-standards-review.md` in the session visualization directory. This is S07 remediation only; S08 remains unimplemented.

## Information boundary correction

Removed `DriverFits` from `PreparationTeamSnapshot` and its projection, rather than merely hiding scores in HTML. Normal authenticated household and intentional Open GET/SignalR views now contain source preferences/Adaptability and, after the one assessment, its recorded uncertain observations. Saving any later candidate supplies no new exact fit or handling measurement. Designer simulation diagnostics and saved effective/replay inputs retain exact fits for engineering. Both-driver cards now label preferences separately from the frozen practice feedback; the copy no longer invites players to save choices to refresh a measurement.

The regression was written first at the approved API/SignalR seam. The red run failed because a private SignalR subscription contained `driverFits` (PID 8208, exit 1, 15.780s; `.tools/reports/review-api-red.json`). After removing the contract field, the same test passed (PID 17968, exit 0, 12.621s; `.tools/reports/review-information-green.json`). It covers private/Open GET and both SignalR streams before practice, one saved assessment, and four later Mechanical candidate revisions. Preferences remain available, exact fit/compatibility keys and seed/diagnostics are absent, and the same confidence-labelled uncertain feedback survives every revision.

## Narrow review refactors

`PreparationNotifications` centralizes household/Open team publication and the public preparation change sequence. Setup, practice, confirm/reopen, freeze and assistance notifications reuse it only after the authoritative mutation returns successfully from persistence. Open projection suppresses only the explicit Forbidden case; other failures still reach the existing notification logging/recovery handling.

`ValidatePreparationCommand` centralizes canonical GUID, required revisions and current assignment revision checks. `PreparationReceipt` centralizes accepted fingerprint comparison and original acknowledgement recovery. Each caller still authorizes current identity/device/assignment before these helpers. Operation-specific payload fingerprints and lifecycle/current-team-revision checks remain local, including old accepted-receipt retries after reopen/lock. No unrelated lifecycle or persistence changes were made.

## Focused verification

Pinned Node 24.19.0, .NET SDK 10.0.401 and Java 21 were used with isolated artifacts. Auth 19349, Firestore 19340, hub 19550, logging 19600 and Firestore websocket **9158** avoid root's separate broad-suite ports. Browser UI/API were 4508/4608. The known dependency audit finding remains root follow-up; `NuGetAudit=false` was command-local and dependencies were unchanged.

Affected API command from repository root (pinned Node/Java on PATH, `F1_RECOVERY_HOST=127.0.0.1:19340`):

```text
node tools/run-bounded.mjs --timeout-ms 180000 --report .tools/reports/review-preparation-final.json -- node tools/auth-emulator/node_modules/firebase-tools/lib/bin/firebase.js emulators:exec --only auth,firestore --project demo-f1game --config .tools/firebase-review.json --non-interactive "dotnet test tests/Game.Api.Tests --artifacts-path .tools/dotnet-review -p:NuGetAudit=false --filter FullyQualifiedName~Preparation --nologo"
```

All **15 affected API cases passed**, including the new information-boundary regression and existing authority, canonical retries, changed-payload rejection, one-assessment persistence, SignalR privacy, interrupted-write rollback/recovery and eleven-team atomic freeze. PID 22664, exit 0, 20.713s; command/report above. Earlier focused runs overlap this final set and do not add unique tests.

From `src/frontend`, `node ../../tools/run-bounded.mjs --timeout-ms 240000 --report ../../.tools/reports/review-browser.json -- node node_modules/@playwright/test/cli.js test --config playwright.shared.config.ts preparation.spec.ts preparation-guidance.spec.ts` passed **three rendered flows** in 66.111s (runner PID 31772, browser-test PID 27264; `.tools/reports/review-browser.log`). This covers the complete 360px setup/practice/final-ready/reopen path, tablet clearing, Guided draft preservation and public privacy. New assertions require both preference panels and prohibit the previous compatibility preview. `F1_SHARED_EMULATOR_CONFIG` points to the isolated config above; `F1_DOTNET_ARTIFACTS_PATH` points to `.tools/dotnet-review-browser`.

`node tools/verify.mjs typecheck build` passed only those two named steps, recorded in `.tools/reports/verification/runs/2026-10-16T22-01-12-273Z-358a92b3-1dad-4d9e-b9b5-8f5aaf44b2ee/result.json`: typecheck PID 25904/7.517s; build PID 33472/21.171s. Existing initial-bundle budget and qrcode/dijkstrajs CommonJS warnings remain; there was no build error. `git diff --check` passed. Root owns the prior all-12-step broad result and final independent recheck; this worker did not rerun a broad suite.

## Current reviewed captures and historical provenance

- [360px final-ready phone](review-phone-final-ready.png): complete 360×6068 page from the 360×800 flow. Both supplied driver portraits remain readable with preferences and Adaptability; only recorded practice feedback carries fit/confidence/uncertainty. No compatibility score or live candidate measurement appears. Controls, final-ready status and Reopen are readable without horizontal overflow.
- [1920×1080 public display](review-public-display.png): circuit illustration/demands and readiness count remain readable; no private setup, driver preferences or practice observations appear. The privacy copy now names preferences and recorded feedback accurately.

These captures were inspected before publication through `tools/publish-evidence.mjs`. Source captures came from this focused run's ignored `.tools/artifacts/s07-06-ui`, copied under distinct names to `.tools/reports/gallery` for constrained publication. Ticket 02–06 screenshots and the standalone ticket04/ticket05 phone captures are preserved unchanged as **historical pre-remediation** evidence: some show the exact-fit preview identified by review. Their README/index notices point here for the corrected current interface. Simulation reports retain their designer-only scope; hosted acceptance, human comprehension and measured preparation duration remain pending.
