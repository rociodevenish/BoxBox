# Angular visual foundation

The production Angular workspace contains reusable UI, an independently runnable fixture gallery and a local race client. The gallery demonstrates presentation and interaction. The race client consumes authoritative HTTP/SignalR snapshots; neither frontend owns simulation or a 3D renderer.

## Run and verify

Use Node 24.15 or later within the pinned Angular 22 toolchain's supported Node range. On this host, prepend `C:/Users/sbdev/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin` to PATH. Versions are pinned in package.json and package-lock.json.

```powershell
npm ci
npm start
npm run build
npm run typecheck
npm test
```

The gallery and Playwright test server use `http://127.0.0.1:4400/`. Run `npm start -- --port 4401` for an independent review instance. Browser tests start their own server when needed. Reuse the installed Chromium cache with `$env:PLAYWRIGHT_BROWSERS_PATH='C:/Projects/F1Game/.tools/playwright'`, or install the pinned browser with `npx playwright install chromium`. `PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH` optionally selects an existing compatible browser. Generated node_modules, .angular, dist, test-results and playwright-report directories are ignored.

| Entry | Observable fixtures |
| --- | --- |
| `/` | Compact, expanded and TV cards; long names, unknown attributes and missing portrait; public Classification |
| `/?view=public` | Dedicated 22-car shared display with time/lap gaps, announced pending stop and ongoing stop |
| `/?view=controller` | Private two-car decisions, confirmation, submitted waiting, acknowledgement/lock, rejection recovery, loading/resolving and pending stop |
| `/?view=results` | Initial race, resolving, quiet/crowded phase results, public overtake/pit feedback and finish |

Gallery controls switch explicit local fixtures. They do not model server validation or infer legality. Ratings, prices, teams and narratives are illustrative. Fixture labels remain outside future live data contracts or are supplied through an optional gallery label.

## Adopt the components

| Component | Input/output boundary |
| --- | --- |
| `DriverCard` | `DriverCardViewModel`, compact/expanded/tv variant. Null means unknown; ratings are presentation data with no engine conversion. |
| `TimingTower` | `PublicClassification`, preserving supplied order and distinguishing time/lap/unavailable gaps from public stop status. |
| `CompoundBadge` | Written Soft/Medium/Hard or unknown, with colour as a supplementary cue. |
| `TwoCarController` | `PrivateControllerViewModel`; emits a `TeamPlan` snapshot only after both-car confirmation. Stable car order/identity within a phase; new phase initializes supplied choices, while acknowledgement/rejection retains the current draft. |
| `PhaseResult` / `RaceEvent` | `PublicPhaseResult` and ordered `PublicRaceEvent` summaries. Filtering, chronology and authoritative lifecycle are supplied by the caller. |

Native dialogs retain keyboard focus and return it to the initiating control on Escape/close. Driver details currently has one interactive control; extend its focus cycling if more controls are added. Controller rejection is an assertive recovery alert; normal waiting states are polite status messages. Pending stop and per-car allowed battle options are supplied, not calculated in the component. A confirmed revision restores accepted choices while rejection preserves the editable draft. Public contracts have no tyre condition, private estimates, random draws or opponent decisions. The server filters wire contracts before the race adapter maps them into component models.

`design/tokens.css` is canonical. `src/styles.css` maps semantic aliases into Tailwind's inline theme and supplies shared focus treatment; component CSS owns stripes, portraits and complex graphics. Ownership, role, ratings and preferences remain distinct. See [the visual system](../../docs/visual-design-system.md) for rules and extension guidance.

## Assets and verification

The portrait manifest under `public/portraits/` maps stable driver identity, source provenance, URL and crop. Only two supplied original PNGs are packaged (about 4.5 MB), separately from the JavaScript bundle. Missing/failed imagery preserves live identity, details and race information. Follow [asset adoption instructions](../../docs/visual-design-system.md#adding-supplied-assets) for driver, track or other imagery. Full roster assets and responsive image delivery are deferred; Angular's oversized-image warning remains visible. Bundle measurements exclude portraits and are not a claim about controller bandwidth.

`npm test` exercises the user-approved rendered Angular boundary through visible roles, labels and interactions. Current integrated checks cover cards/details/focus, two-car choices and recovery, stop/battle availability, public result states, missing images, reduced motion, touch targets and representative layouts. `npm run capture` regenerates card captures; run individual `*-visual-review.spec.ts` files for tower/controller/results/integrated compositions. Captures are evidence only after inspection. Keep a small reviewed set; compare dimensions and review layout, rather than treating generation or pixel equality as acceptance.

[Integrated review](../../.scratch/visual-design-foundation/evidence/integrated-review.md) is the current verification record and links prior inspected captures with actual dimensions. Earlier per-ticket bundle sizes and static `design-baseline/` compositions are historical evidence, not the adopted app or its current bundle. The static baseline established the charcoal/coral direction; it supplies neither production state nor race orchestration. Desktop Chromium review does not establish living-room TV readability, physical-device usability or a full accessibility audit.

The complete local dry race is documented in [browser-dry-race.md](../../docs/browser-dry-race.md). Use npm run start:race and open /race/new; npm run test:browser uses separate ports 4410/4510. RaceSession handles authoritative revisions and commands; RacePage holds the previous published table during recorded feedback, then maps the exact result into PhaseResult. Playback, skip and replay do not open server decision windows. Result context labels are supplied by the caller so a first finisher is not automatically described as an eligible winner.

The authenticated household lobby begins at `/shared/new`; see [local auth and joining](../../docs/shared-sessions.md). The solo laboratory and gallery remain independent.
