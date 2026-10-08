# 05: Record replay and browser playtest evidence

**What to build:** A designer can reproduce browser race outcomes and assess whether two-car decisions, feedback and race pacing work for players.

**Blocked by:** 04 — Present events and continue through a complete race; segment-model evidence review and integration readiness; visual foundation 06 — Verify and document the integrated foundation.

**Status:** resolved

- [x] Integrate reviewed segmented presets with equal/mixed 22-car fields on contrasting tracks; record readiness conclusions and model limitations.
- [x] Designer-only replay records retain seed, configuration, model/randomness and opponent-strategy versions plus all accepted human/opponent decisions, separately from player payloads.
- [x] Replay recorded decisions through the existing engine and verify authoritative phase/final outcomes match the browser session.
- [x] Record decision, server resolution, presentation and total race durations separately; publish a reproducible playtest protocol and local multi-device run instructions.
- [x] Complete short smoke and full-distance checks on contrasting tracks, including pending pits, legal battles, lapped finishes, eligibility and refresh/reconnect.
- [x] Record actual player findings about comprehension, event explanations, both-car workload and pacing, with representative phone/display visual review.
- [x] Record the user's 2026-10-09 retirement of the fixed duration target; timing informs pleasant pacing without a required duration. Automated clicks or server benchmarks do not establish enjoyment or player workload.
- [x] Run relevant API/SignalR, browser, frontend build/type and existing simulation regression checks without duplicating private engine formulas.

## Completion evidence

Implemented separate local designer credentials, versioned complete accepted-plan export and exact engine replay, monotonic server metrics and write-only public-display telemetry. Applied actual player feedback to inward select carets, Gap/Interval naming and authorized return navigation. The user retired the fixed duration target on 2026-10-09.

See [findings and verification](../evidence/05-playtest.md), [machine summary](../evidence/05-playtest.json) and [protocol](../../../docs/browser-playtest.md). Five 50-lap browser scenarios verify exact replay, legal battles, lapped distance and eligible/ineligible finishes; short and recovery checks remain green. Owned queued/in-pit states were not observed at these five-lap boundaries; engine and gallery coverage is explicitly separate. Physical-device readability and full-race human workload remain unmeasured, with next player checks recorded.

Verification: 129 backend tests, 34 gallery tests, 19 browser tests; six affected telemetry/full-distance cases repeated after restricting reports to visible public displays. Typecheck/build and existing runner smoke passed; initial Angular bundle 284.29 kB raw / 73.10 kB estimated transfer. Exported browser replay CLI matches all 10 phases. Original user assets and frozen preview preserved.
