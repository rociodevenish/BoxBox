# Race Package library and development outcomes

Status: ready-for-agent
Blocked by: season economy and permanent development; effective weekend inputs and qualifying locks.

## Problem Statement

Permanent refinements alone cannot express circuit-specific investment, a flexible package library or the risk of rushing work for an upcoming Grand Prix. Players need alternatives that use the same scarce budget and development opportunity without stacking every package at once.

## Solution

Extend development with Race Package creation and refinement, richer project outcomes and Rush. Teams maintain a library and select exactly one package per Grand Prix before qualifying. Compare permanent gains, specialised packages, refinement and saving through complete campaigns, using the existing ledger and one development decision per event.

## User Stories

1. As a player, I want to create a specialised package, so that I can invest for particular circuits.
2. As a player, I want a package library, so that earlier work remains useful.
3. As a player, I want exactly one selected package, so that packages do not stack into universal gains.
4. As a player, I want strengths and compromises visible, so that selection is understandable.
5. As a player, I want to refine an existing package, so that improvement can compete with building anew.
6. As a player, I want package work to consume my development decision, so that repeated logins do not grant extra actions.
7. As a player, I want permanent refinement and Save still available, so that packages are alternatives rather than mandatory spending.
8. As a player, I want normal, partial, breakthrough, compromised and delayed outcomes explained, so that risk is not a hidden binary roll.
9. As a player, I want compromised work usually usable, so that an imperfect outcome retains a choice.
10. As a player, I want Rush with explicit cost and risk, so that reaching the next event is a deliberate trade-off.
11. As a player, I want pending completion dates clear, so that I cannot select an unavailable package.
12. As a player, I want free package selection between events, so that selection does not consume development.
13. As a player, I want package and setup locked at qualifying, so that race performance is stable.
14. As a player, I want engineer specialisation reflected in supported outcomes, so that personnel have meaningful value.
15. As a player, I want quoted costs and remaining credits, so that Rush cannot silently overspend.
16. As a player, I want restart to preserve project outcomes, so that risk cannot be rerolled.
17. As an Assisted player, I want uncertain trade-offs explained, so that advice does not reveal future draws.
18. As a host, I want AI subject to the same library, costs and action count, so that its development is fair.
19. As a designer, I want campaign evidence for package philosophies, so that a large library is not assumed always best.
20. As a player, I want readable package cards and previews, so that the library fits the established design system.

## Implementation Decisions

- Extend public development rules and the campaign coordinator; reuse the existing integer-credit ledger, opportunity identity and durable project records. Package creation/refinement and permanent work are mutually exclusive choices within the same opportunity.
- Represent a Race Package as a stable identity with immutable versioned positive modifiers, compromises, applicability information and refinement history. It is a coordinated specification, not a stack of separately equipped parts.
- Supply bounded low-drag, downforce, tyre-management, cooling/reliability and mechanical-grip templates using implemented attributes. Validate cost floors, attribute limits and diminishing returns; treat all values as tuning hypotheses.
- Provide a neutral baseline package from campaign creation so every team can select exactly one even before purchasing specialised work. Migrate existing preset context explicitly; unavailable packages never become silent zero modifiers.
- Refine strength, reduce a compromise or broaden usefulness through a stated bounded option. Refinement creates a new version rather than mutating a package already frozen into a weekend.
- Generalise the saved development outcome contract to normal success, partial success, breakthrough, compromised result and delay. State the effect/range of each outcome in legitimate previews; hidden draws remain diagnostic-only and use stable project/channel/version identities.
- Rush is selected with project commitment and atomically includes its extra cost and changed outcome distribution. It aims for earlier completion but does not guarantee success; supported compromises may reduce performance or Reliability. Do not add an independent risk draw that duplicates an existing project hazard.
- Persist quoted costs, personnel inputs, timing, policy/version and concrete outcome once. Reuse the existing no-repayment delayed completion rule and document any new timing differences explicitly. Version changes cannot reinterpret a committed project.
- Resolve projects in stable order at preparation close. Selection may be drafted in advance but must reference a completed package version at final lock; late work cannot change a qualifying/race snapshot. Present pending work honestly and require a valid fallback selection.
- Effective inputs compose Base Car, completed Permanent Development, exactly one selected Race Package and Weekend Setup once. Preserve all source records for replay and never apply library entries that are not selected.
- Technical Engineer specialisations and Manager Development/Efficiency have bounded documented effects on relevant costs/outcome quality; personnel information benefits do not become unexplained raw speed. Label inactive staff attributes rather than inventing consumers.
- AI/assistance use legitimate library, budget, calendar and project observations with no access to outcome draws or competitor plans. Selection and project choices are recorded separately; private channels honour household modes.
- Extend Angular/Tailwind with package cards, library comparison, refinement/Rush choices, pending outcomes and qualifying-lock summaries. Inspect supplied design assets at implementation, use data-driven identity/provenance and accessible fallbacks.
- Publish paired-seed short/full-calendar comparisons of base-car investment, small/large libraries, refinement and Rush. Record spending, outcomes, failed hypotheses and runaway-risk evidence rather than claiming a single optimal philosophy.

## Testing Decisions

- Reuse the user-approved public API/SignalR, public development/effective-input operations and focused browser boundaries, plus persistence emulators.
- Verify single-opportunity enforcement, atomic Rush debit, exact-one selection, refinement versioning, pending rejection, outcome replay and unchanged active-weekend inputs through observable state.
- Reuse ledger, delayed project, random identity, qualifying lock and race replay tests. Restart around project/outcome/selection commits; retries never resample or charge twice.
- Test all outcome categories with deterministic public scenario inputs, including usable compromised packages and a delay past the intended event. No private probability-formula assertions.
- Browser flows cover creation, refinement, Rush, uncertainty, selection and recovery. Campaign evidence evaluates opportunity costs separately from unit correctness.

## Out of Scope

- Unlimited development, independently stacked component packages, trading/resale, research trees and real engineering simulation.
- New race mechanics, final prices/ratings and complete art production.

## Further Notes

This completes the planned development alternatives. The next content and release specs finalise coverage and measured tuning rather than introducing a second economy. Testing boundaries remain those approved for campaign/economy work.
