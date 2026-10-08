# Weather strategy

Status: ready-for-agent
Blocked by: incidents, reliability and Safety Cars and its interrupted-phase evidence; weekend preparation and effective attributes; qualifying and automatic race start.

## Problem Statement

The complete dry weekend has no forecast uncertainty or changing tyre suitability. Players cannot plan for rain or react when conditions change, and naively adding weather could leak future outcomes, interrupt repeatedly at a threshold or disagree with Safety Car and pit timing.

## Solution

Add a simplified deterministic weather process, imperfect team forecasts, evolving track wetness and Intermediate/Wet compounds. Meaningful weather changes use the existing interrupted Race Phase flow to create simultaneous reaction decisions. Extend preparation, qualifying, starts, AI and assistance using the same authoritative conditions and legitimate information boundary, then validate complete wet/dry weekends in laboratory and browser playtests.

## User Stories

1. As a player, I want a probabilistic weekend forecast, so that I can prepare for possible rain.
2. As a player, I want current conditions distinguished from predictions, so that I understand uncertainty.
3. As a player, I want track Weather Volatility reflected in forecasts, so that circuits differ meaningfully.
4. As a player, I want changing track wetness, so that tyre suitability changes over time.
5. As a player, I want Intermediate and Wet compounds, so that I can respond to damp and heavy-rain conditions.
6. As a player, I want dry compounds to remain useful when appropriate, so that tyre timing is a trade-off.
7. As a player, I want wrong-condition pace and wear consequences explained, so that a weather stop has understandable value.
8. As a player, I want Tyre Temperature Management to influence suitability within its defined scope, so that car attributes have clear effects.
9. As a player, I want Wet-Weather Skill to matter, so that drivers differ in wet performance.
10. As a player, I want exact visible position and gaps, so that uncertainty does not obscure known race facts.
11. As a player, I want significant changes to stop the phase promptly, so that I can react before several laps are simulated.
12. As a player, I want both-car weather choices simultaneous, so that opponents cannot react to my submission.
13. As a player, I want existing queued/ongoing stops respected, so that a change cannot grant duplicate or impossible pit actions.
14. As a player, I want a clear window when rain and a Safety Car coincide, so that I do not submit conflicting decisions twice.
15. As a player, I want forecasts refreshed predictably, so that repeated page reads cannot generate better luck.
16. As a player, I want starting compound choices before the race, so that a wet grid does not force an unsuitable default.
17. As a player, I want qualifying to reflect current conditions, so that weekend weather is coherent.
18. As a player, I want the automatic start to reflect wet conditions, so that the opening uses the same model.
19. As a player, I want the compound eligibility rule explained for wet races, so that I understand final results.
20. As an Assisted player, I want weather advice to acknowledge uncertainty, so that help does not imply perfect forecasts.
21. As a host, I want AI to react from the same legitimate observations, so that it cannot know the future rain path.
22. As a player, I want timeout fallback and shared-device allowance in weather windows, so that the household can respond fairly.
23. As a player, I want restart to preserve weather and forecasts, so that interruptions cannot reroll rain.
24. As a viewer, I want concise weather and tyre-change feedback, so that I can follow the race story.
25. As a designer, I want dry, wet, drying and changeable presets, so that competing strategies can be compared.
26. As a designer, I want weather disabled for regression checks, so that dry evidence remains reproducible.
27. As a designer, I want crossover and forecast sensitivity evidence, so that thresholds are tuning hypotheses rather than guesses presented as facts.
28. As a household, I want weather decisions paced clearly, so that the full weekend remains approachable.

## Implementation Decisions

