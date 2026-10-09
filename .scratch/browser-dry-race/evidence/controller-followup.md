# Controller follow-up verification — 10 October 2026

Approved scope: [controller feedback](../controller-feedback-20261009.md). Implementation pin before final review fixes: `7f0c180`. The first controller-flow browser regression failed against the former Phase heading before the flow was implemented. The new API timing projection tests likewise began red.

## Implemented behavior

Approve Plan opens the two-car confirmation modal; Confirm plan submits atomically. Resolving presentation hides driver and decision panels, displays ordered recorded events and the resolved Classification, then automatically returns to next-lap decisions. Three-second local playback can skip or reconcile immediately for reduced motion. The server window remains independent; replay cannot relock revealed choices. Refresh/reconnect and delayed acknowledgement cases preserve that state. Shared-team waiting copy is prepared for the subsequent decision-barrier ticket; this solo preview does not simulate a waiting team.

Both controlled drivers show their supplied portraits and published provisional cards, with explicitly pinned synthetic-to-card identities and unchanged fixture physics. Each card's vicinity/recorded pace/decisions are colocated. Ongoing settled screens omit the whole public result tower; Replay can show it again. Full detail drill-down retains 1–100 detail values. The analysis modal cycles Tab and Shift+Tab, including nested driver details Escape and focus return to the originating row.

Five recent full lap intervals exclude the initial rolling partial lap; strict fastest duration, earliest completion, then ordinal driver ID resolves ties. A dedicated public Stops column counts completed PitExit only. The legal battle buttons after playback are compared with fresh authorized server observations. The six-lap test correctly offers five laps initially and one remaining lap afterward. Finished screens show final instructions.

## Checks

- Independent full backend verification: **140 passed**, comprising 110 simulation and 30 API tests, including six actual Firebase-emulator lobby tests and two public recorded timing tests. No failures or skips.
- **21 unique live browser scenarios passed** across the full run and focused corrective reruns: outcome-first/skip/reduced motion, source-card/vicinity/recorded pace, fresh battle options, retries/reconnect/refresh/delayed acknowledgements, privacy/navigation, five 50-lap cases and exact designer replay. Repeated stops retain the lapped eligible finish.
- **34 unique gallery scenarios passed** across the original run and affected reruns, including keyboard compound badge selection and modal focus. Historical visual evidence overwritten by capture tests was restored unchanged.
- **Two queued/ongoing stop-boundary browser scenarios passed** with rejected replacement preserving both drafts and authoritative snapshots.
- Final Angular typecheck and production build passed: **345.70 KB raw / 82.63 KB estimated transfer**. The existing 300 KB initial-bundle warning and qrcode/dijkstrajs CommonJS warnings remain visible; these are not described as a warning-free build.

The 22 race-roster runtime WebP portraits total **715,346 bytes**; the first two controlled portraits total **60,776 bytes**. Variants are 384×576, Pillow Lanczos, WebP quality 82. Original supplied PNGs/provenance remain intact. Two legacy gallery PNGs remain packaged separately from these live assets and the JavaScript bundle.

## Visual evidence

- `controller-followup-decisions-360.png`: genuine 360px phone width, full scrollable page; both real supplied mapped portraits, colocated cards/vicinity/recent times/choices and Approve Plan.
- `controller-followup-resolving-360.png`: genuine 360px phone width, full recorded playback; decision/driver panels absent, chronological event playback precedes resolved Classification.
- `controller-followup-display-1920x1080.png`: true shared-display viewport; all 22 rows, completed Stops and fastest marker visible.
- `controller-followup-finish-1920x1080.png`: true final public viewport; recorded completed distance/eligibility and Stops remain visible.

No human household findings are inferred from automation. No new ticket was started. Frozen previews at 4430/4440/4450 and their APIs were preserved. Final independent review and new frozen preview are coordinated by root.
