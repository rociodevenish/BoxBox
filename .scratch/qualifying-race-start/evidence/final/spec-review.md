# Independent S08 Spec review

Compared `cd7c61f248cdc1e32f38beda5bd4b31dc738cc1a...dc2d9d283eb7feb2d44f941f5ecb92f31652db06` (111 files; 3,690 insertions, 70 deletions). Read the parent spec, all five tickets/evidence, qualifying/start contracts, glossary and relevant architecture/randomness/session documentation. No tests were started.

## Implemented wrong

**[P2] Automatic opening silently runs Conserve.** Spec line 54 requires “coherent spatial progress and recorded timing gaps” handed into the existing engine; line 53 requires automatic opening without a player start decision. At `src/backend/Game.Simulation/RaceEngine.cs:145–159`, `CreateStandingStart` constructs an engine and advances directly to lap one. `Car.Pace` at line 27 has no initializer; `PaceInstruction` starts with Conserve (`Game.Domain/Race.cs:4`). The normal Standard assignment at RaceEngine line 188 is bypassed. Consequently opening duration/penalty and wear use Conserve, and the first actual phase changes to Standard. This unintended instruction affects opening Classification, recorded gaps and starting tyre condition. Explicitly initialize the intended opening pace, preserving legacy rolling behavior. Existing `automatic-standing-start-v1` outcomes and frozen evidence must remain replayable or receive an explicit versioned migration; do not silently reinterpret stored v1 inputs/hashes.

## Missing/partial specification

Actual household timing and comprehension/readability, plus hosted/full-weekend acceptance, remain unobserved. Spec line 57 says to “Measure session decision, simulation and presentation times separately, including shared-device handoffs.” Ticket 05 leaves this criterion unchecked and evidence explicitly distinguishes automated/browser verification from player measurements. This is an honest pending acceptance item, not a missing code implementation.

## Scope creep

None found. Preparation freeze, stage barriers, fixed elimination groups, observation-only frozen AI/help, timeout-lock preservation, authority/privacy, replay and same-engine opening belong to S08. No incident/weather/manual-launch expansion was found. Zero intervals alone are not evidence of absent trace: co-located failed-pass queues can legitimately produce zero. Saved movement evidence covers 400 weekends, 282 with position changes.

Commit list: dc2d9d2, 0d1c0d7, ee90ff5, 539bb56, 0d0fde2, 95e60cb, e5e11ce, f516411, 1161403, e4d7162, c6819ba, c64dddb, 4ce827c, 67e6174.

## Independent recheck — 2026-10-17 — target 2df420f5ba4e486395f3fe88b77c9ed968079dcc

Prior P2 resolved. Inspected the remediation diff, README and regressions without starting tests. `RaceEngine.cs:62–65,130` now selects initial pace before initial penalties/durations; new opening v2 uses Standard/Neutral. `AutomaticRaceStart.cs:70–73` explicitly dispatches saved v1 to its historical Conserve behavior. The rolling constructor retains Conserve initialization, and the keyed randomness version remains unchanged. Saved hashes are still checked through the shared replay resolver. `SessionCoordinator.Opening.cs:19` projects the saved version rather than relabeling legacy results as v2.

The meaningful public simulation regression checks pace plus actual time/wear differences and same-engine replay handoff. The persistence-boundary API regression checks restored v1 labeling, first phase and BrowserReplay. The manifest/README record 400 historical and 400 current complete saved-file replays; original evidence remains preserved. The frontend compatibility adjustment retains historical numeric labels only for sessions without opening metadata; S08 named identities remain intact. Expiry settlement extraction preserves missing-lock-only filling and caller guards.

No introduced blocking spec violation or silent version reinterpretation found. Household and hosted acceptance remain honestly pending. Root owns remaining integration checks.
