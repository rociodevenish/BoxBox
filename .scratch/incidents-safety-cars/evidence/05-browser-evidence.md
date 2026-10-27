# 05 browser evidence

All captures reviewed at a 360x800 reduced-motion controller viewport or a 1920x1080 reduced-motion public viewport. Tall captures include the complete scrollable page; viewport dimensions are not capture dimensions.

- `05-safety-car-360.png`: independent Hard and Medium pit choices, both car cards, approval and replay controls remain readable without overlap.
- `05-safety-car-incident-1920.png`: all 22 Classification rows, observed Spin events and yellow Damaged status for Drivers 5 and 21 are readable together.
- `05-retirement-classification-1920.png`: both player retirements and their Mechanical Failure/Retirement events survive automatic neutralisation and reload, alongside exact recorded Classification.
- `05-opening-interruption-360.png`: factual interruption heading, 22 recorded grid/start entries and Safety Car choices link.
- `05-opening-safety-car-360.png`: opening interruption at 1.4s, both independent car choices, readable approval controls.
- `05-opening-incident-1920.png`: opening failure/retirement event panel and all 22 entries, including Liam Lawson's Retired status.
- `05-opening-restart-360.png`: normal controls reopen after host continuation and refresh; both own retired cars retain factual no-instruction status.
- `05-active-restart-360.png`: a complete active-car restart phone flow. Both drivers retain normal Pace controls; Driver 1's committed Hard stop is still ongoing, and Driver 2's fitted Medium tyres and legal next choices remain readable. Approval is available.
- `05-retired-controls-360.png`: the retirement follow-up disables both cars' Pace/Pit choices and disables approval of an empty plan. The focused `retired-controls-final-command.json` passed after these assertions were added. The all-retired team is absent from the human barrier; the household's AI/deadline progression remains authoritative. The earlier opening-restart capture predates this disabling fix and is retained as opening-flow history, not as current control-state evidence.

Verification records (ignored): `.tools/reports/s09-05/browser-run-5.json` passed all 3 focused quickrace tests (PID 21404, 21140ms); `opening-browser-run-2.json` passed the real Auth/Firestore opening flow (PID 9280, 37922ms); `browser-damage-run.json` passed the affected Spin case after damage mapping (PID 6072, 35663ms). UI/API ports4525/4625 were owned by each Playwright run and servers were stopped by Playwright. Emulator ports19389/19380/19590/19640/9162 were owned by the opening run and released before the backend recovery run.

Source capture directory: `.tools/reports/s09-05`. Reviewed files staged explicitly under `.tools/reports/gallery/s09-05-reviewed` and published with `tools/publish-evidence.mjs`; historical evidence files were not rewritten.
