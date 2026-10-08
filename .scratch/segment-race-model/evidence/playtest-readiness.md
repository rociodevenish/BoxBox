# Recommended browser playtest

Pacing decision update, 9 October 2026: the user retired the fixed 20–25 minute race target. The historical recommendation below informed the design; future playtests measure timing to identify friction and assess a pleasant experience, without a fixed race-duration acceptance target.

Proceed with a small strategy playtest using the existing four-segment layout and 40-lap, eight-phase laboratory. The evidence supports testing decision clarity and causal feedback; it does not establish final balance, multiplayer capacity or the intended 20–25 minute session duration. This recommendation introduces no browser implementation or new tyre tuning.

## Supported conclusions

- [Track/passing evidence](track-passing.md) distinguishes actual on-track passes from attempts, blue flags and pit-cycle position changes. With identical cars/drivers at difficulty 0.4, baseline means are only 0.95 overtakes/race on open and 0.50 on restricted; early-stop alternatives average 2.15 and 1.15. The equal-all layout difference is inconclusive across 20 seeds. Players should expect periods without passing, and a position gain must not be presented as an overtake.
- The mixed-car-fit fixtures expose local strengths, queues and more on-track passing (39.60 open versus 30.20 restricted baseline means). They vary both car strengths and base driver Pace, and deliberately amplify fit for an experiment; those counts are not a calibrated expectation for ordinary players.
- [Tyre/pit evidence](tyre-pit.md) shows that gains after a stop can remain trapped in traffic. Default fresh-stop diagnostic controls are slower on average in all four field/layout cases; their no-stop baselines are ineligible. Legal earlier stops can improve position while increasing the target's own elapsed time, because opponents' physical outcomes also change. Show both the stop rule and the cause of subsequent position changes.
- [Form/noise evidence](form-noise.md) separates persistent driver modifiers from independent lap noise using resolved diagnostics. Normal combined-profile strategy intervals cross zero; stronger modifiers alter queues and conclusions. This supports uncertainty in feedback, not a player-visible oracle. Form belongs to one driver/race, with no shared team factor or weekend carryover; its default remains zero.
- [Benchmark evidence](benchmark.md) provides an actual historical executable comparison and shows rising phase allocation, retained history and report cost at 4/8/16 segments. The coarse model is appropriate for an exploratory playtest. Full designer reports contain cumulative histories and must not become the ordinary phase-response payload by default. Transport payloads and concurrent sessions require their own measurements.

## What players should be able to understand

At each simultaneous Race Phase decision, make the queued next-lap-end Pit Instruction, estimated tyre condition, out-lap warm-up and current lap/time gaps clear. Explain a catch held in a queue, a passing-zone opportunity, an actual completed pass, a blue flag and a pit rejoin as different events. Offer Attack/Defend only at actual encounters. Feedback should explain the visible cause without exposing a future cached passing outcome, hidden form, raw random values or opponents' exact tyre condition.

Start with an equal-all control and then a clearly labelled mixed-car-fit scenario, using the same rules and coarse layout. Ask players to explain why a stop moved them in the classification, whether it helped their pace or only changed opponents' traffic, and when their queued stop will happen. Record confusion about tyre estimates, eligibility, warm-up and next passing opportunities. Avoid changing coefficients during the first clarity session; record balance complaints as hypotheses for the next experiment.

Measure real player session duration, including preparation, waiting for simultaneous decisions and result feedback. The simulation's virtual race clock does not predict wall-clock play time. Test the intended 20–25 minutes across the eight decision phases; record decision/wait time and abandonment rather than assuming a fixed countdown proves the target. Assess whether sparse equal-all passing still makes pace/pit choices understandable and engaging.

## Remaining modelling questions

The exhaustion sweep at default degradation reaches zero condition only at the last crossing, so its neutral result leaves exhausted-running sensitivity untested. A bounded follow-up can vary exhaustion in a workload that actually runs on exhausted tyres, without adopting a new curve family. Warm-up, traffic and variability can change encounter sets, so strategy effects need not be monotonic. The current coefficients and artificial fit fields remain hypotheses.

The laboratory omits weather, incidents, reliability, fuel/vehicle dynamics, shared latent team factors, across-weekend form and the wider season economy. The benchmark is a serial local Release workload, not a browser responsiveness or server-capacity test. Compact player event delivery, retention policy, simultaneous multiplayer waiting and actual enjoyment remain playtest/infrastructure work. Preserve replay metadata for diagnosis while keeping designer diagnostics outside team observations.
