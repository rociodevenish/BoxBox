# Browser slice review and feedback fixes — 2026-10-09

## Standards

The independent final Standards review reported no actionable findings. The subsequent fixes retain the agreed API/SignalR and rendered-browser boundaries; production uses the existing engine, public/private allowlisted contracts and four server presets.

## Spec

The independent Spec review identified one P2: pending/ongoing stops had only separate engine/gallery coverage, while ticket 05 claimed integrated completion. This is resolved with actual HTTP, authorized SignalR and browser coverage through the production API and real RaceEngine. A narrow server-owned preset catalogue keeps production inputs unchanged; test dependency injection changes only Driver 02's initial rolling gap (600 seconds for queued, 350 for ongoing). These deliberately delayed fixtures reach an actual stop at the five-leader-lap boundary. They are not measured production-preset encounters, alternate physics or public configuration upload.

Both scenarios show the known compound and stop restriction, disable replacement controls, retain an editable first car, reject a manipulated complete replacement plan without any snapshot/revision/choice mutation, then accept a corrected plan. Unauthorized private subscriptions remain rejected. Ordinary five 50-lap preset runs still did not encounter an owned queued/ongoing stop at their decision boundaries; that earlier observation remains honest.

## Actual player feedback and accepted flow change

The user retested the ticket 05 preview and found that both cars appeared locked after resolution without a clear way to start the next phase. The user then explicitly requested merging phase submission and continuation to remove unnecessary interaction. The revised flow reviews both cars and offers **Run phase with both cars**. A valid accepted plan resolves once and automatically opens the next window with Standard / Stay Out / Neutral defaults. There is no separate Continue endpoint, command or button. Final finish remains terminal and restart stays explicit.

The accepted acknowledgement preserves its original resolved phase/revision. Cached retries do not advance twice, and delayed acknowledgements cannot relock newer editable defaults. New opponents freeze from their legitimate starting observation as before. The retained bounded public timeline identifies resolvedPhase independently of the next decision phase; its cursor/events survive automatic opening and refresh. Public playback, skip/replay and reduced motion do not hold the decision window closed or alter physics. A browser test keeps 10-second feedback playing while the next choices are editable, then refreshes the same events and completes the race.

This feedback establishes a concrete comprehension/friction finding. It does not measure full-race human workload, pleasantness or physical-device readability. The previously retired fixed duration target stays retired.

## Verification and evidence

- 132 backend tests: 110 simulation and 22 API/SignalR.
- 34 rendered foundation gallery tests; historical screenshot regeneration was discarded.
- 19 ordinary real-server browser tests, including five 50-lap runs and recovery faults; two additional queued/ongoing real-engine browser tests.
- Three affected complete-race/playback/recovery cases repeated after final copy changes.
- Angular typecheck/build passed; initial bundle 283.45 kB raw / 73.07 kB estimated transfer.
- Exported browser replay verified by the existing laboratory CLI: all 10 recorded phases match and finish. No coefficients, engine/randomness versions or physical outcomes changed.

[New machine summary](evidence/review-automatic-windows.json) records the five actual automatic-window runs, explicit configurations, accepted choices, eligibility/distance coverage and separate timing sources. Ready-wait is null because no manual continuation interval exists. Historical [ticket 05 measurements](evidence/05-playtest.json) retain their original explicit-Continue semantics. Automation duration is not human pacing or capacity evidence.

Inspected captures use a 360 × 800 phone viewport with full-page heights, and a true 1920 × 1080 final display viewport:

| Capture | Actual image dimensions | Review |
|---|---|---|
| [Next choices](evidence/review-automatic-window-controller-360.png) | 360 × 1853 | Phase 2 editable defaults and clear run instructions; no Continue action. |
| [Queued stop](evidence/review-queued-controller-360.png) | 360 × 2060 | Current Medium compound, queued restriction and preserved rejected Push draft. |
| [Ongoing stop](evidence/review-ongoing-controller-360.png) | 360 × 2060 | Current Hard compound, ongoing restriction and preserved rejected Push draft. |
| [Final display](evidence/review-finish-1920x1080.png) | 1920 × 1080 | All 22 rows, distances and eligibility fit; First across the line remains distinct from eligibility. |

Original assets, concurrent design work and both existing previews were preserved. Parent final review and preview publication follow this fix commit.
