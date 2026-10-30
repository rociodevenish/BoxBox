# Slice 1: welcome and create

Implements https://github.com/rociodevenish/BoxBox/issues/113.

Root now explains managing a constructor and both drivers, preparation and simultaneous Race Phase decisions. Create opens `/campaigns/new`; Join opens the established `/shared/join`. Existing account registration and sign-in return to campaign setup through an explicit allowlist (`/` and `/campaigns/new`), preserving external redirect safety. No entry action creates a campaign. Signed-in users reach setup directly. Discovery remains slice 3.

Gallery moved to `/dev/gallery`; root `view=public`, `view=controller`, `view=results` query links deliberately replace their URL with the corresponding gallery destination, retaining other query parameters and fragments. Fixture navigation/tests updated; game deep links keep their existing routing.

## Verification

- Observed red before implementation: `home-red-escalated.json` (root heading missing; legacy URL not relocated), `home-auth-red.json` (registration left user at household page).
- Focused root/gallery journeys: `playwright.config.ts tests/player-home.spec.ts`, 2 passed, `home-captures.json`.
- Real account/API journey: `playwright.shared.config.ts player-home.spec.ts`, 1 passed, `home-auth-green-2.json`. Covers signed-out create, registration continuation, subsequent sign-in continuation, signed-in create. A test-only navigation race was corrected by waiting for completed sign-out.
- `tools/verify.mjs typecheck build gallery`: typecheck/build passed; 35/36 gallery checks passed. Missed absolute touch-only fixture entry corrected; affected `tests/driver-card.spec.ts` rerun 5/5 passed (`gallery-touch-fixed-2.json`). Parent owns integrated regression pass.
- Reports under ignored `.tools/reports/`; verification manifest `verification/runs/2026-10-30T14-19-58-241Z-7d6b77b1-26fe-4dfd-b34d-d87f1bca1053/result.json`.
- Phone 360x800 full home flow inspected: `.tools/reports/gallery/player-home-360.png`; readable heading, stacked reachable actions, no clipping or horizontal overflow. Public 1920x1080 inspected: `player-home-public-1920.png`; all 22 cars and portrait/context remain contained.
- Node24.22.1 bundled runtime; .NET10.0.402 repo SDK; Java21 repo runtime; Chromium1234 cache. UI4482/API4582/Auth19709/Firestore19700/hub19710/logging19760 for real journey; gallery UI4483. Bounded launch records retain exact PIDs; all test servers stopped after completion. Existing preview ports untouched.

Initial sandbox run could not connect local sockets and timed out under its bounded runner; approved local-network execution supplied the actual red/green evidence. Build retains existing bundle-budget/CommonJS warnings.
