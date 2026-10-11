# Observation-driven AI evidence

Policy `dry-observation-ai-v2` replaces scripted fill-ins without changing simulation coefficients, driver ratings or execution rules. Its bounded evaluator receives only a team observation, legitimate observed gaps, race distance and phase. It considers estimated tyre condition, own completed compound use, queued/in-progress stops, remaining laps, visible order and legal nearby battle options. Missing timing stays uncertain; a legal nearby battle may justify an attack without claiming a measured gap. All suggested choice groups remain complete; confidence is conservatively false for subsequent assistance presentation work.

Own observations now distinguish completed compounds from a queued target compound. These are known team instructions/history, not hidden exact tyre condition. The application preserves its explicit private/public payload filtering. Policy is deterministic; it takes no engine, seed, form, opponents' locks, future draws or candidate future simulation inputs. Engine validation applies ordinary legality and every benchmark race uses that same public resolver.

## Freeze and recovery

Automatic choices are frozen at creation/window opening, before human locks. The durable recovery record saves policy, explicit unclaimed-ai source, frozen concrete instructions and complete committed phase choices. Designer replay adds phase/source/policy/decision records, outside ordinary player payloads. Restart reuses frozen choices. Supported `dry-opponents-v1` records replay saved instructions and retain their original policy for future phase choices; unsupported policies produce an actionable preserved-input limitation. No older race is silently upgraded to v2.

## Focused checks

- RED: public engine observation test could not compile without own pending/used compound facts; GREEN: `TeamStopObservationTests` passes.
- RED: API battle behavior retained Neutral when measured timing was unavailable at initial phase; GREEN: a legal nearby option can select Attack with uncertain timing. The battle fixture uses 40 laps because short-race eligibility deliberately recommends an immediate stop.
- Initial 480-race experiment exposed ineligible six-lap leaders (401/420 AI entries in typical groups). A public API regression with an AI leader reproduced the missed final-lap stop: one of20 AI cars ineligible. GREEN: plan the mandatory change before the last feasible five-lap decision window. No simulation mechanic changed.
- Final public API fairness/battle/compound, designer replay and stop-boundary filter:9 passed; report `.tools/reports/s06-ai-focused.trx`.
- Final observation pending-stop/stability/hidden exclusions filter:3 passed; bounded report `.tools/reports/s06-ai-observation-final.json`. Structural equivalence checks nested compound history values instead of list reference identity.
- Real isolated Firestore restart check compares all concrete automatic choices with an uninterrupted race after repeated observation reads; owned Auth19229, Firestore19220, hub19420, logging19470. Reports `.tools/reports/s06-ai-recovery-final.json` and inner `.tools/reports/s06-ai-recovery-inner.json`.

NuGet vulnerability audit metadata was inaccessible in sandbox; restore used `NuGetAudit=false` only for this local invocation, without changing project settings. Sandbox testhost IPC failed initially; actual behavior checks ran through the same public boundary with elevated local testhost permission. Builds use pinned .NET10.0.401, and emulator execution uses Node24.19.0/Microsoft OpenJDK21. No broad suite or browser/gallery run was performed for this backend-only ticket; integrated verification owns those checks.

## Reproducible strength comparisons

Run `dotnet build tools/AiComparisons -c Release`, then `dotnet tools/AiComparisons/bin/Release/net10.0/AiComparisons.dll .tools/reports/ai-comparisons.json` through the bounded runner. This serial workload contains480 races: seeds7–26, distances6/40, open-equal/restricted-equal/open-mixed/restricted-mixed, and balanced-one-stop/early-two-stop/conserve-one-stop representative strategies. A comparison driver rotates through20 initial grid positions; the other21 cars use AI. Strategies obey pending-stop rules and all use a second compound. On six laps the two-stop strategy intentionally cannot fit an extra stop, so it collapses to the one-stop plan; it is not independent strength evidence.

[Raw measurements](ai-comparisons.json) include every sample's eligibility, finishing position/time, stops, phase count and separately measured policy/resolution times, plus source SHA256 hashes. Timings are serial local diagnostics, not hosted latency. Mean position intervals use sample SD and Student t(19)=2.093; they are descriptive across seeds/grid positions, not paired causal effects. The exploratory mixed field and track/passing fixtures remain provisional.

