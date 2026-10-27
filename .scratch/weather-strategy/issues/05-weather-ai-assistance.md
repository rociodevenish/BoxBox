# 05: Guide and recover weather decisions

**What to build:** Use fair AI and uncertain Assisted/Guided advice throughout a recoverable wet weekend.

**Blocked by:** 03: React to significant weather changes; 04: Complete wet qualifying and select starting tyres; qualifying-race-start 03.

**Status:** resolved

## Acceptance criteria

- [x] Record policies and choices for starting tyres and reaction windows using legitimate observations; hidden future weather changes do not alter advice when observations are fixed.
- [x] Persist forecasts, trigger identities, locks and mixed-window outcomes; retries/restart never reroll or advance twice.
- [x] Render clear two-car controls, forecast horizons and uncertainty with supplied assets/fallbacks, privacy modes, Guided options and reduced motion.
- [x] Verify this slice through the parent spec-approved public API/SignalR or public domain/simulation boundary as appropriate, with focused browser coverage for user interaction and persistence/retry checks for durable transitions.

## Spec and supplied content

[Parent spec](../spec.md). Read its implementation, testing, scope, and Data and asset references before implementation. Review the named driver/track/car/construction data as relevant; record provisional-to-runtime mappings and content versions without converting card stars silently into physics.

For presentation, inspect the current supplied asset collection, including new additions; map stable identities, preserve originals, record provenance and runtime packaging, and retain live text and readable fallbacks. Do not infer simulation geometry or rules from imagery.

## Answer

Implemented observation-only weather AI and saved Standard/Assisted/Guided help, preparation/start forecasts and private uncertainty ranges. Both-car controls preserve committed stops, mandatory Safety Car choices and existing eligibility. Real-Firestore before/after-commit failures verify starting locks and one mixed Weather outcome; four focused browser flows and actual 1920×1080 public captures verify the rendered slice. See [runtime rules](../../../docs/weather-assistance.md) and [checks/captures](../evidence/05-weather-advice.md). Partial-lock recovery is API-tested; the attempted extra browser partial-lock setup did not reach weather and is not counted. Live activation and broader experiments remain ticket 06.
