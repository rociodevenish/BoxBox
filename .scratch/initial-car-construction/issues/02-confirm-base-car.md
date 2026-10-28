# 02: Confirm and recover an immutable Base Car

> GitHub issue: [#33](https://github.com/rociodevenish/BoxBox/issues/33) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Confirm a car once and carry its saved identity and attributes into weekend preparation.

**Blocked by:** 01: Compare and draft an affordable Base Car.

**Status:** resolved

## Acceptance criteria

- [x] Atomically debit existing ledger and freeze build/catalogue version; reject stale, invalid, unaffordable or conflicting confirmations without partial effects.
- [x] Keep Base Car distinct from future permanent modifiers, one package and setup; initialization applies construction once and preserves source history/Pit Crew presets.
- [x] Persist draft/final revision and confirmation before acknowledgement; recovery and duplicate commands cannot charge again or replace the accepted build.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
## Answer

Implemented atomic immutable confirmation through the approved public domain and authenticated API. A conditional campaign write saves the exact existing-ledger debit, receipt/fingerprint, frozen catalogue/options/ratings/source history and both cars in every calendar event. Identical retries preserve the accepted identity; stale, unauthorised, unaffordable and conflicting commands cannot charge or replace it. Personnel/scout history remains intact, and a legitimate baseline can leave zero credits.

Construction and setup compose once through versioned effective inputs, retaining legacy neutral outputs, roster/engineer inputs and Pit Crew/conditions. The existing first lobby opens only after every frozen car is durable; interrupted confirmation, lobby handoff, preparation and race initialization recover through identical retries and reads. A focused confirmation UI keeps uncertain commands across reload and shows the locked paid-once result.

See [mapping, verification and reviewed captures](../evidence/02-confirmation.md). Final focused checks: 34 domain/preset/personnel tests, four authenticated persistence/CAS/restart cases, one browser flow, Angular typecheck and diff whitespace passed. Ticket 03 can reuse `ConfirmConstruction` and the repairable all-team handoff for its policy/readiness work.
