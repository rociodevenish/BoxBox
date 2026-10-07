# Dry-race strategy evidence

Numerical coefficients are initial hypotheses.

Seeds: 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26. Differences are alternative minus baseline.

## pace-short

Eligible position difference: Distribution { Count = 0, Mean = , StandardDeviation = , Minimum = , Maximum =  }.

Eligible equal-distance time difference: Distribution { Count = 0, Mean = , StandardDeviation = , Minimum = , Maximum =  }.

Diagnostic position difference: Distribution { Count = 20, Mean = -5, StandardDeviation = 0, Minimum = -5, Maximum = -5 }.

- Eligibility False/False; laps 5/5; position Distribution { Count = 20, Mean = -5, StandardDeviation = 0, Minimum = -5, Maximum = -5 }; time Distribution { Count = 20, Mean = -4.67703124999997, StandardDeviation = 0, Minimum = -4.67703124999997, Maximum = -4.67703124999997 }

## pace-long

Eligible position difference: Distribution { Count = 0, Mean = , StandardDeviation = , Minimum = , Maximum =  }.

Eligible equal-distance time difference: Distribution { Count = 0, Mean = , StandardDeviation = , Minimum = , Maximum =  }.

Diagnostic position difference: Distribution { Count = 20, Mean = 16, StandardDeviation = 0, Minimum = 16, Maximum = 16 }.

- Eligibility False/False; laps 40/38; position Distribution { Count = 20, Mean = 16, StandardDeviation = 0, Minimum = 16, Maximum = 16 }; time Distribution { Count = 0, Mean = , StandardDeviation = , Minimum = , Maximum =  }

Pace controls deliberately omit stops and are ineligible; they isolate the tyre trade-off and are diagnostic evidence.

## fresh-tyres

Eligible position difference: Distribution { Count = 0, Mean = , StandardDeviation = , Minimum = , Maximum =  }.

Eligible equal-distance time difference: Distribution { Count = 0, Mean = , StandardDeviation = , Minimum = , Maximum =  }.

Diagnostic position difference: Distribution { Count = 20, Mean = -5, StandardDeviation = 0, Minimum = -5, Maximum = -5 }.

- Eligibility False/True; laps 40/40; position Distribution { Count = 20, Mean = -5, StandardDeviation = 0, Minimum = -5, Maximum = -5 }; time Distribution { Count = 20, Mean = -65.11274850156829, StandardDeviation = 0.1623967477346244, Minimum = -65.41699554027946, Maximum = -64.90052682490659 }

Seed 7: stop at end of lap 20, Pit Loss 23.175s; first subsequent matching lap crossing to repay the loss: 29. This is evidence from a paired control, not an inference from finishing position alone.

Fresh-tyre control compares a legal stop against an ineligible no-stop control. Raw equal-distance times establish whether tyre gains repay the stop.

## pit-window-equal-fast

Eligible position difference: Distribution { Count = 20, Mean = -1.5, StandardDeviation = 2.328315406378386, Minimum = -5, Maximum = 3 }.

Eligible equal-distance time difference: Distribution { Count = 20, Mean = 2.058793519243659, StandardDeviation = 0.45549848963449674, Minimum = 0.6447431052320098, Maximum = 2.7955885877913715 }.

Diagnostic position difference: Distribution { Count = 20, Mean = -1.5, StandardDeviation = 2.328315406378386, Minimum = -5, Maximum = 3 }.

- Eligibility True/True; laps 40/40; position Distribution { Count = 20, Mean = -1.5, StandardDeviation = 2.328315406378386, Minimum = -5, Maximum = 3 }; time Distribution { Count = 20, Mean = 2.058793519243659, StandardDeviation = 0.45549848963449674, Minimum = 0.6447431052320098, Maximum = 2.7955885877913715 }

## pit-window-equal-technical

Eligible position difference: Distribution { Count = 20, Mean = -4.4, StandardDeviation = 1.0462967275611939, Minimum = -7, Maximum = -2 }.

