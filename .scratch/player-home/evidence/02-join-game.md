# Slice 2: Quick join and account-aware entry

Implements https://github.com/rociodevenish/BoxBox/issues/114.

Signed-out home prioritises a labelled code field and Quick join, followed by Sign in returning to `/`. The code is URL-encoded into the existing `/shared/join?code=...` guest flow. Signed-in home provides Create campaign, Join another game and Sign out. Anonymous guest identities remain in the signed-out home experience. Home owns a live auth subscription and disposes it on destruction, so account-specific UI clears on sign-out. The resume slice can consume the permanent-user signal without initializing authentication again.

The established lobby flow retains actionable invalid-code feedback and editable code. A Back to home link is available before and after joining. Direct invitation links remain usable without visiting home. Home entry does not create campaigns, join lobbies or grant private control.

## Verification

- Real red: `.tools/reports/join-red.json` failed because root lacked the labelled join-code input (test runner PID43768).
- First green: `join-green.json`, invalid-code correction and back-home journey passed.
- Real browser/API/emulator journeys: `join-journeys.json`, 3/3 passed. Includes invalid-code recovery; keyboard Quick join; preserved code; guest membership with no host controls/private-controller links; direct invitation; account registration returning home; signed-in create; anonymous home; sign-out; prior create registration/sign-in regression. Runner PID23380.
- Home/gallery regression: `join-gallery.json`, 2/2 passed, including all legacy gallery view links. Auth API is absent in this fixture-only config; the home exposes its retry state while retaining entry actions.
- `join-typecheck.json`: TypeScript app check passed. Angular compilation also passed in both browser launches. Parent owns the integrated broader checks.
- Runtime: bundled Node24; .NET10.0.402; Java21; cached Chromium1234. Owned UI4484/API4584/Auth19909/Firestore19901/hub19910/logging19960. Config `.tools/join-emulators.json`; bounded records retain launched identities. Existing preview ports4411/4510/Auth19099 preserved.

## Visual inspection

Inspected a complete 360×800 phone journey through home, guest join, lobby and back home. Published `home-join-360.png` and `home-join-lobby-360.png`: labelled input and stacked actions remain reachable, no horizontal overflow; lobby QR and constructor cards remain contained. Inspected the preserved 1920×1080 public fixture (`player-home-public-1920.png`): all 22 cars and portrait/context remain contained. Published only these specifically reviewed captures using `tools/publish-evidence.mjs`.

## Resume integration contract

Import `CampaignResume` from `./campaign-resume`, add it to the home standalone imports, and render `<app-campaign-resume [user]="user()"/>` inside the signed-in branch after its navigation. The `user` signal is `User|null` and filters anonymous identities to null. This ticket deliberately compiles independently before the sibling resume component is merged.
