# 01: Receive event income and inspect the shared ledger

**What to build:** See preseason spending retained and a modest one-time event grant in readable transaction history.

**Blocked by:** campaign-championships 02; initial-car-construction 02; personnel-acquisition 01.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Extend the existing credit ledger with immutable versioned flat grants for all teams independent of finish/help/login; no second budget or negative balances.
- [ ] Commit income once with event contribution; retry, failed write or rebuild cannot grant it twice and history explains exact balances.
- [ ] Expose public readiness separately from authorised balances/history under household privacy modes.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
