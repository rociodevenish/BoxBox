# Integrated live race screens

Reviewed 9 October 2026 in desktop Chromium, using the real local API and SignalR server. The open-equal, six-lap laboratory race submitted Push/Stay Out/Neutral for Driver 01 and Standard/Pit/Hard/Neutral for Driver 02. Its first boundary completed at leader lap 5.

The live routes reuse the foundation controller and tower. RaceSession owns transport, revision filtering, authorized recovery and retained commands; RacePage maps explicit server contracts to presentation models. There is one controller form and one source of authoritative state. Each car receives separate allowed battle options, queued/ongoing stop restrictions and a labelled own-team estimate. Confirmed revisions hydrate the actual accepted choices rather than defaults.

Public timing uses recorded leader/current and ahead/current observations independently. A controlled three-car regression demonstrates a last car with an unavailable two-lap leader time but a valid same-lap ahead time, preventing subtraction of incompatible leader gaps. Genuine recorded zero contact remains distinguishable from unavailable initial timing. The initial mixed preset follows configured rolling order. Existing numeric engine snapshots, physics and replay versions are unchanged.

## Inspected captures

| Capture | Viewport | Actual image | Review |
| --- | --- | --- | --- |
| [Public phase](03-public-1920x1080.png) | 1920 × 1080 | 1920 × 1080 | All 22 rows fit; units, compound, age and stop state remain distinct. Synthetic fallback avoids false portrait identity. |
| [Confirmed controller](03-controller-360.png) | 360 × 800 | 360 × 1808, full page | Both accepted choices readable, own estimates labelled, disabled primary action muted, no horizontal clipping. |

The current supplied asset inventory contains named real-driver portraits, including George Russell, and no track image suitable for these synthetic presets. Their identities do not match Driver 01–22. Existing gallery portrait provenance is preserved; live screens use a CSS coral/gray diagonal fallback with written identity. No asset originals were changed or new photograph mapping invented.

## Verification

- Backend: 110 simulation and 13 API tests pass. New red/green tests cover mixed initial order, running public timing, genuine zero/missing evidence and independent ahead timing. Existing tests retain capability/group boundaries, explicit payload allowlists, exact engine outcome and atomic validation.
- Real browser: all 8 tests pass, including per-car legal controls and defaults, initial 22-row layout, refresh/rejection, lost acknowledgement, full observation recovery, uncertain retry and actual SignalR interruption/older frames. A final focused one-phase check also verifies all 22 result rows within 1080 px.
- Foundation gallery: all 34 tests pass, including pending stops, rejection, native dialog keyboard/focus return, 44 px controls, reduced motion and portrait failure. These fixtures complement the live server tests without inventing server legality.
- Angular typecheck and production build pass. Final bundle: 271.17 kB raw, estimated 70.56 kB transfer, excluding separately served portrait assets. Muted disabled-action styling was reviewed and followed by a focused browser check and build.

No living-room TV or physical phone playtest is claimed. Full-race continuation and richer phase event presentation remain ticket 04. The server remains an in-memory local laboratory, with no persistent session guarantee across restart.
