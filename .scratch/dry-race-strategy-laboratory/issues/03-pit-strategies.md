# 03: Execute pit strategies and enforce compound eligibility

**What to build:** A designer can script pit and pace decisions, run complete races with tyre changes and inspect stop costs and compound eligibility.

**Blocked by:** 02 — Make pace and tyre choices affect stint performance.

**Status:** resolved

- [x] Accept scheduled phase decisions for each driver, including Pit with compound or Stay Out and Pace Instructions.
- [x] Deliberately unspecified pace and pit choices default to Standard and Stay Out; malformed scripts or decisions fail clearly.
- [x] Decisions are evaluated and locked together at a phase boundary before resolution, with no access to opponents' current locked choices.
- [x] A pit choice takes effect at that driver's next lap-end after the boundary, even for drivers on different laps or at different progress. Existing tyres apply until pit entry.
- [x] Apply track-specific pit-lane loss and small seeded stationary-stop variation, with equal pit-crew quality.
- [x] Tyre changes supply fresh tyres and reset tyre age; the Pace Instruction continues after the stop.
- [x] Record pit entry, service, tyre change, re-entry and Pit Loss in readable and structured reports. Traffic effects arrive in ticket 04.
- [x] Results completing a race without two compounds are marked ineligible with raw diagnostics retained; no automatic corrective stop.
- [x] A controlled clear-air example demonstrates a well-timed stop repaying its cost.
- [x] Public-boundary tests cover scheduling, next-lap timing, stop variation replay, fresh tyres, defaults, input rejection and eligibility.
