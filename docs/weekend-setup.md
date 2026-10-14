# Weekend Setup v1

`WeekendPreparation.Prepare(preset, laps, setup)` accepts one complete immutable `WeekendSetup` for both cars. Each integer axis offers -2, -1, 0, 1, 2: Aero Speed to Downforce; Mechanical Responsive to Stable; Tyres Warm-Up to Preservation. Zero is Balanced. Source base ratings, drivers, circuit and neutral package remain unchanged in the effective snapshot. Revisions recompute from that same preserved base once, never from previous effective values. `CreateRace` consumes saved effective inputs without reopening content or applying setup again. Future practice and qualifying must consume this same preparation model.

Numerical mappings are authored provisional hypotheses, version `weekend-setup-v1`, with 1–100 detailed capability bounds. None is calibrated to real-world performance or derived from Simple stars. Neutral legacy preparation still uses `neutral-setup-v1`, retaining its original inputs and coefficients. Saved setup snapshots explicitly carry choices and setup model version; unsupported versions reject with preserved-input explanation.

| Axis increase of one step | Detailed score changes | Compromise |
| --- | --- | --- |
| Aero towards Downforce | Top Speed -4, Acceleration / Energy Deployment -2, High-Speed Aero +4, Low-Speed Aero +4 | Straight performance for cornering |
| Mechanical towards Stable | Mechanical Grip -3, Traction +3, Tyre Preservation +2 | Rotation/grip for traction and stint preservation |
| Tyres towards Preservation | Tyre Preservation +4; first-lap warm-up multiplier +0.12 | Early response for stint life |

All unlisted car scores, including temperature management and Reliability, remain unchanged. Warm-up has a separate engine consumer: the existing age-zero tyre time penalty is multiplied by `clamp(1 + Tyres * .12 - (driverTyreWarmup - 82) * .004, .65, 1.35)`. This applies after a tyre change too. It is separate from the existing wear denominator, where car Tyre Temperature Management still affects wear only. Default aggregate `Driver.TyreWarmupMultiplier` is 1, preserving old replay behavior. Existing segment baseline-time fractions distribute this whole-lap penalty without multiplying it by segment count. Detailed driver Tyre Warm-up is active only on the explicit setup path. Remaining future/inactive attributes retain the meanings documented in effective-weekend-presets.

## Compatibility authoring

The supplied 35 trait keys have no numerical axis mapping. This version deliberately approximates their handling dimensions as one Mechanical target; it does not claim that braking/traction/high-speed stability are identical. Aero and Tyres fit use their capability compromises rather than inventing driver preferences. The average of all supplied Mechanical trait targets is the driver's desired setting. Empty preferences have no mismatch; unknown trait keys reject explicitly.

| Mechanical target | Authored supplied trait keys |
| --- | --- |
| -2 Responsive | aggressive-front, decisive-front-response, early-rotation, entry-rotation, immediate-turn-in, pointy-front, responsive-front, rotation, sharp-front, sharp-initial-turn |
| -1 Responsive bias | aggressive-entry-braking, loose-rear-tolerant, natural-front-feel, positive-front, progressive-front, strong-front, strong-front-feedback |
| 0 Balanced | adaptable-balance, neutral-adaptable, neutral-balance, neutral-predictable, predictable-balance |
| +1 Stable bias | confidence-predictability, moderate-rear-stability, predictable-rear, strong-braking-platform, understeer-tolerant |
| +2 Stable | braking-stability, clean-exit-traction, entry-stability, high-speed-rear-stability, rear-stability, stable-car, stable-entry, stable-rear |

Mismatch is `abs(Mechanical - meanTarget) / 4 * (1 - Adaptability / 125)`, in 0–1. Compatibility shown to the player is `100 * (1 - mismatch)`. A bounded race-pace loss of `mismatch * .15` whole-lap attribute units gives handling fit a small real consequence; it never grants a preference bonus. Higher Adaptability reduces loss. Both drivers receive their own fit from the shared setting. Confidence words are Comfortable at >=90, Manageable compromise at >=80, Preference mismatch otherwise; these are compatibility labels, not practice confidence. Source preference research confidence is not a numerical physics input.

Engineer Setup and driver Setup Feedback grant no pace or wear bonus. Engineer neutral preset remains authored Setup 82; [Practice Assessment](practice-assessment.md) uses these scores for information quality and confidence. Identity cards use detailed design scores and retain names/preferences with images and text fallback; Simple headline groups do not become coefficients.

## Authoritative preparation

The explicit host `POST /api/lobbies/{id}/preparation` is available only from a lobby with approved assignments. It preserves all eleven two-car presets under `Lobby.ModuleState["weekend-preparation-v1"]` and moves lifecycle to `preparing`. Constructor-to-driver mappings are explicit supplied IDs, including Alexander Albon and Andrea Kimi Antonelli. Driver metadata is a selected authored preset context, not inferred ownership. There is no RaceEngine, practice session, final confirmation or qualifying result. Legacy Create/StartLobby paths stay available for unprepared lobbies; StartLobby rejects a preparing lobby to prevent bypassing the future freeze transition.

Authenticated GET and complete setup POST use current UID, joined device, approved assignment and additional-controller grants. Commands carry GUID identity, expected team preparation revision and assignment revision. The same canonical command returns its saved acknowledgement; reused identity with changed inputs conflicts. Authority is checked before retries. Another team's save increments public lobby revision without staling the owned team's revision. Missing/noninteger/out-of-range choices, stale revisions and revoked authority reject without changing saved state. The coordinator serial mutation and existing conditional persistence boundary save context, choices, effective inputs and acknowledgement together before notification. A failed durable write restores the previous saved module through existing rollback machinery.

Public snapshots/events contain circuit/context lifecycle and team IDs only. Authorised own-team projections contain source preset, choices, both fits and readable summary, excluding effective engine coefficients and future diagnostics. Read-only Open preparation is allowed solely for approved Open assignments outside Competitive. Private SignalR groups are keyed by assignment revision and removed on revocation. Public preparation uses a dedicated group/event. Snapshot GET recovers notification failure. Shared-tablet handoff, account change, disconnect and assignment change clear private sources, drafts, pending commands and subscription generation; saved server choices recover after authenticated reload.

Setup tuning/source versions are saved independently of filesystem changes. An existing aggregate replay has no setup and keeps its old numerical behavior. Persisted provisional state now supports ticket 03 assessment state and final candidate revisions; ticket 04 will author atomic all-team freezing before qualifying, without retroactively constructing an engine here.
