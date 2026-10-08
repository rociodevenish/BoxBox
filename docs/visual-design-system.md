# Broadcast visual system

The Angular fixture gallery and production UI live in `src/frontend`. [Execution and component contracts](../src/frontend/README.md) and the [integrated review](../.scratch/visual-design-foundation/evidence/integrated-review.md) describe the adopted implementation. The earlier `design-baseline` and recovered `.docs` prototype are historical design evidence; their builds and illustrative state models are not production dependencies.

## Direction and semantic tokens

Preserve charcoal surfaces, coral Driver role styling, bold condensed italic display identity and outlined preferences. Portrait backgrounds use a 138-degree repeating gradient: gray first, 32-pixel stop widths, 30-pixel gap, coral/gray layer opacity .8. Stripes stay behind the transparent portrait, fade and live name. They are decoration, never team ownership or selection. Ownership has a separate written team name and data-provided accent. Use stable card placement rather than hover lift when comparing live race information.

Canonical tokens are in `src/frontend/design/tokens.css`; Tailwind aliases in `src/styles.css` map semantic meaning without self-referencing custom properties. Component CSS implements distinctive graphics. Tokens never depend on simulation coefficients.

| Meaning | Rule |
| --- | --- |
| Canvas / surfaces | #101214 canvas, #17191b panels, #24282c raised areas; #69747d borders |
| Text | #f5f6f7 primary, #b5bdc4 supporting; Arial/Helvetica; 16 px body, at least 13 px supporting |
| Display identity | Impact/Arial Narrow fallback, italic; 38 px card display and 48 px TV surname. Keep full names live and allow wrapping; no essential ellipsis. |
| Driver role / ownership | #ff5954 coral role on opaque charcoal; ownership accent plus written team name, independent of role |
| Status | #a4e6b4 success, #f5d978 warning/progress, #ffb7b4 error; always written state and explanation |
| Compound | Written Soft/Medium/Hard plus S/M/H and distinct token colours; unknown is explicit |
| Spacing / shape | 4, 8, 12, 16, 24, 32 px scale; 16–24 px panel padding; 8 px choice gaps; 6 px control / 12 px panel corners |
| Focus / touch | 3 px #a4d8ff outline with 4 px offset on buttons, links, selects and focusable event lists; primary controls at least 44×44 px |
| Selection / restriction | Coral with charcoal text and aria-pressed; repeat both cars in a written confirmation. Native disabled controls retain opaque, readable selected treatment and a written reason. |
| Motion | 120 ms feedback token; no information requires animation. Reduced motion disables animation, transitions and smooth scrolling. |

The carried-forward prototype patterns are transparent portraits, live names/attributes/preferences, five Simple groups, separate role/ownership and native controls. Adaptations include a charcoal surrounding canvas, larger supporting text and controls, explicit unknown data, native modal focus handling and public/private view models. The prototype's inline JIT component, acquisition state, modelContext bridge and unverified cloud imagery were not adopted.

## Anatomy and meaning

Driver cards separate portrait/live identity, personnel role, optional ownership and illustrative display price. Five Simple groups—Pace, Racecraft, Tyres, Consistency and Technical—are the display vocabulary. Detailed attributes are labelled illustrations and may be unknown. Null is unknown, not zero or a low rating. Preferences describe tendencies; outlined tags are not quality stars. There is no numerical conversion into current simulation attributes.

Compact cards support scanning; expanded details support inspection; the TV variant enlarges identity and uses portrait/stat columns. Missing images preserve names, ownership, attributes and access to details through decorative initials and a written unavailable message. Coral/gray stripes remain role graphics around transparent edges. Inspect each crop: the current portrait mapping starts at center 22%, and compact wide crops may trim hair without hiding the face.

The public tower preserves supplied Classification order. Position, team cue/code/full name, explicit time or lap deficit, compound and announced/ongoing stop status remain distinct. Stops do not replace an available timing gap. Phone rows fold stop status under identity and permit longer wrapping; shared display aligns columns and keeps all 22 cars visible. [Dated primary broadcast references](../.scratch/visual-design-foundation/broadcast-references.md) ground the compact hierarchy. Opaque panels, full names, written compounds, taller accessible rows and game-specific phase/result headings are deliberate adaptations. No official graphics raster or logo is copied.

