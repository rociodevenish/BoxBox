# Lobby and team-claim evidence — 2026-10-09

Implemented ticket 01 in the existing SessionCoordinator: authenticated permanent host/anonymous guest identities, public join link/code/QR, lobby-local players and devices, atomic eleven-team claims, host approval/correction, named shared participants, privacy arrangements and a frozen human/scripted start. The existing RaceEngine initializes once at host start. Legacy solo capabilities cannot control a household race; active multi-team locks remain ticket 02.

## Public-boundary verification

Six API/SignalR tests use real emulator-issued tokens, not fabricated principals. They verify permanent-host versus guest permission, malformed/expired/wrong-project rejection, concurrent claims with one winner, wrong-device/session denial, host-only corrections, eleven approved claims with a rejected twelfth claim, shared participants, immutable post-start assignments and authorized group updates. The expired-token case deliberately expires a genuine unsigned emulator token; it does not assert production signature handling from an emulator test.

A regression leaves team-01 scripted and controls team-04. The old solo-only team-01 exclusion now belongs to solo callers, so every unclaimed household team receives legitimate frozen scripted choices. Existing solo outcomes and script/model versions remain unchanged. Public snapshots retain synthetic driver identities and initial rolling order; constructor identity does not replace personnel or ratings.

Two real browser contexts create an email/password host and an anonymous guest, claim separate constructors, receive live updates, approve the guest and start a 2-human/9-scripted grid. A second phone case creates a shared participant, moves its constructor assignment, reloads authenticated membership and checks QR/panel containment. The QR and join link contain only the public locator. No private estimates, seed, tuning, auth UID, email or controller capability enters the lobby roster.

Final checks passed: **138 backend tests** (110 simulation + 28 API), **34 gallery tests**, **19 solo race browser tests**, **2 real pit-boundary browser tests** and **2 household lobby browser tests**. Typecheck/build passed. Initial bundle is 299.89 kB raw / 76.75 kB estimated transfer; Auth and QR are lazy chunks. Frontend npm audit reports zero vulnerabilities after the pinned grpc-js 1.13.6 override. The lazy qrcode CommonJS warning is documented. Historical gallery captures were restored.

## Inspected phone captures

| Capture | Viewport | Actual image | Observation |
|---|---|---|---|
| [Host invitation](lobby-host-360.png) | 360 × 800 | 360 × 800 | Join code/link and QR fit; privacy mode and credential boundary are readable. |
| [Shared assignment](shared-assignment-360.png) | 360 × 800 | 328 × 584 element crop | Full participant name appears above the native select, Shared tablet/Approved are explicit, and 44-pixel controls remain contained. |

The selected native dropdown text can truncate a long name; the full owner name is shown separately. Existing focus tokens and charcoal/coral contrast are reused, with visible input focus. The lobby is a scrolling roster, not a claim that all host forms fit on a TV. No physical phone/QR camera scan or actual household usability finding is claimed.

## Runtime and limits

Verified local auth-only Firebase CLI 15.33.0 on bundled Node 24 with FirebaseAdmin 3.7.0 and browser Firebase 13.0.0; no Java, cloud login or cloud project was used. Auth/hub/log ports are 19099/19400/19450. Existing previews on 4430/4530, 4440/4540 and 4450/4550 were untouched. See [local procedure](../../../docs/shared-sessions.md).

Constructor IDs are pinned from the provisional driver reference-team metadata, independently of fixture performance. Current driver/circuit assets were inspected and preserved; no real identity image is attached to synthetic laboratory inputs. All player and race state remains in process; server restart loses it. Household decision barriers, tablet handoff, open controls, revocation and real multi-device playtest findings remain the following tickets.
