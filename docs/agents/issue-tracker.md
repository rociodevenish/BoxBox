# Issue tracker: GitHub issues

- Specs and implementation evidence stay in `.scratch/<feature-slug>/`.
- Each actionable ticket is a GitHub issue in `rociodevenish/BoxBox`, titled
  `[SXX NN] <short title>` and numbered in dependency order within its spec.
- Store ticket scope, acceptance criteria, blockers and status in the GitHub issue.
  Use the default triage labels in `triage-labels.md`; close an issue when its
  work and required acceptance are complete. An implementation complete but
  awaiting human or hosted acceptance stays open with `ready-for-human`.
- Discuss, claim and update ticket status on GitHub. Do not treat a local snapshot
  as canonical or maintain a separate local discussion thread.
- Preserve `.scratch/<feature-slug>/issues/<NN>-<slug>.md` as a read-only snapshot
  for repository history and offline reference. Add the canonical GitHub issue
  URL near its heading; make status and scope changes on GitHub first.
- `.scratch/GITHUB-ISSUES.md` indexes migrated issues. Each effort map links to
  canonical issues; specs, maps and evidence remain versioned in the repository.
- Publishing a ticket means creating its GitHub issue. Fetching means reading
  that issue and its linked repository spec/evidence.

## Wayfinding and decisions

- Effort map: `.scratch/<effort>/map.md`.
- Decision tickets use the same GitHub issue convention and record Type, Status,
  dependencies and Answer in the issue body.
- Claim before working; resolve by recording the Answer and linking evidence.
- Add each resolved decision and its GitHub issue link to the map.
- Work unclaimed issues whose blockers are resolved.
