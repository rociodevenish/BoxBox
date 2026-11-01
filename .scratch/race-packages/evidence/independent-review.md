# Independent S15 review

Fixed base: `b3d861d4242f387d39f659f912b9c59d60061943`. Initial candidate: `e09b028d2c30a070227ff2443fd93a066974e8b8`. Diff: `git diff b3d861d4242f387d39f659f912b9c59d60061943...e09b028d2c30a070227ff2443fd93a066974e8b8`. Fresh Standards and Spec agents reviewed independently; usage interruptions occurred before their first attempts did work. Both completed after continuation. Tests supplemented, rather than replaced, source inspection.

## Standards

1. **P2 — Restore accepts incomplete or false selection observations.** `SessionCoordinator.Preparation.Packages.cs` checked that observed entries existed, allowing a proper subset of the actual library, and did not validate Reliability activation. This breached the documented legitimate-observation/configured-consumer contract. **Resolved:** restore now requires the complete library and actual activation using the same observation helper. Two public persistence/restart regressions returned 200 before the fix and 409 afterward.
2. **Product-flow rule breach — Implementation jargon.** Rush/rich descriptions exposed class-draw and independent-risk-draw terminology directly. **Resolved:** frontend-only wording explains the displayed probabilities and one project outcome. Saved backend text, quote IDs and replay remain unchanged.
3. **Possible Duplicated Code / Repeated Switches — judgement call.** Selection and investment policies duplicated capability-demand mapping, conditional Reliability weighting, clamping and weighted changes. **Resolved:** `CircuitDemandFit` centralizes the calculation while preserving each policy's original addition order, normalization and rounding.

The Standards reviewer independently confirmed all three fixes at `6ee1dbffbc2244ef21847ced851b685e2b8c9de7`, with no introduced actionable Standards findings. Initial findings: **3**, worst **P2**; remaining: **0**.

## Spec

Missing/partial requirements: none identified within accepted scope. Scope creep: none identified. Full-calendar and actual human-understanding limitations are disclosed.

**P2 — Broadening can erase its own improvement when strength and weakness share an attribute.** The spec requires bounded broadening; a legitimate partial-cooling → compromised-broadening → two compromise-reduction history leaves Reliability as both strongest positive and largest compromise. The chained update overwrote the improvement, reducing Reliability from 1.75 to 1.25 without another gain. **Resolved:** new drafts explicitly reject this ambiguous operation. A public paid-history regression reproduces the case and verifies new-quote rejection. Existing accepted v2/v3 contracts retain their exact quote identities and original outcome semantics through saved-contract reconstruction.

The Spec reviewer independently confirmed the fix and inspected the remaining changes at `6ee1dbf`, finding no introduced actionable Spec issues. Initial findings: **1**, worst **P2**; remaining: **0**. No human/full-calendar acceptance was inferred.

All fixes were integrated through one implementer branch, with evidence-only regeneration assistance. See [review checks and captures](review-fixes.md), [refreshed replay evidence](ticket05-reviewed.md) and [completion manifest](completion.json).
