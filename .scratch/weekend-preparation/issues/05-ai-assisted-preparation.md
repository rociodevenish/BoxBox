# 05: Support AI, Assisted and Guided preparation

**What to build:** Prepare AI teams and give human players legitimate, understandable setup guidance.

**Blocked by:** 04: Confirm and recover preparation; race-continuity-ai-assistance 02, 05.

**Status:** resolved

## Acceptance criteria

- [x] Record provisional/final automatic choices and policy versions using the same legal observations and choice set as humans.
- [x] Advice explains uncertainty, both-driver fit and compromises, preserving per-player help and household privacy modes.
- [x] Holding legitimate observations fixed while changing hidden diagnostics leaves policy output unchanged; Guided controls remain executable and preserve drafts.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Added public deterministic `PreparationDecisionPolicy.Evaluate` with an explicit legitimate-observation input: circuit demands, own base-car scores, both driver preferences/Adaptability and saved observed practice feedback. It cannot access exact compatibility, effective pace, seed, opponents or future outcomes. The authored bounded policy, source/observation versions and confidence-based final revision are documented in [Preparation guidance v1](../../../docs/preparation-guidance.md).

BeginPreparation prepares only unclaimed teams: record provisional observation/choice, assess once, record final observation/choice from that own feedback and mark final-ready. One durable mutation stores original assessment inputs/output, source/policy/content versions and final effective inputs. Restart and duplicate BeginPreparation retain those records without rerolls. Human teams stay provisional and preserve submitted choices. The existing full-field freeze includes automatic source and recorded choices. Actual qualifying integration remains S08; no qualifying UI or race mechanics are added.

Private advice uses the currently assigned player's Standard/Assisted/Guided settings and existing authorised SignalR groups and intentional Open rules. Standard omits advice; Assisted/Guided explain demand, both drivers, compromises and uncertainty. All fifteen controls remain executable; only explicit **Use recommended setup** changes the draft. Live help updates preserve manual drafts. Reconnection checks matching assignment/team revisions before restoring an editable draft; handoff/revocation clears private content. Autoaccept retains its existing race-expiry meaning; S07 introduces no preparation timeout or automatic human submission.

[Focused evidence](../evidence/ticket05/README.md) records API/browser red-green, fourteen affected API cases (two new), two policy examples, one Guided browser flow and Angular typecheck. Public API fixtures change exact candidate compatibility diagnostics while legitimate observations remain fixed and observe identical recommendations. Versioned automatic choices replay and survive real Firestore restart; per-player SignalR/revocation/privacy are covered. The reviewed 360px capture retains supplied assets/live labels. Root owns the final broad suite and current public-display inspection; hosted and human comprehension evidence remain unverified. The pre-existing pinned NU1903 finding is recorded, not changed here.
