# 04: Confirm and recover preparation

**What to build:** Confirm final-ready preparation, recover it after restart and provide the atomic freeze operation for qualifying.

**Blocked by:** 03: Run one practice assessment.

**Status:** resolved

## Acceptance criteria

- [x] Persist revisions, assessment inputs/output and versions before acknowledgement; retry and restart preserve feedback and accepted confirmation.
- [x] Final-ready does not start qualifying; provide a transition that freezes all team setup/package inputs atomically when qualifying later invokes it.
- [x] Enforce authority, privacy and lifecycle checks; reject changes after lock and recover usable preparation through API/SignalR and controller screens.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Added durable final-ready confirmation and explicit reopen. Confirmation accepts only the saved assessed candidate; reopening withdraws readiness and preserves the single original assessment, inputs/output and versions. Current controller authority precedes canonical receipt lookup. Identical accepted retries remain acknowledgement-only after later revisions or lock; changed command payload/operation and stale lifecycle/revisions conflict. The controller recovers final-ready state, disables edits until reopen and clears private content immediately after shared-tablet confirmation.

Added the host-authoritative `FreezePreparationForQualifying` application operation and authenticated API seam. It validates the complete eleven-team catalogue and every team's final readiness before a single durable mutation captures preset/base/package/source identities, final setup, effective inputs and all versions, locks every team and stores the immutable context/receipt. Incomplete fields cause no partial lock; storage failure restores complete durable state and publishes no success. Accepted retries recover the same frozen context after restart; new mutations cannot rewrite it. Public preparation reveals readiness flags only, and existing authorised SignalR/GET projections recover locked private state. S08 must integrate this operation into the actual qualifying start; S07 implements no qualifying UI/resolution, race engine or deadline.

[Focused evidence](../evidence/ticket04/README.md) records the observed browser red, twelve passing API cases with real Auth/Firestore recovery and interrupted writes, two passing browser flows, Angular typecheck and the reviewed 360px final-ready capture. Existing source identities, provisional mappings and asset packaging remain unchanged. Full regression, current public-display inspection and hosted/human evidence remain root responsibilities; the known pinned-package NU1903 finding is explicitly recorded rather than remediated by this ticket.
