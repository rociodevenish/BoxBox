# 03: Discover and resume campaigns from home

Canonical issue: https://github.com/rociodevenish/BoxBox/issues/115

Read-only snapshot; scope/status updates belong on GitHub.

## Spec

Repository spec: .scratch/player-home/spec.md on codex/player-home.

## What to build

Authenticated players discover their campaigns through server membership, including another browser and completed campaigns. Resume uses existing device-return authority. Cover loading, empty, retry, sign-out clearing, account isolation, private-summary exclusion, and both volatile and durable stores.

## Acceptance criteria

- [ ] Deliver the described player-visible journey and preserve existing game authority.
- [ ] Demonstrate behavior through agreed browser/API seams with focused regression checks.
- [ ] Record verification and relevant visual evidence.

## Blocked by

#113 — Welcome players and start a campaign

## Implementation

Claimed for implementation on the player-home integration branch.
