# 03: Build the public timing tower

**What to build:** A viewer can inspect an accessible broadcast-style Classification with clear driver identity, gaps, tyres and stop status in the gallery.

**Blocked by:** 01 — Establish and validate the visual design system; 02 — Build reusable driver cards and the executable gallery.

**Status:** resolved

- [x] Compose reusable row, identity, gap, compound and status treatments from the established visual rules.
- [x] Inspect newly supplied track and other design assets for appropriate public-display context; use suitable assets within this ticket's scope, keep race facts as live text and retain accessible missing-asset fallbacks. Track-card and 3D-board implementation remain deferred.
- [x] Show exact fixture Classification and distinguish time gaps, units and lap deficits, including dense 22-car fields and long names.
- [x] Identify compounds with text as well as colour and show pending/ongoing stops without inventing unsupported race data.
- [x] Public fixtures exclude private tyre estimates, hidden condition, random draws and opponent decisions.
- [x] Inspect recognisable similarity against the dated broadcast references, recording adaptations for accessible cues, size and contrast.
- [x] Review phone, tablet and 1920×1080 shared-display layouts; record screenshot dimensions, readability findings and limits of desktop review.

## Answer

Built reusable public Classification rows, live identity, gap, compound and stop-status treatments in the Angular gallery. The public-only view model excludes hidden/private inputs; announced/ongoing stops retain distinct live gaps. Rechecked assets and reused the supplied portrait with an accessible failure fallback. [Reviewed evidence](../evidence/timing-tower-review.md) records dated primary-frame comparison, phone/tablet captures and genuine 1920×1080 composition with all 22 rows visible. The phone Medium clipping regression failed before the correction and passes afterward. All 15 frontend tests, production build and strict template/type checking pass. [Execution instructions](../../../src/frontend/README.md) include the public gallery entry. Real TV viewing distance and moving race broadcast parity remain explicitly unverified.
