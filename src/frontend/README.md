# Frontend fixture gallery

Angular22.2.1 / Tailwind4.3.3. Use Node24.15+ (verified24.19.0); the repository machine's default Node20 is too old. Dependency versions and `package-lock.json` are committed. From this directory:

```powershell
npm ci
npm start
npm run build
npm run typecheck
npm test
```

The gallery runs at `http://127.0.0.1:4400/`; port4400 is also the browser-test server. `npm start -- --port4401` can run a separate review instance. Playwright automatically starts the test server when needed. Install the pinned browser with `npx playwright install chromium`, or reuse the approved local cache using `PLAYWRIGHT_BROWSERS_PATH=C:/Projects/F1Game/.tools/playwright`. Optional `PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH` selects an existing Chromium executable. The local implementation used the shared browser cache without another download.

On this host, prepend `C:/Users/sbdev/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin` to PATH for Node24. The npm command remains available from the installed npm distribution. Generated node_modules, dist, .angular, test-results and playwright-report directories are ignored.

## Public UI boundary

`DriverCard` accepts a `DriverCardViewModel` and compact/expanded/tv variant. Names, simple rating groups, detailed attributes, preferences, optional display price, portrait/crop and optional ownership are presentation inputs. Unknown ratings are explicit nulls. The model defines no conversion into simulation attributes. Fixtures are separate under `src/app/gallery`; changing an asset manifest or view model does not require redesigning a component.

Cards expose a labelled details button and a native modal details dialog. Clicking Close or pressing Escape returns focus to the initiating card. The read-only details dialog has one focusable control; its Tab handler keeps both directions on Close. If later features add interactive dialog content, extend this keyboard flow rather than blocking those controls. Failed/missing portraits use neutral initials and a written message while preserving full identity and details access.

`design/tokens.css` remains the semantic source. `src/styles.css` maps semantic aliases into Tailwind's inline theme; responsive grids, padding and typography use utilities. Component CSS owns portrait layering, stripes, card anatomy and dialog treatments. Ownership and Driver role have separate tokens and labels.

This gallery has no authoritative session, network API, simulation dependency or race renderer. Public/private screen fixtures are added by later tickets. No recovery-prototype acquisition/state model was copied.

## Validation and visual review

`npm test` runs the agreed rendered gallery boundary through Playwright roles and labels. Behaviors cover details/focus return, real image404 fallback, unknown ratings/long identity, keyboard Tab/Shift+Tab/Escape, phone touch, three viewport layouts and44px targets. `npm run capture` runs the three visual cases, saving representative screenshots and actual crop dimensions under `.scratch/visual-design-foundation/evidence`; capture generation alone is not acceptance.

See [ticket02 review](../../.scratch/visual-design-foundation/evidence/driver-gallery-review.md) and [visual system](../../docs/visual-design-system.md). The built initial bundle is148.18KB raw /43.78KB estimated transfer in this revision. This excludes portraits: the two supplied original PNGs total about4.5MB and are intentionally preserved. Angular's development oversized-image warning remains visible; responsive asset delivery is a future optimization, not proof of controller bandwidth readiness. Full-roster images are not shipped.

Integration follows Angular's [standalone bootstrap documentation](https://angular.dev/api/platform-browser/bootstrapApplication) and Tailwind's [Angular PostCSS guide](https://tailwindcss.com/docs/installation/framework-guides/angular). No SSR or 3D bundle is installed.
