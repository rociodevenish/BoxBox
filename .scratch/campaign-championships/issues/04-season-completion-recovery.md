# 04: Recover multi-day campaigns and conclude the season

**What to build:** Resume a multi-day campaign safely and show final champions after its last event.

**Blocked by:** 03: Review results and prepare asynchronously for the next event.

**Status:** resolved (s11-04)

## Acceptance criteria

- [x] Persist model/content versions and replayable inputs rather than engine internals; unsupported saves fail actionably without loss.
- [x] Exercise event creation/result commit/preparation recovery, cross-campaign authority and revocation without duplicated points or lost control.
- [x] Mark final season completion once, show Constructors as primary and Drivers as secondary results, and prevent unintended extra events.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented durable compatible-save validation, explicit absent-team preparation, boundary-safe returning-device recovery and atomic final-season completion. See [Answer and verification](../evidence/04-recovery.md) and [runtime contract](../../../docs/campaign-runtime.md).
