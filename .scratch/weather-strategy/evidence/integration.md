# S10 integration scope

Completed engineering, verification and review: [final record](final/README.md). The sequencing below is the original integration plan; real household measurements remain pending.

Implement S10 from completed S09 on `codex/weather-strategy`. The public session API/SignalR, existing preparation/qualifying/start/Race Phase operations and focused rendered browser are the testing boundaries explicitly approved in the parent spec. Use isolated ticket worktrees, TDD vertical slices and independent merges; 03 and 04 become independently ready after 02, then join at 05.

Weather is one deterministic authoritative simulation-time process. Stable exogenous time identities, bounded wetness/drying, named forecast observations and versioned source inputs must survive strategy changes, subdivision, reads, waiting and recovery. Public current conditions, private uncertain forecasts and hidden future weather/diagnostics have separate allowlisted projections. Keep weather off/null paths byte-compatible with prior dry and S09 replay contracts; no silent reinterpretation of historical versions.

Integrate tyre suitability and weather-dependent effective attributes with the existing incident opportunity once. Detect actual significant crossings with hysteresis and separation; merge coincident weather/incident/Safety Car decisions, preserve physical neutralisation, queued/ongoing stops, all-car shared time and finish precedence. Starting compounds are a simultaneous tyre-selection step, not launch tactics. Qualifying uses a shared abstract interval independent of submission order. Wet eligibility requires actual Intermediate/Wet race use.

Inspect supplied data and current assets. Treat detailed driver/car ratings and track characteristics as provisional design inputs with explicit units/mappings; weather and tyre curves are separately authored/versioned. Never infer geometry from images or convert card stars silently into physics.

Before the single broad verification, inspect complete 360px phone and actual1920×1080 public flows, including all22 classification rows. New browser integration directories require exclusion from default gallery discovery and dedicated fixture configuration. After failures run only affected/remaining named stages. Independent Standards and Spec reviews use baseline `51c18a5e8db556372ca4021ad9dfc75dac5d8fd6`; one remediation implementer owns all findings.

Preserve immutable commands/PIDs/ports, source/model/forecast versions, frozen paired experiment inputs, distance/eligibility groups and uncertainty. Use Node24.19.0, global.json SDK, Java21 and local demo emulators with explicit host resolution, isolated artifacts and owned process cleanup. Command-scoped NuGetAudit=false is only an accommodation for the already recorded SignalR10.0.12 advisory, not remediation.

Actual household forecast comprehension, mixed-trigger clarity, added decision/presentation time and hosted acceptance require real playtests. Keep those pending rather than substituting automated wall-clock time or agent image inspection. No push, PR, deployment or S11 is requested.
