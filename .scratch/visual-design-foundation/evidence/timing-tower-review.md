# Ticket 03 public timing tower review

Reviewed 8 October 2026 in Chromium 151.0.7922.34. Angular 22.2.1 and the approved semantic tokens underpin the independently runnable gallery. `/?view=public` is a genuine Angular public composition; the private controller/card gallery branch is absent from its DOM.

## Information and reusable parts

`PublicClassification` carries phase/lap metadata and already-public rows in supplied Classification order. Each row contains position, driver identity/team cue, an explicit gap union, current compound and public stop status. There are no condition, private estimate, random draw, seed or opponent instruction fields. Authoritative server filtering remains outside this gallery milestone.

Reusable Angular parts are TimingTower, TimingRow, DriverIdentity, RaceGap, CompoundBadge and StopStatus. They preserve live text and do not calculate race outcomes. Gap kinds distinguish Leader, time seconds (three decimal places), integral lap deficits and Unavailable. One/two laps have explicit singular/plural labels. Stops never replace gaps: Carlos remains +8.244 s with Stop pending; Fernando remains +9.618 s with In pit lane. Pending explicitly means an already-announced public stop, rather than a hidden team decision. Soft/Medium/Hard retain letter, full written name and accessible compound label; unknown compound is written Unknown.

Full driver names remain visible next to terse broadcast codes. Team cues have accessible team-name alternatives; fixture teams Apex/Ember/Orion/Coral are synthetic and do not claim current real-team ownership. The long Alexandra Montgomery-Sutherland fixture is synthetic. Classification contains exactly 22 rows; it is not silently reordered by the UI.

## Asset and broadcast comparison

Reinspected the shared root asset collection: 28 supplied portraits plus duplicate root George, with no newly supplied track/other assets. Reused the existing packaged Max portrait and manifest without modifying originals. The public leader context keeps live identity when its image fails, with neutral initials and Portrait unavailable. No track card or 3D board was introduced.

Reopened and inspected the actual [Globo 2026 Japanese practice frame](https://s04.video.glbimg.com/x720/14470151.jpg), dated 27 March 2026 by its [broadcaster article](https://ge.globo.com/motor/formula-1/noticia/2026/03/27/gp-do-japao-piastri-lidera-2o-treino-livre-bortoleto-e-16o.ghtml). The source shows a narrow charcoal tower with 22 rows, position/team/code/timing/compound columns and a red first-position tile. The implementation preserves that column rhythm, bold code and aligned numbers, team stripe and restrained leader accent. Deliberate adaptations: opaque charcoal for contrast, full names, written compounds/status, a separate stop column, taller rows and an explicit race gap basis. Leader tile uses charcoal text on coral (6.09:1) rather than low-contrast white-on-red imitation. Project typography/symbols replace F1 logos/marks. The practice frame cannot establish actual race-gap precision, lap-deficit convention or moving transitions; those are the game's explicit presentation contract. See the original [dated reference index](../broadcast-references.md) for official F1 sources and its inspection limitations.

## Captures actually reviewed

All four final screenshots were opened and inspected after the phone badge correction. Capture manifests distinguish browser viewport from cropped table size:

| Browser viewport | Table crop | Additional viewport capture |
| --- | --- | --- |
| 360×800 | `angular-tower-360.png`, 326×1323 | Vertical scrolling accepted |
| 768×1024 | `angular-tower-768.png`, 718×880 | Portrait context follows below |
| 1920×1080 | `angular-tower-1920.png`, 758×880 | `angular-public-1920x1080.png`, exactly 1920×1080 |

Phone names wrap rather than ellipsise. Announced/ongoing status moves below identity; redundant On track lines are omitted on phones. Time/lap values and full compound text remain separate and readable. Initial Medium badge clipping was visible despite no document-wide overflow; a rendered geometry regression reproduced it. The compound column now reserves 94 px, preserving Medium and Unavailable with additional name wrapping accepted. Tablet shows the full name, aligned seconds and a separate stop-status column. The display composition includes the phase header, 22-car field and contained leader portrait. The last row occupies y=990.59–1027.09, inside the 1080 px viewport, with no essential row below the fold. Portrait masking softens the supplied cutout's lower edge while preserving its original image file.

Colour pairs retain the contrast-checked baseline. Compound and stop meanings have written alternatives; semantic table headings/row headers provide navigable Classification structure. Rows are read-only and introduce no unnecessary focus targets; the public gallery link retains visible focus and a 44 px target. Reduced-motion captures use the existing no-required-motion treatment. These desktop captures do not prove living-room readability or real-TV viewing-distance performance.

## Completion checks

All 15 rendered frontend tests pass, including previous card interactions and new gap/status, dense display, public portrait-failure and phone clipping regressions. New feature regressions were observed red before implementation. Production build and strict Angular template/type checking pass: initial bundle 165.04 KB raw / 47.44 KB estimated transfer, excluding preserved portrait PNGs. No simulation/3D dependency or orchestration was added. No C# rerun was needed for this UI-only ticket. Browser cache and the pinned frontend lockfile are reused.
