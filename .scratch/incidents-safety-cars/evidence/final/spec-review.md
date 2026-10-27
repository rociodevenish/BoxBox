# S09 independent Spec review

Reviewed baseline 285277d715c938b656b36c5adb0a99a8970b86d9 through HEAD 4af4f4359afeb5dd0c65026668a971f1d19099c9; read-only source review, no tests/services executed.

## Missing or partial requirements

- **P2 — Expose the already computed own-team component/risk estimates.** `src/backend/Game.Application/SessionCoordinator.cs:190–193` projects tyres and legal stop options but discards `TeamObservation`'s `TeamConditionEstimate` collection; `Contracts.cs:24–25` has no component/risk fields. The simulation computes Good/Worn/Poor and Low/Elevated/High at `RaceEngine.cs:399–400`, yet live players cannot see them. Exact spec rule: “Show observed outcomes and justified condition/risk estimates without exact hidden wear thresholds, future failure draws or opponent diagnostics.” Add these coarse own-team fields to the private API/SignalR projection and controller presentation, preserving diagnostic privacy.

- **P2 — Provide Safety Car advice for Assisted players.** `SessionCoordinator.Deadlines.cs:28` skips advice generation for SafetyCar; `src/frontend/src/app/race/race-page.ts:111` explicitly removes assistance. AI uses the ordinary dry-race policy with pit-only conversion, so no special-window advice incorporates the observed disruption or reduced stop loss. Exact spec story 21: “As an Assisted player, I want advice using observed incident and Safety Car facts, so that help remains legitimate.” Generate frozen legal pit-only assistance from public interruption/neutralisation facts and own estimates, and expose it in this window.

- **Outstanding human acceptance (disclosed, not an evidence-integrity defect):** `.scratch/incidents-safety-cars/issues/06-disruption-strategy-evidence.md:13` remains unchecked. Exact spec rule: “manual playtests measure comprehensibility, disruption frequency and duration separately from server benchmarks.” The 904 saved runs and machine/browser durations do not satisfy this requirement. Keep ready-for-human and collect household/hosted acceptance before claiming completion; current reporting accurately preserves this limit.

## Scope and correctness

No clear scope creep or independently confirmed engine/recovery correctness defect found in this pass. Monza/Monaco-only, explicitly enabled production policy is documented experimental scope; all other tracks remain off pending route authoring. Frozen-run counts, paired dependence, forced scenarios, eligibility/distance groups and uncertain strategy conclusions are disclosed honestly. Historical screenshots were not used to contradict current UI fixes.
