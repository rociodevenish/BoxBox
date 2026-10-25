# Standards review

Scope: de0340be6060578699d36884fa5ba724e3ba8dba...a10fcbc0241d2fb637ade87a0bb55ceb7c9c7c8a. Read-only review; no checks or services launched.

## Documented breaches

None identified. Reviewed AGENTS.md, docs/agents/{issue-tracker,triage-labels,domain,verification-workflow}.md, GLOSSARY.md and the architecture/concept baseline. No shared ADR directory exists. The added campaign store retains an application persistence abstraction, scoring remains server-owned, and the simulation dependency boundary is preserved. Full-calendar differences are explicitly documented as provisional short-fixture scope rather than an undisclosed baseline change.

## Possible heuristic smells

**[P3] Duplicated Code — centralize the canonical result-to-awards projection.** `src/backend/Game.Application/CampaignResultIntegrity.cs:14,23` duplicates the entrant ordering and scoring-input construction from `CampaignCoordinator.Results.cs:5,35`:

```csharp
var entrants=record.Teams.SelectMany(t=>t.Drivers.Select(d=>(t.TeamId,d.PersonId))).Select((d,i)=>new ChampionshipEntrant(d.PersonId,d.TeamId,i)).ToArray();
var awards=ChampionshipScoring.Score(points,entrants,result.Classification.Select(r=>new ChampionshipFinish(r.PersonId,r.Position,r.Eligible,r.Status=="retired")).ToArray());
```

These are the write and recovery paths for the same persisted awards. A future adjustment to entrant order or retirement mapping must update both together; divergence can make newly committed records fail recovery validation. Extract their pure canonical mapping into one application helper used by both paths. Keep independent integrity assertions and independently authored expected-value tests. This is an optional Fowler duplication heuristic, not a documented repository breach or a demonstrated current scoring bug.
