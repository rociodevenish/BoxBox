# 02: Join a game from the home page

Canonical issue: https://github.com/rociodevenish/BoxBox/issues/114

Snapshot status: closed (completed).

Read-only snapshot; scope/status updates belong on GitHub.

## Spec

Repository spec: .scratch/player-home/spec.md on codex/player-home.

## What to build

Join a game opens the existing household joining flow, with a route back home and actionable invalid-code feedback. Preserve direct invitations and verify a visitor can join without creating a campaign.

## Acceptance criteria

- [ ] Deliver the described player-visible journey and preserve existing game authority.
- [ ] Demonstrate behavior through agreed browser/API seams with focused regression checks.
- [ ] Record verification and relevant visual evidence.

## Blocked by

#113 — Welcome players and start a campaign

## Implementation

Claimed for implementation on the player-home integration branch.

## User clarification

Signed-out home prioritises Quick join with a code and a sign-in prompt. Signed-in home exposes Create campaign and Join another game; account-aware states must clear on sign-out. Preserve an entered code into the established guest-join flow, with actionable invalid-code feedback and no implicit host/private-control grants.
