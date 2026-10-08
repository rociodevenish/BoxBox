# 02: Commit points and rebuild championship standings

**What to build:** Finish an event and see authoritative Drivers' and Constructors' standings with explained scoring.

**Blocked by:** 01: Create a durable short campaign and first event.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Use versioned game points 25/18/15/12/10/8/6/4/2/1 by raw finishing position, no fastest-lap bonus; retired/ineligible drivers score zero without promoting others.
- [ ] Sum both drivers for constructors; ties use counts of higher eligible finishes then fixed campaign starting order; driver points follow stable person identity.
- [ ] Commit each event contribution once with raw Classification/status/distance/eligibility retained; rebuild standings and survive concurrent commits, failed writes and retries.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
