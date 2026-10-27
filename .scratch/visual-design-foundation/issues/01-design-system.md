# 01: Establish and validate the visual design system

**What to build:** A designer can review one coherent Broadcast visual system applied to representative driver cards, a public timing tower and a two-car controller before production components are built.

**Blocked by:** None (can start immediately).

**Status:** resolved

- [x] Audit the recovered prototype and record patterns to reuse, adaptations and asset provenance; its unverified build is not assumed to be production-ready.
- [x] Inspect the repository's shared design-asset collection at the start of work, including supplied driver imagery and any newly added track/other assets. Establish consistent portrait, crop, background and fallback rules using suitable real assets and document provenance and identity mapping.
- [x] Establish semantic typography, colour, spacing, surface, radius, border, elevation and motion rules, including accessible focus, selected, submitted, locked, disabled, loading and error treatments.
- [x] Preserve the selected charcoal/coral Broadcast direction and documented coral/gray portrait stripes; distinguish personnel role styling from team ownership.
- [x] Define card anatomy, the five Simple driver attribute groups, preference tags, unknown values, compound badges, labelled estimates and race information hierarchy without inventing simulation conversions.
- [x] Record dated recent F1 broadcast references and demonstrate recognisable timing-tower hierarchy adapted for readability.
- [x] Apply the rules to representative card, timing-tower and two-car controller compositions at 360px, 768px and 1920×1080; inspect the layouts and record findings and deliberate departures.
- [x] Establish WCAG 2.2 AA contrast, text alternatives to colour, visible focus, reduced motion and primary touch targets of at least 44×44 CSS pixels as the implementation baseline.
- [x] Document the agreed visual baseline and extension points; later card categories remain deferred.

## Answer

Established the [visual design system](../../../docs/visual-design-system.md), canonical frontend semantic tokens, supplied-portrait manifest and independently runnable static compositions. Recorded the prototype audit, asset identity/provenance, dated primary broadcast inspection, interaction-state/accessibility rules and extension points. Reviewed cards, 22-car tower and two-car controller at all three target widths, plus a genuine 1920×1080 public display with all22 rows in view. [Capture evidence](../evidence/) records actual PNG dimensions separately from viewport sizes, focus/44px/reflow checks and screenshots. Contrast pairs meet the documented baseline. Production Angular behavior belongs to subsequent tickets; no unit tests were invented for design documentation. Static module syntax and actual Chromium rendering passed; no build dependency is needed for this ticket. Real TV distance and moving official race video remain explicitly unverified.
