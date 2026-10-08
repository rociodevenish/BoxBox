# Broadcast design baseline

Established 8 October 2026 for ticket01. The static [composition baseline](../src/frontend/design-baseline/index.html) is runnable before the Angular workspace exists. It establishes visual rules, not component behavior or a playable controller. Ticket02 imports `src/frontend/design/tokens.css` unchanged into its Angular/Tailwind theme and replaces composition fixtures with reusable rendered components. No acquisition, selection state machine, engine conversion or race orchestration is inherited from the documentation prototype.

## Recovered patterns and deliberate adaptations

Audited the root-only `.docs/driver-card-design-spec.sample.md`, recovered `driver-card-mockups/src/main.ts`, `public/styles.css`, README and artifact inventory. Reuse separate transparent portraits, live name/ratings/preferences, the five Simple groups, charcoal/coral identity, bold condensed italic surname, outlined preference tags, separate ownership badge/border, native controls and reduced motion. Preserve the exact full-background 138-degree gray/coral stripes: 32px gradient-stop widths, gray first, 30px gap, opacity .8, behind portrait and fade/name layers. These are role decoration, never a team or selection indicator.

Adapt prototype's light surrounding canvas to charcoal for coherent race graphics. Increase 9–11px labels to 13px minimum supporting text, 16px body; replace 32px close/primary targets with at least 44px. Do not port its inline JIT component, document-query dialogs, unverified build, sample acquisition state or opaque modelContext bridge. Production keyboard dialogs and observable interactions belong to02–06. Avoid hover lift for live race cards; stable positions make comparison easier.

## Semantic system

Canonical CSS custom properties are in `src/frontend/design/tokens.css`; layout decisions can use Tailwind utilities in02. No token depends on engine coefficients.

| Role | Rule |
| --- | --- |
| Canvas / surfaces | #101214 canvas, #17191b cards, #24282c raised summaries; borders #69747d. Use surfaces to group meaning, not decorative nested boxes. |
| Primary text / supporting text | #f5f6f7 / #b5bdc4. Arial/Helvetica body; 16px default, 13px supporting, 22px section heading. Keep long explanatory text in sentence case. |
| Display | Impact/Arial Narrow fallback, italic uppercase surname; 38px compact,48px TV example,36–48px page title. Allow wrapping at hyphens and then anywhere, never ellipsis essential identity. No remote font dependency. |
| Driver role | #ff5954 coral, shared by every Driver. Place role label on opaque charcoal, not over variable imagery. Future personnel roles may receive their own semantic role token. |
| Ownership | Data-provided team accent, initially #73ddd2; explicit owner name plus independent badge/border. Multiple drivers in one team still have the coral Driver role. |
| Spacing | 4,8,12,16,24,32px scale;16–24px panel padding;8px choice gaps. |
| Corners / elevation | 6px controls,12px panels;1px borders,3px ownership/summary accents;0 12px32px black25% panel shadow. Avoid elevation as the only selected cue. |
| Motion | 120ms feedback only; no required animation. Reduced-motion disables transitions/animations and smooth scrolling. |
| Focus | 3px #a4d8ff solid outline,4px offset; keep it visible outside overflow wrappers. |
| Selected | Coral fill with charcoal text, aria-pressed for buttons; explicit two-car summary repeats choices in words. |
| Submitted / locked | Written Submitted/waiting message with check; written Locked plus lock symbol and disabled controls. Lock is distinct from selection. |
| Disabled | Opaque #1c2023, supporting text, native disabled; do not fade whole text below contrast baseline. |
| Loading / resolving | Written progress state in warning token; no spinner-only meaning. |
| Error | Written cause in #ffb7b4, recovery action placed next to affected input in production; role alert when rejection occurs, retain choices. |

## Anatomy and information contract

Card: ownership badge → transparent portrait/role stripes → live first name and surname → Driver role → five Simple rows (Pace, Racecraft, Tyres, Consistency, Technical) → quieter outlined preference tags → optional detail disclosure. Numbers1–5 are illustrative display ratings; missing values say Unknown, not zero or an empty pip. Expanded detailed attributes remain explicitly unsupported/unknown unless real UI data supplies them. Preferences such as high speed are tags, never ratings or advantages. Fixture content is labelled at gallery entry, rather than forcing sample disclaimers inside future live cards. Costs and full roster are omitted from race compositions rather than invented.

Tower: race/session header → labelled Classification and gap basis → position, team cue/driver code, time or lap deficit/status, compound. Align tabular numbers; say LEADER, PIT and +1 LAP explicitly. S/Soft, M/Medium, H/Hard have text as well as colour; a full compound name must be available to assistive technology in production. Public examples have no condition estimate, randomness, private choices or hidden numerical state. See [dated broadcast inspection](../.scratch/visual-design-foundation/broadcast-references.md). The compact aligned column rhythm is recognisable; taller rows, opaque panels, written compound names and game-specific header deliberately differ from the practice broadcast. No F1 logo/graphic raster assets are copied.

