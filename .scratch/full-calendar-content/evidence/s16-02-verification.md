# S16-02 focused verification

Ticket [#22](https://github.com/rociodevenish/BoxBox/issues/22) remains open for independent review. New immutable version `release-2026-full-circuits-v1` enables the verified 23-event calendar. The pilot and legacy shorts remain compatible; the separate personnel candidate remains distinct until #24 composes the final release. Source facts and authored coefficients are separated in [the profile audit](s16-02-circuit-profiles.md).

## Approved public seams and results

- **46/46 complete race/replay cases:** all 23 circuits ran complete 22-car six-lap races and forced-spin Safety Car/pit scenarios. Each serialized/deserialized replay reproduced the complete phase trace, every public window advanced time, all entries remained classified and every race terminated. The four rain-arrival profiles were then strengthened after a genuine red showed the original slow ramp never reached a six-lap weather reaction; all eight affected cases passed again.
- **46/46 final pit-route cases:** 23 controlled public two-car worked examples match all quoted nominal Pit Loss values, plus all 23 affected full-field forced-pit races/replays pass again. A genuine red exposed Monza losing 25.054545 seconds while its summary said 22; lane transit now integrates baseline time over the bypassed 12% of DISTANCE, respecting the nonuniform sectors. [Coverage table](s16-02-race-coverage.md) overlays the latest affected results. Unchanged natural no-pit cases were not repeated.
- **10/10 focused public API checks:** eight new cases plus two legacy checks passed with actual Auth/Firestore emulators and normal dependency audit. These cover exact literal 23-event order, Bahrain-brand-to-physical-Sepang mapping, restart after deployment catalogue removal, incomplete/invalid coverage rejection before reservation, corrected-command retry, and unsupported frozen weather-model rejection without overwriting history. The latter was a genuine red: recovery initially returned OK; frozen full conditions now reach the public race constructor without reopening a deployment catalogue. Restoring compatible inputs recovers the campaign. Understood old source labels/default short creation remain supported.
- **1/1 final browser flow:** 18.5 seconds test / 49.7 seconds bounded launcher including startup. Create the full pack; verify all 23 images/order; reload; inspect Bahrain/Sepang/dates; block its image and inspect the named fallback; restore; use the constructor shortcut; claim McLaren; prepare/practise/confirm Final-Ready; inspect public preparation; start the same first weekend; reload and open Q1. Manual wakeups keep presentation deadlines controlled.

These checks consume campaign APIs and public Weekend Preparation/RaceEngine/SimulationReplay operations. No private content-parser seam or new physics vocabulary was added. Forced diagnostic incidents never enter a campaign. Full 23-event campaign progression, economy/history growth and final balance remain #25 acceptance; the integration agent owns the single broad pass after card coverage.

## Inspected captures

All six final captures were inspected and explicitly published. Existing original/runtime assets were not modified.

| Capture | Viewport | Assessment |
| --- | --- | --- |
| [Created calendar](s16-02-calendar-360.png) | 360×800 | Selected full-calendar wording and direct constructor action remain clear before the long list; no horizontal overflow. |
| [Bahrain/Sepang card](s16-02-sepang-360.png) | Card in 360×800 | Event brand, physical circuit, October dates and authored summary remain separate and readable. |
| [Deliberate image gap](s16-02-image-gap-360.png) | Card in 360×800 | Named neutral fallback preserves event, circuit, dates and summary. |
| [Calendar overview](s16-02-calendar-1920.png) | 1920×1080 | Readable hierarchy, contained imagery and clear nominal Pit Loss wording. |
| [Prepared controller](s16-02-prepared-360.png) | 360×800 | Shared setup/demands fit the phone; the complete practice/final-ready flow succeeds. |
| [Public preparation](s16-02-public-1920.png) | 1920×1080 | Circuit name, image, demands and eleven-team readiness remain readable; private choices stay on authorised controllers. |

A real browser red caught a relative fragment resolving against `<base href="/">` and leaving the campaign. The final shortcut includes the campaign path, so a phone host can reach constructor claims without scrolling through 23 events. Representative images are reviewed here; full card/asset treatment remains #24. All 51 supplied originals were re-audited against the stable inventory, including the current primary asset collection; hashes and all 23 runtime mappings are unchanged.

## Storage, bounded records and scope

The actual initial API campaign measured approximately **1.10 MB logical JSON / 26.5 KB stored lossless envelope**, including 23 events and 253 frozen team presets. The API TRX records 1,100,243 logical bytes / 26,465 envelope bytes before the final lane-coefficient correction; final browser creation/reload succeeds after that correction. These are initial-save observations, not a promise about full-season history. The integrated bounded lossless storage prerequisite preserves history; no bound was silently raised here.

All failed/passing bounded records, TRX, original gallery captures and browser artifacts are preserved outside this worker worktree at `.tools/reports/s16-02/`. Key records:

- `s16-02-all23-circuit-races.json`: PID 46220, 173,709 ms, 46 passed; `trx/all23-circuit-races.trx`.
- `s16-02-rain-arrival-regression.json`: PID 28840, 16,485 ms, 8 passed.
- `s16-02-pit-route-regression.json`: PID 38296, 34,430 ms, 46 passed.
- `s16-02-final-api-envelope.json` / `s16-02-final-api.json`: 10 passed; `trx/final-api.trx` retains individual names and measurement output.
- `s16-02-browser-final.json`: PID 30864, 49,670 ms, exit 0.

Meaningful reds are retained: unavailable full mode, missing pit route accepted, unsupported saved weather consumer accepted, slow rain ramp, nominal Pit Loss mismatch and wrong relative fragment. Prototype build/launch failures remain recorded. Final checks use SDK 10.0.402 / runtime 10.0.13 and normal audited restore. These are local emulator/browser results, not hosted CI acceptance.

Owned ports: Auth 20109, Firestore 20100, hub 20410, logging 20460, websocket 9251, UI 4491, API 4591. Services belong to bounded fixture lifecycles; existing previews were not reused. Cleanup is verified in the final handoff. The parallel-demo-project warning reflects workers with explicit separate owned endpoints.

## Integration merge proof

Integrated personnel/storage tip 90ddaf59ae97ebfc5651f797e5f6001fc4dba2c7. The merge preserves pilot, full-circuit and personnel version registrations, both optional metadata additions, both selector labels and accessibility labels. Only the actually affected checks were rerun: merged frontend type-check passed (6,452 ms), and full-calendar creation/restart plus personnel initial acquisition passed 2/2 with actual emulators (merged-registrations.trx). These records are included in the preserved report directory; no circuit matrix or broad suite was repeated for the registration merge.
