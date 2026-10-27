# 01: Display a representative authoritative race board

**What to build:** View a stylised representative circuit and cars alongside the exact timing tower without changing race truth.

**Blocked by:** weather-strategy 05; visual-design-foundation 06; full-calendar-content 01.

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Lazy-load Babylon.js only on display routes; Angular DOM/Tailwind retains cards/text/controls and controller bundles exclude the renderer.
- [ ] Consume allowlisted stable event IDs, revisions and final snapshots; use explicit representative circuit geometry/asset mapping, never infer physics from track PNGs.
- [ ] Keep permanent semantic DOM summaries/tower and fall back on WebGL/asset failure; renderer has no physics/randomness/resolution authority.
- [ ] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.
