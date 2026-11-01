# Player home and game entry

Status: resolved

Implementation and verification: [completion evidence](evidence/completion.md).

## Problem Statement

Visitors opening the site's root currently see a Driver gallery containing illustrative development fixtures. It does not explain how to play, create a campaign, join a household game or return to an existing campaign. Campaign creation is available through a direct URL, but opening that page for local testing does not make it the site's entry point.

## Solution

Make the root URL a player-facing home page with distinct signed-in and signed-out experiences. Signed-in players can create a campaign, reopen campaigns they host, reenter campaigns they belong to and join another person's game. Signed-out visitors can Quick join using a code or follow a sign-in prompt. Reuse the existing account, household, campaign and device-return flows. Move the development gallery to an explicit development route while preserving its fixture views.

The home page describes the currently available game honestly: players manage a constructor and both drivers, prepare for events and make simultaneous strategy decisions at Race Phase boundaries. It must not promise a full calendar or features that the selected campaign does not support.

## User Stories

1. As a first-time visitor, I want the root URL to explain the game, so that I understand what I will be doing.
2. As a first-time visitor, I want clear entry actions, so that I can choose between hosting and joining.
3. As a prospective host, I want Create campaign to open campaign setup, so that I can begin playing.
4. As a signed-out host, I want account creation or sign-in to retain my intended destination, so that I can continue to campaign setup afterward.
5. As a signed-in host, I want to reach campaign setup without signing in again, so that starting a game is straightforward.
6. As an invited player, I want Join a game to use the existing joining flow, so that I can enter a household game using its code or link.
7. As an invited player, I want invalid or unavailable codes to produce understandable feedback, so that I can correct a mistake.
8. As a player following an invitation link, I want the existing direct joining flow to keep working, so that visiting the home page is optional.
9. As a returning player, I want my campaigns listed after sign-in, so that I do not need to save individual campaign URLs.
10. As a returning player on another browser, I want campaign discovery to use my authenticated membership, so that it does not depend on local browser history.
11. As a returning player, I want enough campaign context to distinguish games, so that I resume the intended campaign.
12. As a returning player, I want Resume to use the established device-return flow, so that current permissions and boundary-safe control rules remain effective.
13. As a player without campaigns, I want an empty state with create and join actions, so that I know what to do next.
14. As a player waiting for account or campaign data, I want a clear loading state, so that a pending request does not look like an empty account.
15. As a player affected by a temporary failure, I want an actionable retry state, so that I can recover without accidentally creating another campaign.
16. As a player who signs out, I want account-specific campaign information cleared, so that it is not shown to the next user of the browser.
17. As a household participant, I want private team choices and device grants excluded from home-page listings, so that discovery preserves the game's privacy arrangements.
18. As a player whose access changes, I want current server authority checked when listing and resuming, so that stale browser data cannot restore access.
19. As a player viewing a completed campaign, I want to reach its existing results, so that completion does not make the campaign disappear.
20. As a keyboard or screen-reader user, I want labelled actions, sensible headings and understandable status messages, so that I can enter the game accessibly.
21. As a mobile player, I want readable explanations and usable entry actions, so that I can start or join on a small screen.
22. As a developer, I want the Driver gallery and its public, controller and results fixtures available through a development route, so that moving the home page preserves design review tools.
23. As a player using a saved game link, I want existing campaign, preparation, qualifying, controller and display URLs to remain valid, so that the home-page change does not interrupt play.
24. As a signed-out visitor, I want Quick join by code without first creating a permanent account, so that I can use the existing guest-join flow immediately.
25. As a signed-out visitor, I want a clear sign-in prompt, so that I can access hosting and my existing campaigns.
26. As a signed-in host, I want my hosted campaigns distinguished from campaigns I joined, so that I can reopen the appropriate game.
27. As a signed-in participant, I want a separate way to reenter campaigns I already belong to and join a new game, so that these actions are unambiguous.

## Implementation Decisions

