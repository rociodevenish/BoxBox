# Incidents, reliability and Safety Car decisions

Integration branch: `codex/incidents-safety-cars`, based on committed S08 `285277d715c938b656b36c5adb0a99a8970b86d9`.

| Ticket | Status | Evidence |
| --- | --- | --- |
| [01 — Contextual incidents and reliability](https://github.com/rociodevenish/BoxBox/issues/26) | resolved | [Lab rules/evidence](evidence/01-local-incidents.md) |
| [02 — Exact phase interruption](https://github.com/rociodevenish/BoxBox/issues/27) | resolved | [Actual-time rules/evidence](evidence/02-actual-time-interruption.md) |
| [03 — Simultaneous Safety Car window](https://github.com/rociodevenish/BoxBox/issues/28) | resolved | [Window rules/evidence](evidence/03-safety-car-window.md) |
| [04 — Neutralised running and restart](https://github.com/rociodevenish/BoxBox/issues/29) | resolved | [Travel/restart rules and evidence](evidence/04-neutralised-running.md) |
| [05 — Opening and durable disruptions](https://github.com/rociodevenish/BoxBox/issues/30) | resolved | [Opening/recovery rules and evidence](evidence/05-opening-recovery.md) |
| [06 — Rule and strategy evidence](https://github.com/rociodevenish/BoxBox/issues/31) | ready-for-human | [904 frozen experiments, routes and acceptance limits](evidence/06-disruption-strategy.md) |

See [integration scope](evidence/integration.md) for verification and external acceptance limits.

The implemented live policy is explicitly opt-in for authored Monza/Monaco routes; other circuits remain off. See [final integration checks and independent reviews](evidence/final/README.md). Household timing, comprehension and hosted acceptance remain pending.

GitHub issues are canonical for ticket status and discussion; linked local issue files remain snapshots.
