# Ticket 06 rendered preparation evidence

Source integration: `2368ab1` (complete revision in [manifest](ui-manifest.json)). Only the two focused browser specs changed; no production or original asset files changed. This evidence uses real local Firebase Auth/Firestore emulators and the public authenticated API/SignalR flow. It does not claim hosted acceptance or a human playtest.

## Reproduction and result

From `src/frontend`, pinned Node 24 and Java 21 on PATH, existing Chromium cache `C:/Projects/F1Game/.tools/playwright`, command-local `NuGetAudit=false`:

```text
F1_TEST_PORT=4506
F1_API_PORT=4606
F1_SHARED_AUTH_PORT=19349
F1_SHARED_FIRESTORE_PORT=19340
F1_SHARED_EMULATOR_CONFIG=../../.tools/firebase06.json
node ../../tools/run-bounded.mjs --timeout-ms 240000 --report ../../.tools/reports/s07-06/ui-final.json -- node node_modules/@playwright/test/cli.js test --config playwright.shared.config.ts preparation.spec.ts preparation-guidance.spec.ts
```

The ignored emulator config uses Auth 19349 / Firestore 19340 / hub 19550 / logging 19600, websocket 9150, disabled emulator UI, `demo-f1game`, and a local copy of unchanged `tools/auth-emulator/firestore.rules`. Servers refuse existing listeners. Final run: **three cases passed**, 26.5s Playwright / 27.032s bounded process, PID 13436, exit0. [Immutable runner record](ui-final.json). Observed child Node identities: emulator 2088, Angular 2284, browser 14652. All seven assigned ports had no remaining listeners after completion, as recorded in [manifest](ui-manifest.json). The preceding successful run (`ui-focused-03.json`, PID 13224, 29.640s) overlaps these three cases and is not an additional unique count. Earlier startup failures are retained under `.tools/reports/s07-06/`: a misplaced rules path was diagnosed and corrected in ignored config before the successful runs.

The final phone case uses a touch-capable context and an actual tap on the Preservation choice, plus keyboard radio navigation. It verifies loaded circuit/portrait images, a minimum 44px choice target, save/practice/final retry command identity after interrupted HTTP delivery, unchanged assessment after final revision/reload/reopen, disabled final-ready choices and no second assessment. Request interception only aborts delivery or images; it supplies no mock API/auth/state responses. Refresh is browser recovery, not process restart; actual restart and rejected-command durability belong to the separate API evidence.

Tablet coverage assigns two real shared participants, clears the first team's private summaries and draft before the second team, assesses practice, confirms final-ready, clears private content on confirmation, explicitly returns Ready, and reopens. Guided coverage changes Guided → Standard → Assisted → Guided over authorised live settings while retaining manual drafts, explicitly applies advice, retains all 15 choices and excludes guidance/cards/controls from the public projection. Runtime assets retain the existing stable driver/circuit identities and provenance; no imagery or card stars became simulation inputs.

## Reviewed captures

These are full-page controller captures at a 360×800 phone viewport and 768×1024 tablet viewport; the public captures are 1920×1080 viewport captures. Original source pixels were inspected in bounded native-width image crops because a tall phone image otherwise appears scaled down in the preview. The final rerun retained the same layout and content assertions. SHA256 values identify the published originals in the manifest.

- [Phone final-ready](phone-final-ready.png) and [reopened](phone-reopened.png): live Monza identity and supplied image, readable trade-offs, locked choices, separately labelled original assessed setup, both driver portraits, confidence/uncertainty and bottom Reopen control. No horizontal overflow at 360px. The full flow is long (about 7.4 phone viewports for final-ready); scrolling is required. Automation does not establish whether players understand it quickly.
- [Tablet final-ready](tablet-final-ready.png) and [reopened](tablet-reopened.png): readable paired driver summaries, uncertainty and preferences in two columns, clear final-ready state and explicit clear/pass action. No horizontal overflow at 768px. The track image is letterboxed to preserve its full supplied composition.
- [Guided phone](guided-phone.png): explicit one-team compromise, both drivers' preferences and Adaptability, uncertainty, an explicit Use recommended setup action and legal alternatives remain legible. Advice appears before the choices and increases scrolling.
- [Phone missing-image fallbacks](phone-fallback.png): supplied portraits are replaced with initials and live full driver names plus Portrait unavailable; feedback remains readable. Missing track imagery retains the live circuit name/demands. Deliberately aborted image requests test fallback only.
- [Public display](public-display.png) and [missing-track fallback](public-fallback.png): circuit image/demand text and 10 of 11 teams ready fit 1920×1080. No private setup choices, driver cards, advice or designer diagnostics appear. This projection remains public even in the signed-in host browser context.

No visible clipping or overlapping text was found in these reviewed states. The remaining human observations are separate and **pending**: comprehension of the compromises, whether both-driver feedback helps decisions, and actual preparation duration. Automated command elapsed times above are verification runtime, not player timing. Qualifying resolution and S08 are outside this record.
