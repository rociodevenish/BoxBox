# S13 — Initial car construction

Status: ready-for-agent
Blocked by: personnel acquisition and its starting-budget ledger; campaign progression; effective weekend attributes.

## Problem Statement

Players can acquire people but still inherit preset cars. They cannot choose a performance philosophy or understand how preseason car spending competes with later development flexibility.

## Solution

Provide the same small catalogue of bounded construction compromises to every team. Players build a viable Base Car, compare circuit fit and both drivers' compatibility, confirm the cost and carry the locked car into the campaign. Prove different specialisations without claiming final coefficients or universally best builds.

## User Stories

1. As a player, I want equal access to car choices, so that cars are not competitively drafted.
2. As a player, I want a viable affordable baseline, so that roster spending cannot prevent participation.
3. As a player, I want low-drag and downforce choices, so that I can favour different circuits.
4. As a player, I want mechanical-grip choices, so that handling can distinguish builds.
5. As a player, I want tyre-focused choices, so that stint performance is a philosophy.
6. As a player, I want reliability choices, so that risk is a clear compromise.
7. As a player, I want deployment-related choices, so that Speed is more than one label.
8. As a player, I want costs and remaining budget visible, so that future flexibility is understandable.
9. As a player, I want strengths and compromises summarised, so that construction is not a spreadsheet exercise.
10. As a player, I want both drivers' fit shown, so that I can account for preferences.
11. As a player, I want contrasting circuit previews, so that local strengths are not mistaken for universal gains.
12. As a player, I want diminishing returns explained, so that extreme stacking is not automatically best.
13. As a player, I want to revise a draft before confirmation, so that comparison does not spend credits.
14. As a player, I want one server-confirmed final build, so that purchases cannot be duplicated.
15. As a player, I want Base Car distinct from Weekend Setup, so that permanent choices are clear.
16. As a player, I want restart to retain my build, so that preseason is durable.
17. As an Assisted player, I want honest construction guidance, so that advice reflects my roster and budget.
18. As a host, I want AI to build with the same catalogue, so that it has no exclusive upgrades.
19. As a player, I want all teams ready before the first weekend, so that construction cannot change during qualifying.
20. As a designer, I want campaign comparisons of build philosophies, so that balance includes circuit variety.

## Implementation Decisions

- Add a construction step after personnel acquisition and before first-event preparation. Use an immutable versioned catalogue shared by all teams and public domain operations validating drafts and computing candidate effective Base Car inputs.
- Supply a small set of explicit compromise categories covering aero, mechanical grip, tyres, reliability and deployment. Offer bounded selections and an affordable baseline; do not implement hundreds of independent sliders or scarcity between teams.
- Use the locked detailed attribute vocabulary and effective-performance composition. Choices favour specialisation with modest weaknesses and diminishing returns, recorded as tunable mappings; do not convert illustrative card stars into numerical physics.
- Validate combinations and bounds before mutation. Catalogue design must ensure every roster-complete team can buy a viable baseline within the acquisition reserve; no hidden credit top-ups or automatic replacement of an unaffordable selected build.
- Draft editing and previews are free and deterministic. Atomic final confirmation debits the existing credit ledger and freezes a Base Car identity/version. Accepted confirmation cannot be replaced by a retry or stale draft.
- Keep Base Car immutable as the initial construction record. Later permanent development adds separate modifiers; Race Packages and Weekend Setup remain separate layers. Do not double-apply draft adjustments at race initialization.
- Preview grouped attributes, implemented driver fit, costs and qualitative circuit trade-offs using legitimate team inputs and validated representative tracks. Optional diagnostic paired comparisons are designer-only; player previews cannot reveal future incidents/weather or guarantee lap-time gains.
- AI and assistance use the same catalogue, budget and roster observations, with versioned deterministic choices and no extra spending capacity. Guided presentation may reduce defensible alternatives but preserve executable affordable choices.
- Persist draft/final revision, catalogue version, ledger debit and preset Pit Crew values. Starting the first weekend requires every team to be ready or a disclosed recorded automatic build policy; no purchase after qualifying begins.
- Extend Angular/Tailwind with construction comparisons and car summaries, showing Simple groups with optional supported detail from the same model. Recheck supplied car/track assets, use data-driven references and maintain readable placeholders and provenance.
- Publish short and full-calendar fixture experiments once track content exists, comparing low-drag, cornering, tyre and reliability philosophies with fixed rosters and paired seeds. Record circuit gains, weaknesses, opportunity costs and unresolved dominance rather than claiming all builds are balanced by definition.

## Testing Decisions

- User-approved main boundary: public API/SignalR and public construction/effective-input domain operations, plus focused browser interactions and existing persistence emulators.
- Verify identical catalogue access, bounds, affordable baseline, draft non-spending, atomic final debit and frozen build through observable outcomes.
- Reuse replay, effective attributes, personnel ledger/idempotency and campaign readiness tests. Confirm permanent/setup composition applies each modifier once and base inputs remain unchanged.
- Restart around confirmation and race initialization; stale/duplicate commands cannot create another charge or change the selected build.
- Browser tests cover comparing, editing, affordability rejection, confirmation and first-weekend handoff. Record real readability and choice-comprehension findings; compare specialisation through public race outcomes rather than private coefficient assertions.

## Out of Scope

- Competitive car drafts, bespoke component engineering, continuous vehicle physics and final car balance.
- Development purchases, Race Package creation, resale, full asset coverage and sponsored income.
- A second simulation for Full Mode or per-driver initial cars with unrelated team identities.

## Data and asset references

Review these repository inputs before implementing this spec:
- [f1_strategy_preseason_car_construction_2026.json](../../.docs/data/f1_strategy_preseason_car_construction_2026.json) - shared preseason construction menu, baseline, option modifiers, costs and example builds.
- [f1_strategy_car_attribute_spec_2026.json](../../.docs/data/f1_strategy_car_attribute_spec_2026.json) - car attribute vocabulary, scales, headline derivation, setup and development/package domains.
- [f1_strategy_driver_cards_2026.json](../../.docs/data/f1_strategy_driver_cards_2026.json) - driver identities, headline/detailed ratings and compatibility preferences.
- [f1_strategy_track_cards_2026.json](../../.docs/data/f1_strategy_track_cards_2026.json) - circuit identities, event-to-track calendar mapping, demands and race characteristics.

Use [driver portraits](../../assets/driver%20images/) and [track imagery](../../assets/track%20images/) wherever this feature presents those identities. Inspect the current [assets collection](../../assets/) at implementation time, including later additions; map files through stable content identities, record provenance, preserve originals and package suitable runtime variants. Keep readable text and missing-image fallbacks. Simulation-only work uses imagery only for associated reports or galleries.

The JSON files are provisional design inputs. Preserve schema/data versions, validate references, units and bounds, and document mappings to supported runtime attributes; do not silently convert headline stars into simulation values. Data availability does not expand the mechanics in this spec. Keep active campaign/race inputs stable when files or assets change.

Start from the supplied four-category menu and baseline/modifiers. Reconcile provisional budgetUnits with the integer-credit ledger, validate affordable completion, and apply construction, permanent development, one package and setup exactly once.

## Further Notes

Construction offers equal access while personnel remain unique. Confirmation establishes the Base Car, not a Weekend Setup or permanent-development transaction. The user approved the public API/domain and browser testing boundaries.
