# S14 01 — event income and shared credit history

Canonical issue: [#70](https://github.com/rociodevenish/BoxBox/issues/70).
Base: `d5b60e3312f758f32fc92a57f93cd5b5b0602a27`.

Implemented one flat grant per team and committed Event Contribution, including the final event. The frozen provisional `flat-event-income-v1` configuration grants **6 integer credits** per event (designer range 0–20), independent of finish, assistance and login. This is a tuning hypothesis, not a release-balance finding. Acquired campaigns append to the existing acquisition/construction ledger. Preset campaigns have one `preset-starting-credit-v1` ledger with 100 starting credits; their supplied preset personnel/neutral car incur no acquisition purchases. No separate development budget is introduced.

The grant's immutable identity is `(team, event-income/event-id)` with configuration version and event identity. Grants and scoring share one campaign conditional write. Failed/uncertain writes, concurrent retries and restart cannot acknowledge or retain a second grant. Integrity reconstructs exact expected income from committed contributions, validates the frozen configuration, rejects missing/extra preset transactions and negative running balances, and preserves acquisition/construction validation. Existing construction advice is checked against its original preseason transactions rather than the later increased balance. Compatible pre-economy records receive historical receipts atomically when the existing result is next read/committed or a new result is committed; no scoring is repeated.

`GET /api/campaigns/{id}/economy/teams/{teamId}?deviceId=…` uses the current campaign weekend's refreshed household controller/membership boundary. It returns only that team's balance, history, income configuration and hired personnel display names. The public campaign snapshot and preparation display retain readiness/standings without balances/history. The campaign page exposes an explicit private history view, signed amounts, running balances, event labels, versions and a Hide action. Refresh discards the private view and reloads controller authority.

## Verification

- TDD public domain seam: initial `SeasonIncomeTests` failed to compile because the public income operation was absent; implementation then passed **3/3** focused tests for equal grants/exact worked balances/idempotence, conflicting receipt rejection, and acquired-roster serialization/retained spending. Report `.tools/reports/s14-income-domain.json`.
- Public API with real owned Auth/Firestore emulators: **2/2** campaign result cases pass (write lost before/after durable commit, concurrent coordinators, restart, exact 106 balance and one grant, outsider/other-team rejection). Final report `.tools/reports/s14-income-api-final.json`.
- Focused real browser campaign result flow: **1/1**, transaction opening, +100/+6/running 106, hide, refresh/reload retention. Reports `.tools/reports/s14-income-browser.json` and final capture run `.tools/reports/s14-income-browser-final.json`.
- Angular compiler typecheck and `git diff --check` passed. Broad verification intentionally deferred to S14 05.
- Runtime: Node 24.22.1, pinned SDK 10.0.401, Java 21. API/browser emulators Auth19589/Firestore19590, browser frontend4481/API4581. Owned launch PIDs are recorded in the command reports; runs shut down their trees. .NET tests require loopback outside the sandbox; initial sandbox testhost timed out, then the bounded permitted run passed. Offline restore used `NuGetAudit=false` after the vulnerability feed was unavailable; dependency pins were unchanged.

## Visual and content inspection

Inspected the complete 360×800 phone flow in [income history](s14-01-income-history-360.png), plus its readable [private history detail](s14-01-income-detail-360.png), and the 1920×1080 [public preparation display](s14-01-preparation-public-1920.png). Assessment: no horizontal overflow or clipped history controls; signed amounts and running balances remain legible; public display shows preparation readiness and no private credits. The existing full campaign report remains long on a phone but does not obstruct the history controls.

Reviewed the supplied assets inventory (`assets/driver images`, `assets/track images`) and current portrait/track manifests. Reused their stable driver/track identities and packaged runtime images on the surrounding campaign roster/calendar; retained text and existing missing-image fallbacks. Originals and provenance remain unchanged. No imagery, headline stars or provisional construction budgetUnits were converted into income/physics. Reviewed source schema1.0.0 car `2026.10.08-initial-model`, construction `2026.10.09-preseason-car-build-v1`, and driver/track `2026.10.08-initial-balance`; this slice adds a separately named credit configuration only.

Actual player understanding and workload remain human acceptance work; no human usability or season-balance claim is inferred from deterministic/browser checks.
