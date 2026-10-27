# 02: Choose a team-wide Weekend Setup

**What to build:** Choose and save Aero, Mechanical and Tyres settings for both cars with understandable bounded compromises.

**Blocked by:** 01: Prepare a weekend with effective attributes.

**Status:** resolved

Claimed by S07 ticket 02 implementation agent. Approved seams: public immutable preparation/race operations, authenticated session API/SignalR and focused rendered browser flow.

## Acceptance criteria

- [x] Offer validated discrete Speed/Downforce, Responsive/Stable and Warm-Up/Preservation choices; no permanent free gains or per-driver setup.
- [x] Show both drivers' preference fit, with Adaptability moderating mismatch; information attributes do not secretly add pace.
- [x] Save authorised revisions and reject incomplete, stale or invalid commands atomically; previews use legitimate observations and preserve privacy.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented one five-choice Aero/Mechanical/Tyres setup for both cars, with immutable base/source inputs, saved setup/model version and bounded effective redistribution computed from base once. Warm-up now has a separate age-zero tyre time consumer; preservation and temperature management keep their wear consumers. Both-driver compatibility uses an explicit authored mapping of all 35 supplied trait keys moderated by Adaptability, with bounded mismatch loss. Engineer Setup and Setup Feedback grant no pace bonus. Numerical hypotheses, inactive attributes and replay compatibility are recorded in [Weekend Setup v1](../../../docs/weekend-setup.md).

Opt-in approved household preparation persists all eleven source contexts under Lobby.ModuleState without constructing a RaceEngine. Authorised complete setup commands enforce current UID/device/assignment and separate team revision, persist before acknowledgement/broadcast, return identical retry acknowledgements and reject stale/invalid/incomplete/reused identities atomically. Dedicated API/SignalR snapshots enforce Competitive/Open privacy and revision-keyed streams. Existing unprepared Create/Start paths stay intact; preparing cannot bypass the future freeze transition.

Angular now provides circuit demands, supplied driver identities/portraits/preferences/detailed scores, saved two-driver fit and accessible setup choices, refresh/retry and shared-tablet clearing. Packaged all 23 supplied track runtime variants through explicit slug mapping, unchanged originals and documented provenance/fallbacks. Phone/display inspection corrected dense summaries, mid-word choice wrapping and encoding. [Focused checks and reviewed captures](../evidence/ticket02/README.md) cover public race tradeoffs, legacy replay, real Auth/Firestore restart/retry, authenticated SignalR/revocation, keyboard/touch, private handoff and assets. Practice, final confirmation, qualifying freeze and hosted/human checks remain subsequent/pending work; this Answer makes no claim to those gates.
