# S12 review remediation

Base: `8458f00dd8b43725395f8746811e371e0a100f72`, branch `codex/s12-review-fixes`.

The two Standards P3 duplication findings are addressed by a shared private personnel-command workflow and shared Scout allocation validation/calculation. The clean Spec review requires no feature change. Hire still explicitly refreshes the campaign after acknowledgement. The helper captures the original team and private-view generation, retries the saved command payload, clears definitive 4xx failures, retains ambiguous failures and refuses to reopen a hidden private view. Open and saved-state validation retain their distinct error messages and all content/save versions and replay formulas.

This is review-stage refactoring. Existing spec-approved public domain and focused browser seams are reused; no implementation-shaped tests were added.

Final Angular typecheck passed. All 14 public personnel domain cases passed against the final helper. The frozen archive replay verified all 48 schedules, observations/actions, assignments/debits and completion summaries exactly without catalog access; the final tool build had zero warnings/errors.

Owned browser ports: UI 4696/API 4796, Auth 22479/Firestore 22480/websocket 22484/hub 22481/logging 22482. Existing services were preserved. The first browser attempt hit the existing 180-second emulator startup deadline before tests; a bounded 90-second diagnostic reached the CLI startup header only after 47 seconds of module loading, then expired. A temporary test-only configuration raises startup to 300 seconds for the focused retry; production configuration/budgets are unchanged.

## Harness correction

The 300-second retry exited before product checks because the temporary Firebase configuration used a rules path outside its project directory. Once command startup returned to normal, an explicit debug launch identified this setup error. The configuration now uses an unchanged copy of the repository rules beside the ignored config; no production source or rules changed. The earlier timeout/module-load observations are harness/startup evidence, not product failures.

## Integration regression: final manual wakeup configuration

The coordinator's integration backend run reported a qualifying recovery conflict at the final explicit sweep in `Weather_start_advice_forecasts_and_partial_locks_survive_real_Firestore_write_failure(commitFirst:true)`. The exact existing two-case public API test reproduced that same committed-write conflict on an isolated fresh emulator pair; a second run with console diagnostics also failed. This ruled out campaign enumeration and cross-fixture data as necessary triggers.

Ranked hypotheses were stale recovery state, another live host delivering concurrently despite manual configuration, and a recovery read persisting without refreshing its version. A temporary persistence-boundary file probe showed the original and restarted coordinator both writing the same expected session version at clock advance. That instrumented run passed because the background write lost the race; the trace still demonstrated concurrent delivery and the failure is timing-sensitive.

`Program` selected local hosted services before `WebApplicationFactory.ConfigureAppConfiguration` completed the test's explicit `Wakeups:Mode=manual` override. `LocalWakeupDelivery` now reads final `IConfiguration` before starting its timer and returns for manual mode. Automatic local delivery retains its existing loop; Cloud Tasks is unchanged. This honors configured manual operation and keeps explicit sweeps as the only delivery in these tests. All temporary instrumentation was removed. The unchanged original two cases passed after the fix; no delay or weakened assertion was introduced.

API checks use owned Auth 22490/Firestore 22491/websocket 22494/hub 22492/logging 22493 and a separate `.tools/remediation-weather-dotnet` artifact directory. One early probe build collided with a concurrently running browser API DLL; subsequent checks were sequenced and given distinct build artifacts. This was a harness error, not a product failure.

## Focused API verification after the fix

`related-api.json` passed 6/6: both original ambiguous-write weather starting-tyre cases, qualifying deadline/pause/sweep preserving accepted locks, the wet-grid starting-tyre restart/timeout/pause flow, and both personnel deadline failed-write/committed-write cases (including repeated delivery and all 77 picks). The owned emulator wrapper also passed and shut down cleanly. The existing public tests exercise explicit manual progression and were left unchanged.

## Final browser and visual verification

`browser-complete.json` passed both existing acquisition flows. They cover saved offer/reconnect stability, Scout replacement and token conservation, delayed private-response hiding, revoked-device 403, lost Scout/Hire acknowledgements, reserve rejection, all 77 picks, a recovered final hire and the ready roster. The completion case took 56.4 seconds and the saved-command/privacy case 13.3 seconds, with 153.9 seconds including owned service startup/cleanup.

The final temporary harness retains the existing acquisition config except emulator startup 300 seconds and an expectation wait 20 seconds to accommodate cold campaign creation. The earlier corrected run passed Scout/privacy but exceeded the default 5-second wait while initial campaign creation was still busy; no application error or wrong state appeared. The final run was sequenced after API checks and used a distinct `.tools/remediation-browser-dotnet` directory. No test assertions, production timeout, or checked-in test configuration changed. The temporary frontend config was removed after the run.

Inspected these new actual captures with `view_image`, including original-detail phone captures:

- `s12-04-driver-offers-360.png`: 360×800 viewport,360×7064 full page. Three portraits, live names, prices, compatibility explanations, Scout state and Hire controls fit inside the cards.
- `s12-03-reserve-360.png`: 360×800 viewport,360×6769 full page. Clear protected reserve and blocked-hire explanation, disabled illegal Hire, readable future-only staff labels, contained ledger and role summary.
- `s12-04-ready-360.png`: 360×800 viewport,360×6983 full page. All seven places, saved ledger, protected 18-credit reserve, final identities and public roster remain legible with vertical scrolling and no horizontal overflow.
- `s12-03-public-top-1920.png`:actual 1920×1080 viewport at scroll position 0 after Hide. Campaign state, calendar and constructor actions are visible; private balance/offers/advice/ledger are absent.

All captures are from the final passing run. Earlier tracked evidence was preserved. This worker did not run a broad verifier; the integration coordinator owns the remaining named checks.

Owned preview and emulator ports were checked after the final run; no owned listeners remained.
