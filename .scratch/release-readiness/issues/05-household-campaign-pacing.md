# 05: Validate actual multi-day household campaigns

**What to build:** Complete actual household campaigns and improve joining, decisions and pacing using recorded player findings.

**Blocked by:** 02: Teach a household to join and make its first decisions; 03: Complete accessible flows and document supported devices; 04: Evaluate full-season strategy and AI fairness.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Include private phones, shared tablet, open TV/Mixed privacy, different help levels and absent/returning players across separate days; record friction, two-car workload, engagement and developer intervention.
- [ ] Measure preparation, qualifying/race decision/simulation/presentation and recovery separately against four-to-five-minute qualifying, twenty-to-twenty-five-minute racing and roughly thirty-minute weekends.
- [ ] Fix material comprehension/usability failures and record retests; automated clicks and full-season statistics do not substitute for actual player findings or establish enjoyment.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
