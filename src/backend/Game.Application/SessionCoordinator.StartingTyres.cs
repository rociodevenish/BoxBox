using System.Text.Json;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;
public sealed partial class SessionCoordinator
{
    private const string StartingTyreDisclosure="Choose both starting compounds simultaneously. Frozen fallback uses observed wetness only: Medium below .25, Intermediate below .7, otherwise Wet. No launch tactics. Accepted locks survive refresh and deadline; forecasts are uncertain.";
    private sealed record StartingTyreLock(string Source,IReadOnlyList<StartingTyreChoice> Choices);
    private sealed record AcceptedStartingTyres(string Fingerprint,QualifyingAck Ack);
    private sealed record StartingTyreState(CurrentWeather Observed,Dictionary<string,StartingTyreLock> Locks,
        Dictionary<string,StartingTyreLock> Fallback,Dictionary<string,AcceptedStartingTyres> Commands,
        Dictionary<string,TeamWeatherForecast>? Forecasts=null,Dictionary<string,StartingTyreGuidance>? Guidance=null);
    private static TeamWeatherForecast? QualifyingForecast(QualifyingState state,string team)
    {
        var observed=state.StartingTyres?.Observed??state.Input.Conditions?.Observed;
        if(state.StartingTyres?.Forecasts?.TryGetValue(team,out var saved)==true)return saved;
        var tuning=state.Input.Weekends[0].Configuration.Weather;
        return observed is null||tuning is null?null:CircuitWeather.Forecast(observed,team,tuning.InformationQuality,tuning.ForecastRefreshSeconds);
    }
    private static StartingCompoundSuitability[] StartingSuitability(CurrentWeather observed)=>Enum.GetValues<TyreCompound>().Select(compound=>
        new StartingCompoundSuitability(compound.ToString(),compound switch{
            TyreCompound.Intermediate=>observed.Wetness is >=.25 and <.7?"Suited to observed damp conditions":"Outside observed damp range",
            TyreCompound.Wet=>observed.Wetness>=.7?"Suited to observed wet conditions":"Outside observed wet range",
            _=>observed.Wetness<.25?"Suited to observed dry conditions":"Dry compound in observed wet conditions"})).ToArray();
    private QualifyingState OpenStartingTyres(Lobby lobby,QualifyingState state)
    {
        var tuning=state.Input.Weekends[0].Configuration.Weather!;
        var observed=CircuitWeather.Observe(tuning,state.Input.Seed,tuning.TimeOriginSeconds+540);
        var fallbackCompound=WeatherDecisionPolicy.StartingCompound(observed).ToString();
        var fallback=AllQualifyingDrivers(state).GroupBy(d=>d.TeamId).ToDictionary(g=>g.Key,g=>new StartingTyreLock("timeout-observation",g.Select(d=>new StartingTyreChoice(d.EntryId,fallbackCompound)).ToArray()));
        var locks=fallback.Where(p=>lobby.ScriptedTeams.Contains(p.Key)).ToDictionary(p=>p.Key,p=>p.Value with{Source="scripted-observation"});
        var revision=state.Revision+1;
        var timing=BuildTimingPolicy(lobby,lobby.RequiredTeams) with{TimeoutPolicy=StartingTyreDisclosure};
        var due=clock.GetUtcNow().AddSeconds(timing.EffectiveSeconds);
        var forecasts=fallback.Keys.ToDictionary(team=>team,team=>CircuitWeather.Forecast(observed,team,tuning.InformationQuality,tuning.ForecastRefreshSeconds));
        var guidance=fallback.Keys.ToDictionary(team=>team,team=>{
            var assignment=lobby.Assignments.GetValueOrDefault(team);
            var settings=assignment is null?new PlayerAssistanceSettings():AssistanceSettings(lobby,assignment.PlayerId);
            return new StartingTyreGuidance(settings.HelpLevel,settings.AutoAcceptRecommendation,WeatherDecisionPolicy.Version,
                "Frozen from observed track wetness. Medium below 25%, Intermediate below 70%, otherwise Wet. Predicted rain and conditions at launch remain uncertain; all five compounds remain available.",
                settings.HelpLevel=="Standard"?null:fallback[team].Choices,Enum.GetNames<TyreCompound>());
        });
        return state with{StartingTyres=new(observed,locks,fallback,[],forecasts,guidance),Lifecycle="starting-tyres",Revision=revision,WindowRevision=revision,
            TimingPolicy=timing,Deadline=state.Paused?null:due,Remaining=state.Paused?timing.EffectiveSeconds:null,
            Wakeup=state.Paused?null:new(lobby.Id,Guid.NewGuid().ToString("N"),"starting-tyres",3,revision,due)};
    }
    private static QualifyingTeamSnapshot ProjectStartingTyreTeam(Lobby lobby,QualifyingState state,string team,Assignment assignment)
    {
        var tyres=state.StartingTyres!;var accepted=tyres.Locks.GetValueOrDefault(team);
        return new(lobby.Id,team,state.WindowRevision,assignment.Revision,"StartTyres",state.Lifecycle,accepted is not null,
            AllQualifyingDrivers(state).Where(d=>d.TeamId==team).ToArray(),null,null,accepted?.Source,"starting-tyres-observation-v1",
            QualifyingForecast(state,team),accepted?.Choices,tyres.Fallback.GetValueOrDefault(team)?.Choices,tyres.Guidance?.GetValueOrDefault(team));
    }
    private QualifyingState SettleStartingTyres(Lobby lobby,QualifyingState state)
    {
        var tyres=state.StartingTyres!;
        foreach(var team in lobby.RequiredTeams.Where(t=>!tyres.Locks.ContainsKey(t)))tyres.Locks[team]=tyres.Fallback[team];
        CreateOpening(lobby,state,state.Grid!);
        return state with{Lifecycle="grid-ready",Revision=state.Revision+1,Deadline=null,Wakeup=null,Remaining=null};
    }
    public QualifyingAck SubmitStartingTyres(string id,AuthIdentity identity,string deviceId,string teamId,StartingTyresCommand command)
    {
        mutations.Wait();try{lock(gate)
        {
            var access=AuthorisePreparationController(id,identity,deviceId,teamId);var lobby=access.Lobby;var state=ReadQualifying(lobby);
            if(command is null||command.Choices is null||command.Choices.Any(c=>c is null||string.IsNullOrWhiteSpace(c.EntryId)))
                throw new SessionFault(SessionFailure.InvalidInput,"Supply both starting compounds.");
            var key=ValidatePreparationCommand(command.CommandId,command.Revision,command.AssignmentRevision,access.Assignment.Revision);
            var tyres=state.StartingTyres??throw new SessionFault(SessionFailure.Conflict,"Starting tyre selection is not open.");
            var choices=command.Choices.OrderBy(c=>c.EntryId,StringComparer.Ordinal).ToArray();
            var fingerprint=JsonSerializer.Serialize(new{Operation="starting-tyres",id,teamId,command.Revision,command.AssignmentRevision,Choices=choices});
            if(tyres.Commands.TryGetValue(key,out var accepted))return accepted.Fingerprint==fingerprint?accepted.Ack:throw new SessionFault(SessionFailure.Conflict,"Command identity changed.");
            if(state.Commands.ContainsKey(key)||state.Starts.ContainsKey(key)||state.ContinueCommands?.ContainsKey(key)==true||ReadPreparation(lobby).Teams.Values.Any(t=>t.Commands.ContainsKey(key)))
                throw new SessionFault(SessionFailure.Conflict,"Command identity belongs to another operation.");
            var entries=AllQualifyingDrivers(state).Where(d=>d.TeamId==teamId).Select(d=>d.EntryId).Order(StringComparer.Ordinal);
            if(!choices.Select(c=>c.EntryId).SequenceEqual(entries)||choices.Any(c=>!Enum.TryParse<TyreCompound>(c.Compound,false,out var compound)||!Enum.IsDefined(compound)||compound.ToString()!=c.Compound))
                throw new SessionFault(SessionFailure.InvalidInput,"Choose exactly one legal starting compound for each driver.");
            if(state.Lifecycle!="starting-tyres"||state.Paused||command.Revision!=state.WindowRevision||tyres.Locks.ContainsKey(teamId)||state.Deadline is {} due&&clock.GetUtcNow()>=due)
                throw new SessionFault(SessionFailure.Conflict,"Starting tyre window changed or team is already locked.");
            tyres.Locks[teamId]=new("human",choices);state=state with{Revision=state.Revision+1};
            if(lobby.RequiredTeams.All(tyres.Locks.ContainsKey))state=SettleStartingTyres(lobby,state);
            var ack=new QualifyingAck(key,id,teamId,"StartTyres",state.Revision,state.Lifecycle);tyres.Commands[key]=new(fingerprint,ack);
            lobby.Revision++;WriteQualifying(lobby,state);Persist(id);return ack;
        }}finally{mutations.Release();}
    }
}
