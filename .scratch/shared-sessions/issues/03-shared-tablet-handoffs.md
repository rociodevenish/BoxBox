# 03: Share a tablet with private handoffs

**What to build:** Several household players use one authorised tablet, revealing only the active team's private screen after an explicit handoff.

**Blocked by:** 02 — Lock multiple teams and resolve together.

**Status:** resolved

- [x] Shared-device authority covers an explicit team list while participant identity remains separate from device identity.
- [x] Show a neutral pass-to-team screen and require Ready before fetching/subscribing to the next team's private view; identify the active team clearly while editing.
- [x] On lock or neutral exit, clear rendered private data, drafts and notifications and unsubscribe the previous stream; explain discarded unsubmitted drafts.
- [x] Previous choices never remain visible behind the handoff screen and private views are excluded from offline/service-worker caches.
- [x] Team identity and assignment revision prevent late messages from populating another team's view; refresh restores an appropriate authorised state without exposing the previous team during handoff.
- [x] Verify complete shared-tablet interactions, keyboard/touch access, refresh and late updates through browser tests and API/SignalR checks.
- [x] Explain the trusted-household-device privacy boundary without advertising handoff as independent authentication.

## Answer

Shared tablets now use a dedicated neutral controller entry with an explicit Ready action. A metadata-only authenticated endpoint returns the device's explicit authorised team list, constructor names, participant identities and assignment revisions. Participant ownership remains separate from device authority. The controller fetches/subscribes to private observations only after Ready and clearly identifies the active constructor.

Locking or choosing Pass tablet destroys the private controller view, clears observations, local drafts, pending plans, acknowledgements and private alerts, and removes the old SignalR subscription. Unsubmitted draft discard is explained. A generation guard rejects late subscription/command responses; private messages must match the active team and current assignment revision, with revision ordering preserved. Refresh and reconnect return shared controllers to neutral before Ready. Private HTTP and SignalR responses have no-store caching; this frontend has no service worker/offline data cache. The neutral screen explains the trusted household device boundary and that handoff does not authenticate each player independently.

## Evidence

- Vertical TDD at the approved API/SignalR seam: initial controller-list request failed with 404. The passing test uses two distinct shared participants on one device, checks its explicit team list without private observations, rejects an unknown device, verifies no-store on private HTTP state, unsubscribes McLaren, subscribes Ferrari and observes only Ferrari's private update after a McLaren lock.
- Vertical browser TDD: initial full shared-tablet flow failed because the shared controller entry was absent. The completed real-browser flow proves zero private HTTP fetches and zero private SignalR subscriptions before Ready; keyboard Enter reveals McLaren; Pass tablet clears the private screen and explains discarded drafts; refresh remains neutral and the discarded Push draft returns to Standard; lock moves to Ferrari's neutral handoff; touch Ready reveals only Ferrari; replaying an earlier real McLaren wire message cannot replace Ferrari; final lock returns to the next neutral handoff.
- Inspected and published `evidence/tablet-neutral-360.png` at 360x800: neutral screen contains only public device status, team name, Ready and household privacy explanation. No prior car choices, estimates, private panels or notifications remain. No horizontal overflow, with comfortably sized Ready/select controls.
- Inspected and published `evidence/tablet-ferrari-360.png` at 360px: the active Ferrari identity and both cars' controls remain contained, existing driver portraits preserved. Full-page image records the complete two-car flow; compact timing headings retain existing density.
- Inspected and published `evidence/tablet-readiness-1920.png` at 1920x1080: all 22 timing rows fit, readiness shows McLaren locked/Ferrari deciding using existing team IDs, and no private choices or condition estimates appear.
- Affected verification `.tools/reports/verification/runs/2026-10-10T11-44-52-173Z-85d47741-f7a8-4859-9e4e-cf6d785aac6c/result.json`: typecheck/build, 34 API tests, 110 simulation tests and 3 shared browser tests passed. Build retains initial bundle-budget and qrcode CommonJS warnings.
- Final focused verification after stale-assignment reset and late-request cleanup guards: `.tools/reports/verification/runs/2026-10-10T11-46-01-693Z-32fb0aa6-f571-4015-9ad3-a201532c47d6/result.json`; typecheck and all 3 shared browser tests passed. No redundant backend rerun after frontend-only guards.
- Bounded command reports record owned PIDs, command identities, exit codes and completion. UI/API 4471/4571 and auth 19099/19400/19450 have no listening services retained. Authentication emulator lease released for ticket 04.
- Actual human household playtest feedback remains pending under ticket 06 by explicit user direction. Automated browser coverage does not substitute for human playtest findings.
