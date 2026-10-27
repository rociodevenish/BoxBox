# Assisted and Guided decisions — focused acceptance

Ticket05 adds per-player Standard/Assisted/Guided settings and an explicit, optional saved-recommendation timeout choice. Host changes take effect at the next decision window. Recommendations are computed once from the ticket02 legitimate observation policy before human choices, then saved with the decision window and recovered unchanged. Acceptance copies both suggested car choices into the normal draft/review/confirm path; it does not bypass command validation or lock approval.

Standard omits strategic recommendations from its team payload. Assisted and Guided include the concrete plan, phase/window revision, observation time, reasons, uncertainty and legal option groups. Current policy confidence remains false: Guided retains all legal choices rather than asserting that estimated tyre state supports pruning. Pending stops and forced Neutral states receive explicit explanations. No help setting modifies simulation performance, budget or championship state. Advice uses private authorised team channels; intentionally open Family/Mixed teams expose it through their established open channel. AI responsibility hides human assistance/actions while membership remains assigned.

## Focused verification

- Public API RED: the assistance configuration request returned404 before implementation (`.tools/reports/s06-assistance-red.json`).
- Final public API GREEN: all4 AssistanceTests passed after integration04 merge, including per-player deferred settings, frozen repeated reads, Standard omission, host authority/privacy, real Firestore restart and exact saved recommendation timeout instructions, and an intentionally open team with a pit stop queued across a phase boundary. Bounded report `.tools/reports/s06-assistance-api-four.json`; inner report `.tools/reports/s06-assistance-inner.json`.
- Guided browser GREEN: one executable phone flow passed, including host settings, explicit timeout selection, shared participant assignment, fresh advice and uncertainty, retention of all three pace options, applying both-car advice, normal review/confirm, accepted lock across reload, and no private recommendation on the public display. Bounded report `.tools/reports/s06-assistance-browser5.json` (37.1 seconds).
- After integration04 merge: API test project build passed with zero warnings/errors; Angular template/type compilation passed. Broad suite is reserved for root integration verification.

Local checks used Node24, Java21 and isolated Auth19319, Firestore19310, hub19520, logging19570, frontend4495 and fixtureAPI4595. Emulator startup reports other demo-project instances; ports and owned processes remain isolated. Browser retries corrected test locators for native select accessible names and the actual locked waiting-status text; these were locator failures, not new behavioral failures.

## Captures and limits

[Guided phone,360×800 viewport](assistance-guided-phone-360.png) is a full-page360×3816 capture. Saved two-car plan, reasons, uncertainty and all legal groups remain readable without horizontal clipping; actions are reachable by intentional vertical scrolling. Advice adds substantial height; a future per-car disclosure could reduce scrolling. This focused fixture uses synthetic Driver1/Driver2 labels, so root's combined actual-preset capture owns stable identity confirmation.

[Ordinary public display,1920×1080 viewport](assistance-public-1920.png) shows timing/readiness and no private advice, with no horizontal overflow. Its full-page capture includes the fixture's development race-facts panel. Human strategy quality and speed were not measured; policy thresholds remain hypotheses documented by ticket02 benchmark evidence.

The host also receives an early Continue to next choices affordance during an unpaused phase-result, using ticket06's idempotent UUID/phase/public windowRevision contract; automatic progress remains server-owned.
