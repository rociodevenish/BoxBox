# S06 integration record

Implementation started 11 October 2026 on `codex/race-continuity-ai-assistance`, from `26447566c568a0214991660cc7e89c35aeef7d60`.

Existing uncommitted roadmap/spec edits belong to the user and are excluded from implementation commits. The S06 title in the working copy identifies this effort as S06.

## Verification environment

- Node: bundled runtime `C:/Users/sbdev/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe`, v24.19.0. Default shell Node v20.18.0 is unsuitable for the repository checks.
- .NET: SDK 10.0.401, matching `global.json`.
- Java: Microsoft OpenJDK 21.0.12.1 ARM64, unpacked in ignored `.tools/java/jdk-21.0.12.1+1`; process-scoped PATH only. Source: [Microsoft OpenJDK downloads](https://learn.microsoft.com/en-us/java/openjdk/download).
- Firebase CLI: existing locked 15.33.0. Firestore emulator v1.22.0 downloaded through its setup command; bounded download report `.tools/reports/s06-firestore-download.json`.
- Default emulator ports were occupied before S06 verification. Ticket 01 uses isolated owned ports rather than stopping existing services.
- Locked dependency installation uses ignored `.tools/s06-dependencies/` copies of the repository package manifests. An initial root frontend `npm ci` hit an existing preview's locked Lightning CSS native module after removing some packages; missing packages were restored from the isolated locked tree while retaining files in use. Bounded install records stay under `.tools/reports/s06/`.

## Evidence limits

The shared-session prerequisite's automated verification is complete; its actual household playtest remains pending. S06 implementation does not turn automated checks into human findings.

No deployment project has been selected. Hosted join/reconnect, scale-to-zero wakeup, deployment restart, actual family playtest and measured cloud costs remain pending. [Hosting research](../hosting-research.md) records documentation-based constraints and estimates separately from measured acceptance evidence.

Final integrated code SHA and verification records are recorded below. Later evidence/status-only commits do not change the tested implementation.

## Ticket 01

Merged at `e5a49fc62d8932e944bb83786a9b2919f1789c14`. [Durable recovery evidence](durable-recovery.md) records focused real-emulator checks and the separate snapshot/replay latency sample. Raw bounded command records are preserved in ignored `.tools/reports/s06/ticket01/` before worktree cleanup. Overlapping filtered checks are not added together as unique test counts.

## Ticket 02

Merged at `1102b3f7c7fff1c20ab3790593d39f851bd5dc9a`. [Observation-driven AI evidence](observation-ai.md) records 480 reproducible races, legality, uncertainty and policy hypotheses. Raw focused records are preserved in ignored `.tools/reports/s06/ticket02/`. Reported orphan testhost PID 11428 was no longer running when checked after completion.

## Ticket 03

Merged at `0cef3b09b1babc0f6ff9b97c5b00febde1c56b19`. [Deadline and fallback evidence](deadlines-fallback.md) records controlled boundary checks, persistence failure/recovery, and the reviewed 360px phone and 1920×1080 display captures. Raw bounded command records are preserved in ignored `.tools/reports/s06/ticket03/`. The combined S06 flow will be inspected again after control, assistance and presentation progression are integrated.

## Tickets 04–06 and combined visual check

Control merged at `f7940d5bfeb4a16062daf3a408827a74f529c58a`, assistance at `1782286d039e2a782edf0d8eda19a2ea5ad65436`, and progression at `d33f31defc334dbf3e541f4184bda61336e3352e`. Leaf evidence: [control](control-transfers.md), [assistance](assistance.md), [progression](recoverable-progression.md). Raw command records and named test-result directories are preserved under ignored `.tools/reports/s06/ticket04/`, `ticket05/` and `ticket06/`.

The merger composed one API route-registration conflict, retained every leaf module and passed an isolated API test-project build with zero warnings/errors. Existing preview PID 22668 locked ordinary API binaries and was preserved. Optional `F1_DOTNET_ARTIFACTS_PATH` now isolates verification output; fixture content-root discovery also works under that output layout.

At `d88037a`, root ran one combined Guided phone flow using ordinary `DryRacePresets`, real Auth/Firestore and owned frontend 4498/API 4598. It applied the saved suggestion, confirmed the two-car plan, reloaded with the accepted lock intact, and checked an ordinary public display without private advice. Passed in 43 seconds including startup; command record `.tools/reports/s06/integrated-visual03.json`. Earlier temporary-config attempts failed before product assertions because their server working directory was wrong; those records are retained separately.

Root inspected [Guided phone](s06-integrated-guided-phone-360.png) (360×800 viewport, full page 360×4047), [locked/reloaded phone](s06-integrated-locked-phone-360.png) (360×4380) and [public display](s06-integrated-public-1920.png) (1920×1080). No horizontal clipping; plan, freshness, uncertainty, human responsibility and normal approval are readable with intentional vertical scrolling. Accepted choices remain selected and approval disabled after reload. All 22 public classification rows fit, ending around 1054px, with no private recommendations. Existing portrait/constructor assets load. These supported presets deliberately retain synthetic Driver labels and do not claim production adoption of provisional 2026 ratings.

## Final local integration, 12 October 2026

Ticket 07 deployment preparation merged at `5c4d4045d87e8eb5c119d48384cfa405a707201f`. Independent review fixes merged at `ad600b78e91e105baf6e4ca82344460fd0fefad9`; residual regression fixtures at `67c44e78e021a7064dce2eff4df284aa1f072619`; final shared-session fixtures at **`bdf451128fdf9bd2bcbdb001a4f388ada79021ba`**. This last merge changes only shared tests/configuration and evidence, with explicit host Continue and manual test wakeups preventing the result-presentation window from racing handoff assertions. Production progression remains covered by real recovery, deadline and scheduled-callback checks.

All twelve required local verification steps have passing results. One initial broad invocation was followed by affected/remaining named steps, preserving failures and each immutable run manifest. Passing test counts below are distinct suites; overlapping focused implementer runs are not added to them.

| Checks | Passing result | Immutable run directory under `.tools/reports/verification/runs/` |
| --- | --- | --- |
| Tooling, Angular typecheck, production build | 3 tooling tests; typecheck/build passed | `2026-10-11T10-10-07-456Z-e653a8b8-2a97-43a2-b1ad-49f28a6f2ed7` |
| Gallery | 34/34 | `2026-10-12T10-21-27-140Z-07707e4b-6efa-454d-aca1-0ee69356da8f` |
| Backend, browser, stop boundary | 111/111 simulation; 102/102 API; 22/22 browser; 2/2 boundary | `2026-10-12T10-34-29-729Z-e728652b-153b-46fc-a814-283127ccb2af` |
| Assistance, control, deadlines, direct-origin browser | 1/1 in each suite | `2026-10-12T10-31-51-343Z-010a126c-dcb5-46d6-ae42-31ad2583a04c` |
| Shared sessions | 8/8 | `2026-10-12T10-39-18-760Z-63f33cf9-a3fd-40a8-83e0-564dc10b216f` |

Each directory contains `result.json` and bounded per-command records with revision, PID, start time, duration and exit status. Earlier passing checks were retained when later changes did not affect their implementation. Full diagnostic logs are in ignored `.tools/reports/s06/`; failed prior manifests remain intact. The 22 browser checks include five full 50-lap races and exact recorded-plan designer replays. The API suite uses real Auth/Firestore emulators. Expected injected connection/authentication failures appear in browser logs; all assertions passed.

The production Angular initial bundle was 395.27 kB: within the 500 kB error budget, above the 300 kB warning budget. Existing QR CommonJS warnings remain. Local release publishing was verified in ticket 07; Docker build, CI execution and cloud deployment were not performed.

Owned test frontend/API were 4498/4598, with recovery Auth/Firestore 19109/19100 and hub/logging 19410/19460; the control suite used 19309/19300 and 19510/19560. Final command PIDs were backend 49108, browser 51508, boundary 51068, assistance 30500, control 51728, deadlines 44068, direct-origin 11140 and shared 8680. After completion, an elevated listener check confirmed all these test ports clear. Existing previews, including the process locking ordinary API output, were preserved. All seven ticket worktrees and the clean review worktree were removed after preserving their reports. The review worktree's 36 report files, including TRX/screenshots, were copied with matching hashes into `.tools/reports/s06/review-fixes-worktree/reports`; dependency junctions were unlinked without recursively touching their targets. Only the root checkout remains.

[Independent review](review.md): Standards 2 heuristic findings resolved; Spec 1 functional finding resolved. Both reviewers confirmed the fixes independently. [Remediation evidence](review-fixes.md) records the narrower checks and fixture adjustments.

Tickets 01–06 are resolved locally. Ticket 07 and S06 are **ready-for-human**: the deployment runbook/configuration are prepared, but a selected cloud project/region, deployment authorization, hosted restart/idle/reconnect measurements, household feedback and actual cost evidence are still required. [Pending hosted acceptance](hosted-family.md) remains unchecked. Local direct-origin success and provisional AI comparisons do not satisfy those human/hosted gates. User roadmap/title edits remain unstaged; only the S06 status change is committed from its spec.
