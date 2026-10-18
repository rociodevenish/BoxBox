# S09 review remediation

Reviewed baseline: `4af4f4359afeb5dd0c65026668a971f1d19099c9`. Integration tip merged before completion (already up to date). Single remediation owner; no broad verification repeated here.

## Findings resolved

- Private own-car HTTP and SignalR observations now include the simulation's existing Good/Worn/Poor component and Low/Elevated/High risk estimates. The nullable extension is omitted for historical incident-free sessions. Public snapshots contain no component estimates; no exposure, threshold, draw or opponent diagnostic is added. The controller labels both estimates.
- Assisted/Guided Safety Car help saves pit-only advice from the already frozen legal automatic plan, own tyre estimates and public incident/neutralisation facts. Pace/Battle suggestions are absent. Only available cars receive new suggestions. Accepting help fills editable choices; accepted locks survive refresh/recovery and explicit auto-accept fills only missing teams at expiry. Standard help keeps recommendations private.
- Eight disruption fault tests incorrectly constructed their direct Firestore store without an emulator fallback. They now resolve F1_RECOVERY_HOST, then FIRESTORE_EMULATOR_HOST, then the established local recovery port, avoiding accidental cloud composition.
- Finished-plan submissions now return the existing lifecycle conflict before checking active-driver membership, while saved duplicate acknowledgements retain precedence.
- Reading a failed ordinary window save no longer clears its paused storage-error state by performing an unsolicited save. The historical Safety Car migration alone retains a scoped retry for its interrupted migration commit.

## Red/green and checks

- Own-band public API/SignalR test: red (missing `condition` property), then green, 1/1.
- Shared Safety Car help/recovery test: red (missing frozen recommendation), then green. Checks narrow advice, public privacy, saved recommendation equality after restart, accepted lock precedence and recommendation timeout source.
- Ten reported backend regressions reproduced with explicit isolated hosts: 8 passed, 2 failed. After lifecycle and save-retry corrections, the final affected set passed **16/16**, including all ten original failures and six SafetyCarWindow checks. Final bounded record: `.tools/reports/review-remediation/api-final.json`, PID **13008**, exit 0, 14.082 seconds. Separate artifacts: `C:/Projects/F1Game/.tools/artifacts/s09-review-fixes`.
- Angular compiler typecheck passed after the UI changes.
- Focused rendered browser acceptance passed **1/1**, 360×800 reduced-motion controller and 1920×1080 public display. It executes suggested compound choices, locks, refreshes, verifies unchanged saved advice and disabled editing, and checks public estimate/advice privacy. An initial assertion mixed innerText/textContent whitespace; corrected to compare rendered text. Final bounded record: `.tools/reports/review-remediation/browser-final.json`, PID **26004**, exit 0, 13.431 seconds. UI/API ports **4528/4628**. Separate artifacts: `C:/Projects/F1Game/.tools/artifacts/s09-review-browser`.
- Inspected [360px controller](s09-review-advice-360.png): readable stacked advice and both-car estimates, no horizontal overflow or overlap. Full-page height reflects the two-car advice plus existing driver cards. Published only this reviewed capture; historical evidence is unchanged.
- `git diff --check` passed.

## Isolation and reproduction

Owned emulator ports: Auth **19419**, Firestore **19420**, hub **19610**, logging **19660**, websocket **9164**. Preflight rejected occupied ports. Saved config `.tools/reports/review-remediation/firebase.json`; listener identities `.tools/reports/review-remediation/emulator-listeners.json`: Firebase PID **29260**, Java PID **11436**. Owned emulator launcher was stopped after checks. No shared/global process cleanup was used.

The advice browser test lives in `src/frontend/tests/browser-safety-car/advice.spec.ts` and opts in with `F1_SAFETY_CAR_ADVICE_AUTH=1`. Use `playwright.safety-car.config.ts`, the above isolated Auth/Firestore settings, `Firebase__ProjectId=demo-f1game`, `Firebase__EmulatorUrl=http://127.0.0.1:19419`, `F1_CONTROLLED_CLOCK=1` and `Wakeups__Mode=manual`. Normal Safety Car browser runs skip this auth-dependent slice. Dependencies use Node 24, Java 21 and existing cached Chromium; no cloud login is needed.

Household timing/comprehension and hosted human acceptance remain outstanding as recorded by ticket 06; this remediation makes no new calibration or human-playtest claim. Root owns the subsequent affected backend and remaining named verification stages.
