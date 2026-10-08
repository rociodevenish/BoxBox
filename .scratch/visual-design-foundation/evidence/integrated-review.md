# Integrated foundation review — 8 October 2026

All six visual tickets now provide an executable Angular fixture gallery and reusable presentation components. This review covers the user-approved rendered gallery seam, not engine tests or authoritative sessions. Final integration added the missing initial-race fixture, made rejected input an assertive recovery alert, applied shared focus treatment to selects/focusable event lists and tightened desktop controller spacing. Each missing behavior/geometry was observed red before its correction. Existing detail, choice, summary, lock/recovery, stop/battle and result checks remain green.

| Required state | Reproducible entry/action | Evidence |
| --- | --- | --- |
| Initial race | Results → Show initial race | Race ready, initial order, LAP 0/40, unavailable gaps and no invented event history |
| Decisions / battle | Controller | Two distinct cars, qualitative choices, labelled private estimates; fixture availability controls battle visibility |
| Pending / ongoing stop | Controller → Show pending stop; public display | Known requested Hard remains visible/disabled; public tower separates announced pending and ongoing stop from timing gap |
| Submitted / locked | Controller → Review → Submit → Acknowledge and lock | Both-car confirmation, waiting state, then separate locked state |
| Rejected / recovery | Controller → Submit → Reject | Assertive alert, full draft retained and editable, resubmission supported |
| Loading / resolving | Controller fixture buttons; Results → Show resolving | Explained restrictions; last published public order and waiting for events |
| Quiet / crowded phase | Results fixture buttons | Explicit quiet message; chronological events with public stop consistency, desktop scroll hint and keyboard access to last event |
| Finished | Results → Show race finish | Race finished, Final Classification, 40/40, chequered message and no further decision phases |
| Long name / missing imagery / lap deficit | Main cards, public display and result fixtures | Wrapped live identity, explicit unknown ratings, real 404 fallbacks, seconds separated from lap deficits |

## Inspected visual set

Reuse the stable inspected evidence linked from [cards](driver-gallery-review.md), [public tower](timing-tower-review.md), [two-car controller](two-car-controller-review.md) and [results](phase-results-review.md). Those per-ticket records distinguish viewport sizes from full-page/element captures. The static ticket 01 compositions are historical direction evidence; Angular is the adopted foundation. Do not interpret full gallery height as a TV composition.

Added and opened two genuine 1920×1080 viewport captures: [initial](integrated-initial-1920x1080.png) and [controller](integrated-controller-1920x1080.png). Initial row 22 ends at y 1055.8. Both controller regions, long identity, selected summaries and Review are visible; the focused Review control ends at y 1069.5 and its outline stays inside the viewport. The previous geometry test failed with Review at y 1111.5. Desktop spacing corrected that without shrinking 44-pixel controls. [Capture rectangles](integrated-captures.json) record the actual viewport and target bounds.

The four rejection captures at 360×800, 800×360, 768×1024 and 1024×768 were reinspected and updated only for the error token/alert correction; their full-page PNG dimensions remain 360×2214, 800×1424, 768×1422 and 1024×1332. Phone names and summaries wrap and remain fully visible. Landscape confirmation remains centred with both actions visible. Older generated card/tower/result capture drift was discarded to keep reviewed historical evidence stable; new evidence does not claim every historical screenshot is an exact current gallery-page snapshot.

## Accessibility and assets

Unchanged semantic colours retain the calculated AA token ratios in [the visual system](../../../docs/visual-design-system.md): normal text target 4.5:1, large text 3:1, essential borders/focus 3:1. Coral buttons use charcoal text. Status and compounds have written alternatives; role and ownership are distinct. Production disabled controls retain opaque readable text/selection. Buttons, selects and event lists share a 3-pixel focus outline; a rendered select-focus test failed at the weaker browser baseline before correction and now passes. Native dialogs cycle keyboard focus and return it on Escape/close. Primary controls meet the 44×44-pixel baseline, with choice/confirmation gaps. Reduced-motion checks retain the same visible event information; nothing requires animation.

Representative Max/Lewis assets load in cards/public context/events; real 404 checks preserve live identity, details, event text and final state. The root collection still has 28 driver portraits plus duplicate George and no tracks. Packaged portrait SHA-256 matches original Max `3d3bc36490e082cf53aa025646a8e4523256ba956c23d24ac633dfec1c9e0b4f` and Lewis `af5fe22b17368a1759d7ea5da93065929983b65ad8302c7d4770dc402208e54d`. Data-driven manifests record identity/source/crop/provenance. Originals remain unchanged. [Asset instructions and extension points](../../../docs/visual-design-system.md#adding-supplied-assets) cover future drivers/tracks/other imagery without expanding card/renderer scope.

## Verification and limits

All 34 rendered frontend tests passed in Chromium with Node 24. Production Angular build and strict template/type checking passed. Current complete gallery initial bundle: 195.80 kB raw / 53.81 kB estimated transfer (187.75 kB JavaScript plus 8.05 kB styles). Dependency inventory and source imports contain Angular/RxJS UI only, with no Babylon.js, Three.js, engine project or simulation import. The same gallery bundle serves controller/public entries; it is not a separately split controller deployment. Two original portrait PNGs total about 4.5 MB outside that measurement; responsive delivery and Angular's oversized-image warning remain future device-delivery work.

No real TV was available. Viewing distance/readability is unverified; desktop captures do not prove living-room readability. Physical-device use, screen-reader testing and a comprehensive accessibility/zoom audit remain future playtests. The current battle availability boolean enables all battle options or Neutral; live integration needing separate Attack/Defend legality must extend explicit per-option availability. Session filtering, validation, revision handling and orchestration remain outside this foundation.