Eligible equal-distance time difference: Distribution { Count = 20, Mean = 1.9128086714318215, StandardDeviation = 0.4839776895458006, Minimum = 0.4239867373762536, Maximum = 2.6098855940149406 }.

Diagnostic position difference: Distribution { Count = 20, Mean = -4.4, StandardDeviation = 1.0462967275611939, Minimum = -7, Maximum = -2 }.

- Eligibility True/True; laps 40/40; position Distribution { Count = 20, Mean = -4.4, StandardDeviation = 1.0462967275611939, Minimum = -7, Maximum = -2 }; time Distribution { Count = 20, Mean = 1.9128086714318215, StandardDeviation = 0.4839776895458006, Minimum = 0.4239867373762536, Maximum = 2.6098855940149406 }

## pit-window-mixed-fast

Eligible position difference: Distribution { Count = 20, Mean = 0.2, StandardDeviation = 0.5231483637805969, Minimum = 0, Maximum = 2 }.

Eligible equal-distance time difference: Distribution { Count = 20, Mean = 3.6415749826397814, StandardDeviation = 1.3135003622642725, Minimum = 2.1776067671003148, Maximum = 6.5430963169528695 }.

Diagnostic position difference: Distribution { Count = 20, Mean = 0.2, StandardDeviation = 0.5231483637805969, Minimum = 0, Maximum = 2 }.

- Eligibility True/True; laps 40/40; position Distribution { Count = 20, Mean = 0.2, StandardDeviation = 0.5231483637805969, Minimum = 0, Maximum = 2 }; time Distribution { Count = 20, Mean = 3.6415749826397814, StandardDeviation = 1.3135003622642725, Minimum = 2.1776067671003148, Maximum = 6.5430963169528695 }

## pit-window-mixed-technical

Eligible position difference: Distribution { Count = 20, Mean = -2.45, StandardDeviation = 1.3945382182304158, Minimum = -5, Maximum = 0 }.

Eligible equal-distance time difference: Distribution { Count = 20, Mean = 2.0157383516996106, StandardDeviation = 0.9468126961870719, Minimum = 0.14072712519009656, Maximum = 3.8974276516869395 }.

Diagnostic position difference: Distribution { Count = 20, Mean = -2.45, StandardDeviation = 1.3945382182304158, Minimum = -5, Maximum = 0 }.

- Eligibility True/True; laps 40/40; position Distribution { Count = 20, Mean = -2.45, StandardDeviation = 1.3945382182304158, Minimum = -5, Maximum = 0 }; time Distribution { Count = 20, Mean = 2.0157383516996106, StandardDeviation = 0.9468126961870719, Minimum = 0.14072712519009656, Maximum = 3.8974276516869395 }

Warm local simulation-only benchmark: 80 phases across 10 mixed-field technical-track races; mean 5.832ms, p95 6.449ms, maximum 14.396ms. No report I/O is included in these phase timings.

Simulation plus report writing: 294 races in 12.913s. This is a local workload measurement, not a server capacity guarantee.

## Interpretation limits

The model uses weighted whole-lap car performance and linear travel between events. It has no individual corners, sectors or location-specific passing zones. Catch events, crossings and pit exits recompute trajectories. Blue flags and battle wear are simplified. Weather, incidents, reliability and the season economy are excluded. These results establish model behaviour, not final balance or family playtest enjoyment.

Position and elapsed-time advantages can disagree: the early-stop strategy can improve its position while taking longer itself, because changed traffic also changes opponents' times. Opponent scripts are fixed; their physical outcomes are not. Evaluate both metrics rather than calling every position gain a time gain.

## Next modelling questions

Check whether sector-specific pace and passing zones change strategy conclusions; investigate tyre curves, passing frequency, correlated driver performance and pit-window sensitivity. Measure simulation-only latency and report memory separately before choosing server budgets. Browser playtesting should assess decision clarity and the 20–25 minute race target.

