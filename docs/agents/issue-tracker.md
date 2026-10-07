# Issue tracker: Local Markdown

- Specs: `.scratch/<feature-slug>/spec.md`.
- Tickets: `.scratch/<feature-slug>/issues/<NN>-<slug>.md`,
  numbered from 01 in dependency order, one file per ticket.
- Record triage state in a `Status:` line near the top.
- Record dependencies in a `Blocked by:` line.
- Append discussion under `## Comments`.
- Publishing means creating the appropriate local file.
- Fetching means reading the referenced local file.

## Wayfinding

- Map: `.scratch/<effort>/map.md`.
- Decision tickets: `.scratch/<effort>/issues/NN-<slug>.md`.
- Record ticket Type and Status.
- Claim before working; resolve by recording an Answer.
- Add each resolved decision and its link to the map.
- Work unclaimed tickets whose blockers are resolved.