| Layout | Laps | Representative legal strategy | Mean finishing position (95% descriptive interval) | Eligible representative / AI |
|---|---:|---|---|---|
| open-equal | 6 | balanced-one-stop | 14.05 (11.25–16.85) | 20/20; 420/420 |
| open-equal | 6 | early-two-stop | 14.05 (11.25–16.85) | 20/20; 420/420 |
| open-equal | 6 | conserve-one-stop | 14.55 (11.72–17.38) | 20/20; 420/420 |
| restricted-equal | 6 | balanced-one-stop | 12.35 (9.15–15.55) | 20/20; 420/420 |
| restricted-equal | 6 | early-two-stop | 12.35 (9.15–15.55) | 20/20; 420/420 |
| restricted-equal | 6 | conserve-one-stop | 13.25 (10.18–16.32) | 20/20; 420/420 |
| open-mixed | 6 | balanced-one-stop | 13.80 (11.18–16.42) | 20/20; 420/420 |
| open-mixed | 6 | early-two-stop | 13.80 (11.18–16.42) | 20/20; 420/420 |
| open-mixed | 6 | conserve-one-stop | 15.00 (12.39–17.61) | 20/20; 420/420 |
| restricted-mixed | 6 | balanced-one-stop | 12.10 (9.30–14.90) | 20/20; 420/420 |
| restricted-mixed | 6 | early-two-stop | 12.10 (9.30–14.90) | 20/20; 420/420 |
| restricted-mixed | 6 | conserve-one-stop | 12.85 (10.15–15.55) | 20/20; 420/420 |
| open-equal | 40 | balanced-one-stop | 6.75 (4.51–8.99) | 20/20; 420/420 |
| open-equal | 40 | early-two-stop | 16.60 (15.47–17.73) | 20/20; 420/420 |
| open-equal | 40 | conserve-one-stop | 12.75 (9.98–15.52) | 20/20; 420/420 |
| restricted-equal | 40 | balanced-one-stop | 10.30 (7.77–12.83) | 20/20; 420/420 |
| restricted-equal | 40 | early-two-stop | 19.70 (19.07–20.33) | 20/20; 420/420 |
| restricted-equal | 40 | conserve-one-stop | 10.55 (7.58–13.52) | 20/20; 420/420 |
| open-mixed | 40 | balanced-one-stop | 11.20 (8.15–14.25) | 20/20; 420/420 |
| open-mixed | 40 | early-two-stop | 15.65 (14.02–17.28) | 20/20; 420/420 |
| open-mixed | 40 | conserve-one-stop | 14.60 (11.63–17.57) | 20/20; 420/420 |
| restricted-mixed | 40 | balanced-one-stop | 10.20 (7.38–13.02) | 20/20; 420/420 |
| restricted-mixed | 40 | early-two-stop | 17.30 (16.09–18.51) | 20/20; 420/420 |
| restricted-mixed | 40 | conserve-one-stop | 13.65 (10.87–16.43) | 20/20; 420/420 |

All final representative entries480/480 and AI entries10080/10080 are eligible. The legal balanced strategy can beat much of the AI field over40 laps (mean6.75 open-equal,10.30 restricted-equal), while an extra-stop strategy does substantially worse (16.60/19.70). Mixed balanced means11.20/10.20 and conserved means14.60/13.65 show context dependence. This is compatible with a bounded midfield opponent in these synthetic cases, but does not establish competitiveness against engaged humans. Short-race representative means12.10–15.00 caution that mandatory-stop timing and legal battle rules can favour AI against these simple scripts. A perfect solver or guaranteed finishing position is unsupported.

The initial hypothesis that a halfway mandatory stop is safe for every short race failed and was corrected. Tight midfield statistical acceptance thresholds are deliberately absent. Condition thresholds (.45 conserve/.38 stop/.55 battle or end-race push), two-window eligibility lead time and compound preferences are policy hypotheses, not final numerical balance. Actual family play, decision clarity, mixed assistance and hosted cost/latency remain later milestone evidence.