- Extend domain/simulation configuration with weather process inputs, public current conditions, authoritative track wetness and versioned forecast observations. Share this model across preparation, qualifying, start and race; persistence and UI do not evolve weather independently.
- Model bounded rain intensity and one simplified circuit-wide wetness state. Wetness responds over elapsed time to rain and configurable drying; spatial puddles, wind, temperature simulation and track grip-rubber evolution are deferred.
- Use a hidden seeded weather path keyed to stable time intervals/events and track Weather Volatility. Repeated integration/recalculation, strategy choices and diagnostic reads cannot reroll weather. Weather advances with simulation time, not wall-clock decision time or display animation.
- Let current public conditions be directly observed; forecasts are probabilistic estimates over documented horizons, not the hidden future path. Generate stable bounded forecast error with named observation identities, record its version and prevent repeated reads from improving information. Preset information quality can narrow uncertainty without revealing exact future weather.
- Add Intermediate and Wet Tyre Compounds with configurable warm-up, degradation and suitability curves. Retain unlimited sets and existing stop service semantics. Wrong-condition pace and wear are continuous bounded consequences, not automatic disqualification or punctures.
- Implement relevant Wet-Weather Skill, Tyre Temperature Management and condition-sensitive traction/error effects through the effective-attribute model. Integrate these with the existing context-driven incident risks once, avoiding an extra independent weather accident roll for the same opportunity.
- Maintain exact player-visible Classification and gaps; hidden tyre condition, form, future rain and random draws remain diagnostic-only. Compound, age, observed conditions and own-team forecast/estimates use allowlisted public/private projections.
- Define significant weather transitions by configurable suitability bands with hysteresis and a minimum simulated separation. Detect the transition at its actual time using the existing earliest-event flow; avoid repeated windows from noise around one threshold.
- A weather reaction window accepts complete simultaneous two-car Pace, Pit/compound and legal contextual Battle Instructions. A new pit still occurs at that driver's next lap-end; preserved queued/ongoing stops cannot be cancelled or duplicated by a new window.
- Weather may also change during Safety Car running. Merge coincident strategic triggers into one identified decision window with all applicable legal choices rather than duplicate lock cycles; Safety Car control remains active and determines neutralised running/Pit Loss. Document stable precedence for weather, incident, phase-boundary and finish events.
- Reuse durable special-window locks, server deadlines, shared-device allowance, pause and fallback. Commit observed conditions, trigger identity and concrete human/automatic choices; deterministic recovery returns the same weather state and forecast rather than sampling anew.
- Qualifying retains one abstract attempt per remaining driver. Use the current authoritative session conditions and bounded weather effects consistently, with all attempts in one session sharing a documented abstract time interval rather than giving submission-order advantages. Forecasts may inform approach but no new qualifying tyre/run-management game is added.
- Before automatic start, add one simultaneous starting-compound choice per active driver with disclosed automatic fallback. This is a tyre-selection step, not a launch-strategy decision. Persist it with the grid; the opening uses current conditions and the shared incidents flow.
- Make wet-race eligibility an explicit game rule: when Intermediate or Wet is actually used in the race, exempt that driver from the two-dry-compound requirement. Forecast rain or wet-compound use only in qualifying does not grant exemption. Preserve raw outcomes and report the rule/version; do not silently force stops.
- Extend observation-only AI and Standard/Assisted/Guided recommendations for forecasts, starting tyres and weather reactions. Freeze policies/choices at each window, label uncertainty and avoid counterfactual certainty or access to the hidden weather path.
- Extend Angular/Tailwind weather summaries, forecast probabilities/horizons, compound controls and concise event treatments. Distinguish observed and predicted information with text, not only colour; inspect supplied driver/track assets, preserve live labels and accessible fallbacks.
- Supply all-dry, steady-wet, rain-arrival, drying and changeable presets on contrasting tracks. Publish paired-seed crossover/stop-window and forecast-quality experiments, with fixed exogenous weather identities and opponent policy versions, eligibility/distance groups and uncertainty.
- Measure added decision/presentation time, forecast understanding and simultaneous-trigger clarity in full weekends. Numerical thresholds and approximately thirty-minute duration remain measured hypotheses rather than promised results.

## Testing Decisions

- User-approved main boundary: public session API and SignalR. Observe forecast/current-state projections, starting tyre commands, weather reaction locks and resumed results through authenticated session requests/messages.
- Extend existing public preparation, qualifying/start and Race Phase operations; use deterministic weather inputs and zeroable variability to test observable suitability, transition timing and integrated results. Avoid private forecast formulas or scheduler structures.
- Reuse replay, tyre/pit, interrupted-event, random-channel and persisted deadline tests. Verify same input/version/seed replay, read stability, strategy-independent weather draws and all-dry compatibility.
- Cover rain onset mid-segment, drying crossover, threshold hysteresis, queued/ongoing stops, coincident Safety Car deployment, weather during neutralisation and finish at a trigger timestamp. Assert one applicable window and no duplicate decisions/stops.
- Verify qualifying conditions and wet starts use the same process without command-order advantages; test dry/wet eligibility and retain raw results.
- Hold legitimate observations constant while changing hidden future weather/diagnostics; AI/advice output must remain unchanged. Public/private payload checks cover events, forecasts, reconnect and persisted recovery.
- Restart after forecast, trigger, starting-compound lock and mixed-trigger result commits; duplicate callbacks must not reroll forecasts or advance twice.
- Add a small browser wet/drying race flow with both-car choices, uncertainty labels, Guided options, reduced motion and refresh during a reaction window.
- Publish reproducible multi-seed evidence and manual full-weekend clarity/pacing findings separately from unit correctness; report unsupported or inconclusive balance claims explicitly.

## Out of Scope

- Real meteorological forecasting, geographically varying rain, detailed aquaplaning, puddles, wind and continuous tyre-temperature physics.
- Red Flags, visibility-related race suspension, weather-based cancellation and full real-world wet-race regulations.
- Tyre inventories, multiple qualifying runs, campaign weather scheduling and complete calendar climate data.
- New launch tactics, personnel acquisition, advanced 3D weather effects and final tyre/weather balance.

## Further Notes

Weather deliberately follows proof of the interrupted Race Phase and Safety Car flow. Forecasts provide legitimate uncertainty while actual outcomes remain exactly reproducible. The wet-race eligibility and qualifying abstraction are explicit game rules to validate, not claims of sporting-rule fidelity. The user approved the shared testing boundaries.
