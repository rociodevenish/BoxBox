# Two-car controller review — 8 October 2026

The private gallery entry is `/?view=controller`. `TwoCarController` receives a private team view model and emits a snapshot of both instructions after confirmation. `ControllerGallery` alone supplies local acknowledgement, rejection, loading/resolving and availability fixtures. There is no session validation, simulation, opponent-plan access or rendering bundle. The public entry remains isolated from private controller DOM.

Draft choices persist across submission acknowledgement/rejection for the same decision phase; a new phase initializes its supplied choices. Submitted means waiting for acknowledgement, while Locked means acknowledgement has arrived. Rejection keeps the complete draft editable and provides a correction/resubmission message. Pending stops show their known requested compound with disabled pit controls; battle availability and explanation come from the fixture. Tyre ranges are visibly labelled estimates. A real adapter must provide validated instructions and availability, not infer them from this component.

Rendered tests exercised choice changes, both-car confirmation, waiting state, acknowledgement locking, rejected draft recovery, pending Hard stop, unavailable battle, loading/resolving disabling and keyboard completion. The keyboard wrap test failed before explicit dialog focus cycling was implemented; Tab/Shift+Tab remain inside the confirmation and Escape returns to Review. Primary controls are at least 44×44 pixels, with 8-pixel choice gaps and 12-pixel confirmation gaps. Selected controls use dark text on the established coral token. Existing token contrast and reduced-motion rules apply; no new animation was introduced.

Inspected selected, confirmation and rejected captures in portrait and landscape. Long names wrap without truncation. Phone car sections stack; widths of at least 700 pixels use two columns. All controller controls and dialogs stay inside the viewport horizontally. Phone pages intentionally scroll to the second car; confirmation presents both cars together. The 800×360 confirmation uses compact spacing, stays centred with margins and keeps both actions fully visible. This is desktop Chromium review, not a physical-device usability study.

| Viewport | Selected full-page PNG | Rejected full-page PNG | Confirmation viewport PNG |
| --- | --- | --- | --- |
| 360×800 | 360×2190 | 360×2214 | 360×800 |
| 800×360 | 800×1400 | 800×1424 | 800×360 |
| 768×1024 | 768×1398 | 768×1422 | 768×1024 |
| 1024×768 | 1024×1332 | 1024×1332 | 1024×768 |

Files are `controller-WxH.png`, `controller-rejected-WxH.png` and `controller-confirm-WxH.png` beside this document. Per-viewport JSON records the rejected page dimensions and confirmation rectangle; filenames describe viewports, not full-page capture heights. Earlier `controller-360.png`/`768.png`/`1920.png` are ticket 01 static compositions, preserved separately.

Verification: all 23 rendered frontend tests passed, production Angular build passed (181.65 kB raw / 51.24 kB estimated transfer), and strict Angular template/type checking passed with Node 24. The existing supplied-portrait size warning remains unrelated to this text-only controller. No new assets were needed; user originals remain untouched. The controller deliberately uses dense form regions rather than repeating full portrait cards, retaining driver identity, coral selections and established spacing/contrast.

