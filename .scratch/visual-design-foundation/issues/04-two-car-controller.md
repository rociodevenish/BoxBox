# 04: Build the two-car decision controller

> GitHub issue: [#94](https://github.com/rociodevenish/BoxBox/issues/94) — canonical ticket. This file is a local snapshot; track status and discussion on GitHub.

**What to build:** A player can choose instructions for both cars, review the complete team plan and understand its submitted or locked state in the gallery.

**Blocked by:** 02 — Build reusable driver cards and the executable gallery.

**Status:** resolved

- [x] Distinct driver areas provide Conserve, Standard and Push; Stay Out or Pit with compound; and contextual Neutral, Attack and Defend.
- [x] Short explanations describe instruction trade-offs; tyre condition is visibly an estimate in private fixtures.
- [x] Both cars' selected choices appear in a clear confirmation summary before a fixture-driven submission/lock transition.
- [x] Selected, submitted and locked states are distinct; loading, resolving, disabled and rejected-input states explain available actions and recovery.
- [x] Contextual battle visibility and pending-stop restrictions are fixture inputs rather than duplicated game rules.
- [x] Keyboard users can complete the flow with visible focus, and primary touch targets meet the agreed size and spacing rules.
- [x] Review 360px and 768px portrait/landscape layouts, long names and errors through rendered interactions and inspected screenshots.
- [x] Controller entry points do not import a race-rendering bundle; actual server validation and orchestration remain outside this ticket.

## Answer

Built the reusable private two-car controller and fixture entry, with retained drafts, both-car confirmation, distinct submitted/locked states, rejection recovery, fixture-supplied battle/pending-stop restrictions and labelled tyre estimates. Keyboard focus cycling was verified red then green. All 23 rendered frontend tests, production build and strict template/type checking pass. [Reviewed evidence](../evidence/two-car-controller-review.md) records actual portrait/landscape capture dimensions and limits. Server validation and orchestration remain outside this component.
