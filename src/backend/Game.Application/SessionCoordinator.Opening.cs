using Game.Domain;
using Game.Simulation;
namespace Game.Application;

public sealed record OpeningCar(string EntryId,string Name,string Slug,string TeamId,int GridPosition,int Position,int PositionsGained);
public sealed record PublicOpening(string Version,string Boundary,IReadOnlyList<OpeningCar> Cars);

public sealed partial class SessionCoordinator
{
    private static bool IsOpeningPresentation(Session session)=>session.OpeningReplay is not null&&session.Presentation?.Cursor==session.ModuleState.GetValueOrDefault("opening-presentation")&&session.Lifecycle=="phase-result";
    private static PublicOpening? ProjectOpening(Session session)
    {
        if(session.OpeningReplay is not {} replay)return null;
        if(session.OpeningSummary is {} summary)return summary;
        var source=replay.Input.Weekends.SelectMany(w=>w.Cars).ToDictionary(c=>c.Source.EntryId);
        var grid=replay.Input.Grid.ToDictionary(g=>g.EntryId,g=>g.Position);
        // The opening is immutable. Later race windows must not silently rewrite its gains/losses.
        var opening=AutomaticRaceStart.Replay(replay).Opening;
        return session.OpeningSummary=new(replay.Input.ModelVersion,opening.Interruption is {} incident?$"Safety Car at {incident.Time:F1}s":"Leader lap 1 crossing",opening.Classification.Select(c=>new OpeningCar(c.DriverId,
            source[c.DriverId].Source.Driver.Name,source[c.DriverId].Source.Driver.Slug,source[c.DriverId].Source.TeamId,
            grid[c.DriverId],c.Position,grid[c.DriverId]-c.Position)).ToArray());
    }
    private void CreateOpening(Lobby lobby,QualifyingState qualifying,IReadOnlyList<QualifyingRank> grid)
    {
        if(lobby.Race is not null)return;
        var weekends=qualifying.Preparation.Teams.OrderBy(t=>t.TeamId,StringComparer.Ordinal).Select(t=>t.Effective).ToArray();
        if(openingDisruptions?.ForCircuit(weekends[0].Source.Circuit.Id) is {} enabled)
        {
            if(enabled.Route is null)throw new SessionFault(SessionFailure.InvalidInput,"Safety Car activation requires an authored circuit pit route.");
            weekends=weekends.Select(weekend=>weekend with{Configuration=WeekendPresetCatalog.IncidentInputs(weekend,enabled.Incidents).Configuration with{Track=weekend.Configuration.Track with{PitRoute=enabled.Route}}}).ToArray();
        }
        var configuration=weekends[0].Configuration.WithExplicitTuning();
        var input=new AutomaticStartInput(configuration,weekends,grid.Select(g=>new StartGridSlot(g.Position,g.EntryId)).ToArray(),lobby.Input.Seed!.Value,new());
        var start=AutomaticRaceStart.Create(input);
        var drivers=AutomaticRaceStart.StartingDrivers(input);
        var session=new Session(lobby.Id,lobby.Players.Single(p=>p.Uid==lobby.Host).Id,lobby.Devices.First(d=>d.Uid==lobby.Host).Id,
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32),configuration,drivers,input.Seed,new(lobby.Input.Preset,lobby.Input.Laps,lobby.Input.Seed),start.Engine)
        {
            OpeningReplay=new(input,AutomaticRaceStart.OutcomeHash(start.Opening),qualifying.Preparation.ContextId),
            RequiredTeams=lobby.RequiredTeams,Paused=qualifying.Paused,Lifecycle=start.Opening.Finished?"finished":"phase-result",
            EngineEventCursor=start.Opening.Events.Count,DeferNextWindow=true,
            TimingPolicy=BuildTimingPolicy(lobby,lobby.RequiredTeams)
        };
        var changes=start.Opening.Classification.Count(c=>grid.Single(g=>g.EntryId==c.DriverId).Position!=c.Position);
        session.Presentation=new(Guid.NewGuid().ToString(),0,changes,0,0,0,0,0,
            $"Automatic standing start · {changes} cars changed position. Exact order at the leader's lap 1 crossing.");
        session.PresentationPhases[session.Presentation.Cursor]=0;
        session.ModuleState["opening-presentation"]=session.Presentation.Cursor;
        if(start.Opening.Interruption is {} incident)
        {
            session.Events=start.Opening.Events.Where(item=>item.Kind is RaceEventKind.Spin or RaceEventKind.Contact or RaceEventKind.MechanicalFailure or RaceEventKind.Retirement)
                .Take(12).Select(item=>new PublicEvent(Guid.NewGuid().ToString(),item.Kind.ToString(),item.Time,item.Lap,item.DriverId,item.OtherDriverId,item.Compound?.ToString())).ToArray();
            session.Presentation=session.Presentation with{Summary=$"Automatic standing start interrupted: Safety Car deployed for {incident.Kind} at {incident.Time:F1}s. Grid and observed start order are retained."};
        }
        lobby.Race=session;lobby.Lifecycle="started";
        PhaseCommitted(session);
        if(start.Opening.Interruption is not null&&!start.Opening.Finished)OpenHouseholdWindow(session);
        if(session.Paused)SaveProgress(session,Progress(session) with{PresentationRemaining=PresentationSeconds});
    }
}
