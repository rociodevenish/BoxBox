# S19 — Verification workflow and compact controller refinement

Status: resolved

Implement the user's approved retrospective improvements and the separately requested compact controller presentation. Stop after this bounded effort; shared-session ticket 02 remains unstarted.

## Requirements

- Bound process inspection and verification commands; use recorded process identities instead of broad Windows process enumeration. Provide reusable tooling with timeout and failure reporting.
- Reduce orchestration chatter through a concise workflow reference: scope pointers, milestone/blocker messages, bounded unattended waits, and focused reruns after a single broad verification pass. Preserve required independent reviews.
- Ordinary browser tests own isolated servers and write captures to ignored output. Historical tracked evidence changes only through an explicit publish action. Test execution must leave tracked files untouched.
- Inspect representative phone and public-display layouts early, before broad regression runs. Record actual checks once instead of repeatedly reconstructing them.
- Provide one reproducible verification command and CI using existing typecheck/build/backend/browser checks. Local and CI authentication-emulator setup must be explicit and portable, without cloud credentials.
- Controller driver cards show the name and portrait, with a button to open stats, preferences and details using existing accessible dialogs. Preserve full cards elsewhere.
- Nearby Classification uses the same public TimingTower/TimingRow components, filtering to the controlled driver plus at most two positions either side. Retain intervals, tyre/age, stops, fastest marker, stop status and driver-detail drill-down. Avoid a separate visually divergent table.
- Preserve authoritative race state, legal choices, playback sequence, privacy, refresh/retry, and fixture physics. Keep the existing frozen previews untouched; publish a new verified preview.

## Tickets

1. [Verification and agent workflow](issues/01-verification-workflow.md)
2. [Compact driver identity and reused nearby classification](issues/02-compact-controller.md)

These tickets are independent. Review their integrated result, fix findings, record evidence and stop.

## Completion

Both tickets are resolved and integrated on `codex/shared-sessions` through `b8f4770`. Independent Standards and Spec reviews each have zero remaining findings. [Integrated verification and preview](evidence/integration.md) records completed checks and limitations. Stop here; shared-session ticket 02 remains unstarted.