Controller: persistent Car1/Car2 identity, tyre compound and labelled private estimate → Pace/Pit/Battle Instructions → both-car summary → one team commitment action. Estimates retain a range and the word estimate; never exact hidden condition. Pace trade-offs use qualitative text, not invented coefficients. Stay Out/Pit is explicit, compound follows Pit, and next lap-end timing is stated. Nearby battle contextual controls do not appear as globally useful options. No nearby battle shows Neutral and an explanatory line.

## Assets

Reinspected root assets on 2026-10-08:28 filename-labelled1024×1536 RGBA portraits, plus duplicate root George; no track/other assets. Packaged only unmodified Max Verstappen and Lewis Hamilton copies (about4.5MB total), with explicit source paths, ids and object-position in `public/portraits/manifest.json`. Originals remain untouched. These are user-supplied assets; creator/licence was not supplied. No claim they reproduce recovered prototype cloud artwork. Full roster delivery and licensing for publication remain outside this local foundation.

Use object-fit cover, center22% initially, inspect each asset/variant. Keep face above the name fade and coral/gray behind transparent edges. Compact crops emphasise face/shoulders; some hair is cropped on wide desktop examples. Preserve live name independently. Missing/failed images use neutral charcoal initials at the portrait layer and preserve name, role and ownership; initials are decorative redundancy, not the accessible name. No remote image fallback. Add assets through the manifest/view model; adding tracks does not introduce track cards into this milestone.

## Accessibility baseline and inspection

WCAG2.2 AA baseline: all normal text ≥4.5:1, large text ≥3:1, essential borders/focus ≥3:1. Calculated sRGB pairs: ink/surface16.29, muted/raised7.80, coral/surface5.72, charcoal-on-coral6.09, border/surface3.69, border/raised3.11, focus/surface11.62, team/surface10.91, success/surface12.22, warning/surface12.66, error/surface10.65. Coral role text is on solid charcoal. Never use white normal text on coral buttons. Decorative gray stripes need not convey meaning. Status text, compound letters/names, ownership name and written summaries prevent colour-only interpretation. Text can wrap; production components must preserve zoom/reflow.

Reviewed actual rendered screenshots at360×800,768×1024 and1920×1080 in Chromium151, all with reduced motion. [Evidence directory](../.scratch/visual-design-foundation/evidence/) holds cards/tower/controller crops plus full gallery screenshots. All three widths had document width equal to viewport, minimum button dimension44px, loaded portraits, solid keyboard focus on the first Conserve button and zero JS page errors; machine observations are `render-checks.json`. This is rendering verification, not a behavioral test suite for noninteractive compositions.

360: stacked cards/cars; long surname wraps without clipping, both car summaries and compound choices remain readable. Vertical scrolling is deliberate, including22 tower rows.768: paired compact/expanded cards and paired car controls, with TV long-name example below; texts wrap within panels.1920: three identity examples, narrow aligned22-car tower, side-by-side controller. Tower crop is1062px high; a full gallery exceeds1080px and scrolls. Production shared display must compose its race surface separately rather than reuse the entire documentation gallery at once. Body16px and tower16px are desktop baseline, not proof of living-room readability. A real TV viewing-distance check remains outstanding in06/playtesting.

The initial fallback initials intersected the stripe layer; the review moved initials above stripes and toward the upper portrait area. No content is lost if fonts vary. Keyboard/focus/reduced-motion were inspected as presentation; production dialog focus return, image-error fallback and choice behavior receive agreed rendered Angular tests in subsequent tickets.

## Run and extend

From repository root, Node24: `node tools/serve-design-baseline.mjs`, open `http://127.0.0.1:4300/design-baseline/index.html`. Static native Node server has no install/build step. Capture with `node tools/capture-design-baseline.mjs` after installing Playwright Chromium; `PLAYWRIGHT_MODULE` optionally points to a bundled Playwright index.mjs, and `PLAYWRIGHT_BROWSERS_PATH` to its browser cache. Captures are explicitly reviewed, not automatic visual assertions.

02 consumes canonical tokens/assets and replaces this static composition module with Angular models/primitives.03 consumes tower rules,04 controller hierarchy,05 result/status language,06 integrated edge cases and accessibility. Manager/engineer/package/track cards stay deferred; extend role tokens and data-driven assets later without reusing team colours as role semantics.

### Capture dimensions and shared display follow-up

Screenshot suffixes360/768/1920 identify browser viewport width, not cropped image dimensions. `capture-dimensions.json` records each actual PNG size. Full-page1920 lab capture is1920×2084; the tower crop is410×1061. A separate genuine1920×1080 viewport capture, `shared-display-1920x1080.png`, uses `?view=display`: the phase header, all 22 rows and a real portrait TV driver treatment fit together. The last row occupies y 1005–1044, inside the 1080px viewport. This was opened and visually inspected; the contain crop preserves the head and suit, and the enlarged name separates clearly from the five display groups. Team/ownership fixtures are fictional Apex, not a claim about current real constructors. TV distance readability remains unverified.

Capture this public composition with `node tools/capture-shared-display.mjs` using the same Playwright environment variables. The static display mode does not expose the private controller panel.

