# Browser playtest, replay and findings — 2026-10-09

The measurements and original flow below describe the ticket 05 explicit-Continue implementation. Later actual feedback requested merging phase submission and continuation. [Final review fixes](../review.md) document automatic windows; [new machine measurements](review-automatic-windows.json) preserve separate timing sources without a ready-wait interval. Historical numbers remain unchanged.

## Actual player findings

The user tested the frozen ticket 04 preview (source `474110d`, Angular 4430/API 4530) and reported three issues: dropdown carets were too far right and unattractive; leader/ahead timing should be named Gap/Interval; and the public board had no route back to the controller. Ticket 05 moves select chevrons inward, labels Gap (leader) and Interval (immediate ahead), and adds Race setup plus a controller return link only in a browser already given that private grant. A fresh public viewer receives no controller link or capability. Actual retest feedback is separate from the agent's verification below.

The user explicitly retired the fixed duration target on 2026-10-09. Timing now informs a pleasant experience without a required duration. Both-car workload, a measured full-race human duration, broad enjoyment and physical phone/TV readability were not measured. The next player check should assess the corrected navigation/names/carets and whether repeated two-car decisions/event explanations feel manageable; automation supplies no substitute for those findings.

## Automated complete-race evidence

Five serial real-browser runs used 50 laps, 22 cars, seed 123, four-segment presets and `dry-opponents-v1`. Each recorded 10 accepted complete 22-car plans (220 decisions) and replayed all physical phase/final outcome hashes exactly through the existing engine. Full replay records stay in ignored `.tools/reports/browser-race/full-distance`; [machine summary](05-playtest.json) publishes configuration, owned decisions, versions, outcome coverage and duration sources.

The four standard cases use Driver 01 Conserve and Driver 02 Push, legal compound changes at phases 2/6 (zero-based), Hard then Soft, and Attack only when the server offers it. All 22 cars finished 50 laps and were eligible. The separate lapped case uses Driver 01 Push/no stop and Driver 02 Conserve/Hard stop every window; both finish 46 laps, with Driver 02 eligible and Driver 01 ineligible, while the leader completes 50. Public rows show a four-lap deficit without converting it into seconds. Actual Attack, Defend and Neutral options were observed; selected Attack was accepted only where available.

| Scenario | Decision windows, ms | Engine resolution, ms | Client presentation, ms | Server total to finish, ms |
|---|---:|---:|---:|---:|
| Open, equal | 2934.7 | 744.0 | 2.1 | 4581.9 |
| Restricted, equal | 2797.2 | 487.9 | 1.2 | 4185.7 |
| Open, mixed | 2919.5 | 614.6 | 1.0 | 4406.3 |
| Restricted, mixed | 2929.8 | 444.5 | 1.1 | 4337.4 |
| Open, repeated-stop lapped check | 3239.5 | 617.6 | 0.8 | 4789.6 |

These are single automated checks, not a balance sweep or human pacing measurements. Decision time includes viewing/network/automation waits. Resolution surrounds only `ResolvePhase`; server total covers session initialization through authoritative finish, excluding final presentation. Ten public-display samples per race use reduced motion and are untrusted client-reported monotonic durations. The private controller reports no invisible presentation. Readiness wait overlaps presentation and is published separately rather than added into a fictitious total. Nonzero skip/replay telemetry is verified in the focused browser test.

No owned queued/ongoing stop was observed at the ordinary five-leader-lap decision boundaries in these cases. This is an explicit live-scenario limitation, not a claim that it was exercised. Existing `PitTests.Queued_stop_survives_an_intervening_phase_boundary`, segmented pit tests and the rendered controller pending-stop fixture cover the engine and disabled-control behavior separately. No preset coefficients were changed to manufacture a pending stop. Six-lap checks cover the final short phase, eligibility, skip/replay, exact reconciliation and restart; recovery tests cover lost acknowledgements, refresh, authorized rejoin, stale updates and honest unavailable actions.

## Readiness and visual review

Reviewed segment evidence supports exploratory strategy/causal-feedback testing, not final balance. [Passing](../../segment-race-model/evidence/track-passing.md), [tyre/pit](../../segment-race-model/evidence/tyre-pit.md), [form/noise](../../segment-race-model/evidence/form-noise.md) and [benchmark limitations](../../segment-race-model/evidence/benchmark.md) remain applicable: measured passing was sparse, tyre/pit outcomes depend on distance and legal eligibility, default latent form is zero, and full designer histories are expensive. Equal presets have equal driver/car attributes; mixed presets include small driver Pace offsets as well as car fit. These five browser cases are not a new statistical balance claim.

Agent inspection reviewed setup at 360×800, controller at a 360×800 viewport with a 360×2179 full-page capture, and initial/final displays as genuine 1920×1080 viewport captures. Inward chevrons remain visible after component backgrounds; computed image/12 px inset confirmed the rendered style. All 22 initial/final rows and final restart action fit the desktop viewport, verified by geometry. Narrow controller compound choices and both independent car regions remain readable. These are desktop browser-width checks, not physical-device or living-room playtests.

The root asset collection includes 23 named real-circuit images. `albert-park.png` was inspected; synthetic laboratory presets are not Albert Park or other real circuits, so no circuit image was mislabelled as their identity. Synthetic drivers retain fallbacks rather than real-driver portraits. Original user assets remain untouched.

## Verification and reproduction

129 backend tests (110 simulation, 19 API/SignalR), 34 gallery tests and 19 real-browser tests passed; the six affected telemetry/full-distance cases passed again after restricting samples to visible public displays. Angular typecheck/build passed. Existing `tools/verify-runner.ps1` passed; an exported 50-lap browser record also verified through the CLI with all 10 phases matching. Missing/invalid designer credentials, missing key configuration, incompatible replay versions and incomplete inputs reject through public endpoints. Player snapshots exclude diagnostic inputs/timings.

Run instructions and measurement/privacy boundaries: [browser-playtest.md](../../../docs/browser-playtest.md). Regenerate cases with `npm run test:browser`; then `node tools/summarize-browser-playtest.mjs`. Stable reviewed captures accompany this document; raw regenerable records remain ignored.
