# Ticket 01 evidence

Completed on codex/s10-01 after integrating 69e40c2 (integration scope/map). See [runtime model](../../../docs/circuit-weather.md) and [ticket answer](../issues/01-conditions-forecasts.md).

Public seams: RaceEngine/TeamObservation/CircuitWeather, session HTTP and SignalR, rendered race controller/public display. No private formula/scheduler tests.

Unique final checks:
- Focused simulation weather, replay, interruption and preset tests: **31 passed**, `.tools/reports/s10-01-simulation-complete.json`. Final command PID 3444 recorded by the bounded runner (see original manifest for authoritative PID).
- Precisely scoped WeatherTests, SessionTests and RecoveryTests API checks: **12 passed**, `.tools/reports/s10-01-api-complete.json`, launched dotnet PID **29408**. Includes own-team forecast stability over HTTP and SignalR, accepted command retry, and serialized store replay in a fresh real API host.
- Focused real browser weather flow: **1 passed**, `.tools/reports/s10-01-browser-green2.json`, launched Playwright PID **4996**; owned fixture API **4631**, UI **4531**. Both listeners absent after run. No emulator required by this narrow fixture.
- Browser Angular development compile passed as part of this flow; no separate production-build claim.
- Red-first evidence: simulation missing Weather/Forecast projection reports `s10-01-red.json`, `s10-01-red2.json`; API missing weather projection `s10-01-api-red.json`; browser missing observed-weather region `s10-01-browser-red2.json`. Subsequent browser rerun corrected textContent/innerText normalization in the test.

Reviewed captures before focused compatibility checks:
- [360×800 phone complete controller flow](s10-01-controller-360.png): full page, reduced motion; observed vs predicted headings, probabilities, both horizons and low confidence readable, both-car choices intact, no horizontal overflow. Refresh preserved forecast; actual accepted phase advanced it.
- [1920×1080 public display](s10-01-display-1920.png): full page, readable observed rain/wetness and exact timing table; private forecast absent. Existing development race-facts footer remains below the viewport.

The broader first API filter accidentally selected one existing emulator-dependent DurableRecoveryTests case; that case failed because Auth19099 was not running (29 other cases passed). It was excluded with explicit class-qualified filters for this scope; emulator-backed household persistence is not claimed here. NuGetAudit=false was invocation-only, with no dependency change. No full-root verification or balance/pacing evidence was run; final integration owns those checks. No wall-clock sleeps or player reads alter weather. Weather remains opt-in until later wet-weekend slices integrate it.