- The root URL becomes the player home. Campaign creation remains a separate destination rather than an automatic root redirect.
- Signed-in home provides Create campaign, hosted campaign reopening, member campaign reentry and Join another game. Distinguish hosted and joined groups using authenticated server-owned host identity; include no participant UID in public summaries.
- Signed-out home prioritises Quick join with a code and a sign-in prompt. Reuse the existing guest joining flow and account options; Quick join must not grant host or private team authority merely from knowing a code. Preserve the code when transitioning into joining.
- Reuse the existing visual system and account interface. Preserve the intended create or join destination across authentication without allowing arbitrary external redirects.
- Move the fixture gallery to `/dev/gallery`, including its public, controller and results variants. Update fixture navigation and affected browser checks together. Legacy root query links must resolve deliberately to their corresponding development view rather than accidentally showing the home page.
- Preserve existing game deep links and invitation behavior. Home navigation does not create a campaign, join a game or bind a device by itself.
- Add authenticated campaign discovery to the campaign application/store boundary; the inspected API currently supports campaign lookup by identifier but does not expose a membership listing. Browser-local remembered identifiers are not sufficient for cross-browser discovery.
- Return only campaigns belonging to the authenticated player and the minimum non-private summary needed to choose one: stable campaign identity, available display context, lifecycle and current event or completed state. Do not expose private setup, strategy, credentials or device grants.
- Keep membership discovery consistent with campaign creation and joining, and support the existing volatile and durable storage modes. Define an efficient membership lookup within the existing storage abstraction during implementation; do not enumerate and disclose unrelated campaigns to clients.
- Resume delegates to the existing authenticated return/recovery flow. Listing a campaign does not grant control, replace an authorised device, change accepted locks or bypass Race Phase boundary rules.
- Render distinct signed-out, loading, empty, populated and retryable-error states. Discard stale responses after an account change and keep private responses out of shared caches.
- Completed campaigns remain discoverable for existing results access. No new archiving or deletion model is introduced.
- No standalone prefactoring ticket is required by current evidence. Keep any routing extraction narrow and part of the demoable home-page slice.

## Testing Decisions

- Primary seam: Playwright browser journeys using the real frontend, API and local authentication emulator, following existing household and campaign recovery tests. The user requested implementation after this seam was proposed.
- Test observable behavior rather than component state, signal names, routing flags or internal storage layout.
- Cover root arrival, create with and without authentication, destination retention, joining and invalid-code recovery, and preserved invitation/deep-link behavior.
- Exercise campaign discovery and resume with two authenticated users and a fresh browser context. Verify isolation, empty and completed states, sign-out clearing and current-authority enforcement through the existing return path.
- Use focused real-token API/store integration checks for listing authorization, summary privacy and supported storage behavior where browser coverage alone cannot sharply establish the boundary. Reuse established test hosts and emulator setup.
- Verify gallery variants after relocation and update existing visual-test navigation without replacing player-entry coverage with fixture-only tests.
- Check keyboard operation and representative narrow and desktop viewports. Assert meaningful states and destinations rather than brittle implementation details.

## Out of Scope

- Implementing a full racing calendar, changing available campaign content or adding simulation features.
- Redesigning campaign setup, personnel acquisition, preparation, qualifying or Race Phase controllers.
- New account providers, production hosting configuration or a new authentication system.
- Changing household privacy modes, ownership, takeover/reclaim rules or device-return authority.
- Adding campaign deletion, archiving, search, invitations management or automatic resume.
- Turning the solo laboratory or development fixtures into primary player onboarding.
- Broad routing, styling or storage rewrites unrelated to the entry journeys.

## Further Notes

This spec wraps the proposed three slices: (1) welcome and create, including gallery relocation; (2) join from home, blocked by slice 1; (3) discover and resume campaigns, blocked by slice 1. Slices 2 and 3 do not block each other. Each slice includes its own behavior-level verification.

The user requested implementation and explicitly approved publication of the three slices. Canonical GitHub tickets are [S20 01](https://github.com/rociodevenish/BoxBox/issues/113), [S20 02](https://github.com/rociodevenish/BoxBox/issues/114) and [S20 03](https://github.com/rociodevenish/BoxBox/issues/115). Repository tracker instructions make GitHub issues canonical for actionable tickets; specs and read-only issue snapshots remain versioned locally.

Local volatile storage still loses campaigns when the backend restarts. Campaign discovery must reflect available authoritative records rather than imply that the home page adds durability.
