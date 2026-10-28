# Qualifying and automatic race start decisions

Integration branch: `codex/qualifying-race-start`, based on committed S07 at `cd7c61f248cdc1e32f38beda5bd4b31dc738cc1a`.

| Ticket | Status | Dependency |
| --- | --- | --- |
| [01 — Q1 and preparation lock](https://github.com/rociodevenish/BoxBox/issues/41) | resolved — [Answer and evidence](evidence/01/README.md) | Atomic preparation/Q1 commit, complete private team locks, recorded full-field attempts and reviewed phone/public flow. |
| [02 — Q2, Q3 and final grid](https://github.com/rociodevenish/BoxBox/issues/42) | resolved - [Answer and evidence](evidence/02/README.md) | Durable eligible-only Q2/Q3 windows, fixed elimination groups, complete unique grid and reviewed three-session phone/public flow. |
| [03 — Policies and missed choices](https://github.com/rociodevenish/BoxBox/issues/43) | resolved — [Answer and evidence](evidence/03/README.md) | Frozen observation-only policies, private per-driver advice and preserved accepted locks at expiry. |
| [04 — Automatic opening](https://github.com/rociodevenish/BoxBox/issues/44) | ready-for-agent | 02 |
| [05 — Complete experience evidence](https://github.com/rociodevenish/BoxBox/issues/45) | resolved — local engineering; human/external acceptance pending | [400 frozen full-field replays, paired risk and current browser evidence](evidence/05/README.md) |

See [integration scope and verification](evidence/integration.md). Household timing, readability and actual hosted acceptance remain separate from local deterministic and rendered checks.

All five local engineering tickets are resolved. [Final verification, independent reviews and acceptance limits](evidence/final/README.md) pin integrated code at `2df420f5ba4e486395f3fe88b77c9ed968079dcc`. New openings use v2 Standard/Neutral; saved v1 openings retain explicit replay support. The spec is ready for human acceptance.

GitHub issues are canonical for ticket status and discussion; linked local issue files remain snapshots.
