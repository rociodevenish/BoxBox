# S12-05 — Acquisition browser completion and privacy

Basis: `97bd125`, branch `codex/s12-05`. Extends the two existing acquisition browser flows rather than introducing parallel copies. The public campaign API remains the fixture boundary for the 77-pick completion; UI interactions cover comparisons, Scout, reserve rejection and recovered final hire.

## Observed regression and correction

The first focused run failed after a committed Scout response was deliberately delayed: pressing **Hide private personnel** removed the view, but releasing the response reopened it (`Your private personnel`: expected 0, received 1). Retained manifest: `.tools/reports/s12-05/browser-red.json`, launched PID7712, failed after47232ms.

The campaign page now invalidates its private-view generation on Hide and Refresh. Open, Hire and Scout responses update personnel only when their generation remains current. Accepted commands still clear their saved retry identity, so hiding a view does not undo a committed action. The regression verifies the delayed Scout cannot reopen the private region, reload stays neutral, and deliberately reopening reads the one-token debit. Existing lost-acknowledgement Scout/Hire retries still pass. Host revocation followed by refresh removes the old device's open-personnel control; a direct private read from that device returns403.

## Focused results

- `browser-focused.json`: 2/2 passed, PID36036,62852ms. Includes complete roster, reserve rejection, delayed-response privacy, fixed offers and lost acknowledgements.
- `browser-final.json`: 2/2 passed, PID3048,59039ms. Adds revoked-device403 and forced driver-image failure, retaining live names and Hire controls. Both cases are extensions of existing tests.
- Manager and Technical Engineer offers visibly say **Future economy effects · No current performance bonus** and retain readable staff initials. Race Engineer offers explicitly say Setup improves the assigned driver's Practice Assessment confidence; Tyre Reading, Strategy and Communication are inactive, with no pace or error bonus. Driver preferences remain distinct from quality, and all real portraits load before the normal capture.
- The complete flow retains 360px width assertions, scouts through actual legal offers, rejects a reserve-breaking hire with409, finishes all77 mandatory places, recovers a lost final-hire acknowledgement, and shows seven final role assignments plus the protected18-credit car reserve. Automatic picks remain unactivated during manual evidence authoring; no timing-dependent AI progression is used.

Runtime: Node24.22.1, SDK10.0.401, Java21. UI4695/API4795; Auth22309, Firestore22300, hub22310, logging22360, Firestore websocket9330. Unique emulator configuration and retained bounded-run manifests live under `.tools/reports/s12-05/`. Each Playwright run owned its servers (`reuseExistingServer:false`) and completed normally; the bounded manifests pin the launch identities. No broad verification was run by this worker.

## Visual review before broad verification

Inspected actual generated captures using `view_image`: the complete360×800 phone flow (full-page driver comparison, reserve rejection and final ready roster), and the actual1920×1080 public page at scroll position0. Driver photographs, live names, prices, preference explanations, reserve errors, buttons and final ledger remain contained. Staff initials and truthful future-only labels are legible. The long phone page scrolls vertically without horizontal overflow. The desktop top contains the campaign state and calendar; it has no private balance, offers, ledger or advice after Hide. These are current captures, not the historical04 evidence.

Reviewed files published explicitly with `tools/publish-evidence.mjs`:

- [Driver comparison,360px](s12-05-driver-offers-360.png)
- [Reserve rejection,360px](s12-05-reserve-360.png)
- [Completed roster,360px](s12-05-ready-360.png)
- [Public top,1920×1080](s12-05-public-top-1920.png)

Supplied assets retain the existing stable identity manifests and provenance described in [04 evidence](04-effects-advice.md); this slice changes no asset packaging or simulation inputs. Forced portrait failure confirms the live textual identity and action remain available independently of imagery.