The private controller keeps two named car regions, compound and labelled condition estimate, Pace/Pit/Battle choices, selected summary and both-car confirmation. Qualitative explanations avoid invented coefficients. Pit identifies the requested compound and next lap-end timing. Fixture-supplied pending stop and battle availability explain disabled/hidden choices. Selected, submitted waiting and acknowledged locked are written states. Rejection is an assertive alert retaining the complete draft for correction; loading/resolving explain why editing is unavailable. A caller supplies team identity and stable cars for a decision phase; the UI does not validate race rules.

Public results show initial grid order without invented gap/event history, last published state while resolving, quiet/crowded chronological outcomes, and an unmistakable final Classification/chequered state. Context images and event identities are data-driven. A constrained desktop event list includes a visible scroll cue and keyboard focus; phones scroll the full list. Public inputs have no hidden condition, estimates, random draws or opponent choices. Real filtering and orchestration belong to the browser race milestone.

## Adding supplied assets

1. Inspect the current root `assets` collection and the actual image. Record stable domain identity, original source path, creator/licence if known and intended use. Availability does not authorize inventing gameplay or new card categories.
2. Package only suitable representative copies into `src/frontend/public` and update a manifest containing stable id, URL, provenance and crop metadata. Preserve originals. Current user-supplied creator/redistribution licence is not supplied; these are local design fixtures, not a publication licence claim.
3. Map URLs into view models in an adapter or gallery fixture. Components must not derive identity from filenames. Driver cards use the existing portrait manifest; future track/other assets can have their own manifest and live text alternative. A track image can provide context without introducing a track card or 3D board.
4. Keep live names, race facts and decisions outside imagery. Supply meaningful alt text when imagery identifies a subject; decorative backgrounds/initials are redundant. Null or failed URL must preserve useful information and controls. Avoid remote-image fallback dependencies.
5. Review actual loaded and failed images at relevant sizes; inspect face crop, transparent edges, long labels and containment. Check payload separately from JavaScript and prepare responsive derivatives for real device delivery when appropriate.

Rechecked 8 October 2026: 28 filename-labelled 1024×1536 RGBA portraits plus a duplicate root George image; no supplied track imagery. Only unchanged Max and Lewis PNGs are packaged, about 4.5 MB total, with source paths and ids in `public/portraits/manifest.json`. Their original hashes match the packaged copies. Full roster packaging and optimisation are deferred.

## Accessibility, responsive usage and extensions

Target WCAG 2.2 AA: normal text at least 4.5:1, large text at least 3:1, essential borders/focus at least 3:1. Calculated unchanged token pairs include ink/surface 16.29, muted/raised 7.80, coral/surface 5.72, charcoal/coral 6.09, border/surface 3.69, border/raised 3.11, focus/surface 11.62, success/surface 12.22, warning/surface 12.66 and error/surface 10.65. Never use white normal text on coral. Role labels use solid backing, and disabled production controls do not fade their text. Colour supplements written role, ownership, compound, choice and status.

Rendered tests verify dialogs, focus return, choices, summaries, rejection/lock states, real image failures and reduced motion. Reviewed captures include 360 px and 768 px phone/tablet widths, relevant controller orientations and genuine 1920×1080 compositions. Vertical scrolling is intentional on small screens and for secondary details; component names and controls stay inside their panels. Captures prove desktop rendering only. Real TV viewing distance, physical-device playtesting, screen-reader review and a full accessibility/zoom audit remain outstanding; do not claim those findings from screenshots.

Extend shared tokens, compound/status/event treatments and identity primitives rather than copying screen fixtures. Future Manager, Engineer and Race Package cards should define their own view models and role/attribute semantics; preserve the separation of role, ownership, rating and preference. Do not reuse Driver ratings as implicit engine values. Before live browser adoption, provide authoritative public/private filtering, stable phase revisions, validated submission/recovery and explicit per-option battle availability where Attack and Defend differ. Those integrations and deferred card categories are not implemented by this foundation.
