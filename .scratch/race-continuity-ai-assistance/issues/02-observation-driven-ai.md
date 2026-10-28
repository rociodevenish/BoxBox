# 02: Race against fair, observation-driven AI

> GitHub issue: [#47](https://github.com/rociodevenish/BoxBox/issues/47) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** Unclaimed teams make credible two-car decisions from the same legitimate observations and legal choices available to human players.

**Blocked by:** 01 — Persist locks and recover an interrupted race; reviewed race-model evidence.

**Status:** resolved

- [x] Replace scripted fill-ins with bounded rules-based choices considering estimated tyres, public distance/order/gaps, compound use, pending stops and contextual battles.
- [x] Exclude hidden condition/form, opponents' locked choices and future draws/outcomes; grant no performance, information or execution bonuses.
- [x] Freeze decisions from phase-start observations before human choices influence them and durably record policy version, source and concrete instructions.
- [x] Repeated requests/recovery reuse saved choices; use deterministic tie-breaking or explicitly version any separate policy randomness.
- [x] Verify legal executable choices and compound planning through public observations and committed outcomes; hidden diagnostic changes with identical legitimate observations cannot change policy output.
- [x] Run reproducible short/full-distance comparisons against representative strategies on contrasting fields/tracks using published seeds and uncertainty reports.
- [x] Record evidence about roughly midfield competitiveness and failed/inconclusive hypotheses without promising finishing positions or using tight statistical unit-test thresholds.

## Answer
Implemented bounded observation-only v2 choices, compatible frozen legacy replay, durable phase/source/policy/concrete records and executable compound/battle planning. Focused public checks and480 reproducible legal comparison races are recorded in [observation-ai evidence](../evidence/observation-ai.md). Strength conclusions remain descriptive and human competitiveness remains unproven.
