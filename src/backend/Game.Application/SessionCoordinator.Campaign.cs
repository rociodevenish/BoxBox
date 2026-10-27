using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
using System.Text.Json;
namespace Game.Application;

public sealed partial class SessionCoordinator
{
    private const string CampaignRebind="campaign-boundary-rebind-v1";
    private sealed record CampaignRebindIntent(string DeviceId,int Phase);
    public void PrepareAbsentCampaignTeam(string id,AuthIdentity identity,string teamId)
    {
        mutations.Wait();try{lock(gate)
        {
            RefreshCampaignWeekend(id);var lobby=RequireLobby(id);RequireHost(lobby,identity);
            if(lobby.CampaignPresets is null||lobby.Lifecycle!="preparing")throw new SessionFault(SessionFailure.Conflict,"Absent-team preparation is available only before the qualifying lock.");
            var state=ReadPreparation(lobby);
            if(!lobby.Assignments.ContainsKey(teamId)||!state.Teams.TryGetValue(teamId,out var team))throw new SessionFault(SessionFailure.InvalidInput,"Choose an assigned campaign team.");
            if(team.Lifecycle=="final-ready")return;
            PreparedTeam prepared;
            if(team.Practice is null)prepared=PrepareAutomaticTeam(lobby,team);
            else
            {
                var observation=ObservePreparation(team);var choice=PreparationDecisionPolicy.Evaluate(observation);
                prepared=team with{Setup=choice.Setup,Effective=ApplyWeekendConditions(lobby,WeekendPreparation.Prepare(team.Preset,lobby.Input.Laps!.Value,choice.Setup)),Lifecycle="final-ready",Revision=team.Revision+1,AutomaticChoices=[new("final",observation,choice)]};
            }
            state.Teams[teamId]=prepared with{Source="host-absent-ai"};
            lobby.Revision++;WritePreparation(lobby,state);Persist(id);
        }}finally{mutations.Release();}
    }
    private void ApplyCampaignRebindBoundary(Session session,Lobby lobby)
    {
        if(!lobby.ModuleState.TryGetValue(CampaignRebind,out var json))return;
        var pending=JsonSerializer.Deserialize<Dictionary<string,CampaignRebindIntent>>(json,BrowserReplay.JsonOptions)!;
        foreach(var (team,intent) in pending.ToArray())
        {
            if(intent.Phase>session.Phase)continue;
            if(lobby.Assignments.TryGetValue(team,out var assignment))
                lobby.Assignments[team]=assignment with{DeviceId=intent.DeviceId,AdditionalDevices=[],Approved=true,Revision=++lobby.Revision};
            pending.Remove(team);
        }
        if(pending.Count==0)lobby.ModuleState.Remove(CampaignRebind);else lobby.ModuleState[CampaignRebind]=JsonSerializer.Serialize(pending,BrowserReplay.JsonOptions);
    }
    public void RefreshCampaignWeekend(string id)
    {
        lock(gate)
        {
            StoredSession? saved;try{saved=persistence.Read(id);}catch(Exception){throw new SessionFault(SessionFailure.StorageUnavailable,"Campaign weekend unavailable. Retry after storage recovers.");}
            if(saved is null)throw new SessionFault(SessionFailure.Unavailable,"Campaign weekend unavailable.");
            if(storageVersions.GetValueOrDefault(id)==saved.Version)return;
            lobbies.Remove(id);if(active?.Id==id)active=null;Restore(id,saved);
        }
    }
    public void EnsureNextCampaignLobby(string previousId,string nextId,AuthIdentity identity,WeekendPreset[] frozen,RaceConfiguration conditions)
    {
        lock(gate)
        {
            RefreshCampaignWeekend(previousId);var previous=RequireLobby(previousId);RequireHost(previous,identity);EnsureRecovered(nextId);
            if(lobbies.TryGetValue(nextId,out var existing)){RequireHost(existing,identity);return;}
            var lobby=new Lobby(nextId,Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4)),previous.Host,previous.Input);
            lobby.CampaignPresets=frozen;lobby.CampaignConditions=conditions;lobby.ModuleState["campaign-standby-v1"]=previousId;
            lobby.Players.AddRange(previous.Players);lobby.Devices.AddRange(previous.Devices);
            foreach(var pair in previous.Assignments)lobby.Assignments.Add(pair.Key,pair.Value);
            // Weekend preparation is a new boundary: complete any authenticated device return,
            // but reset race AI modes, pending control intents and decision locks.
            if(previous.ModuleState.TryGetValue(CampaignRebind,out var pendingJson))
                foreach(var (team,intent) in JsonSerializer.Deserialize<Dictionary<string,CampaignRebindIntent>>(pendingJson,BrowserReplay.JsonOptions)!)
                    if(lobby.Assignments.TryGetValue(team,out var assignment))lobby.Assignments[team]=assignment with{DeviceId=intent.DeviceId,AdditionalDevices=[],Approved=true,Revision=lobby.Revision};
            lobbies.Add(nextId,lobby);Persist(nextId);
        }
    }
    public void ActivateCampaignWeekend(string id,AuthIdentity identity)
    {
        lock(gate){RefreshCampaignWeekend(id);var lobby=RequireLobby(id);RequireHost(lobby,identity);if(lobby.ModuleState.Remove("campaign-standby-v1")){lobby.Revision++;Persist(id);}}
    }
    public JoinedLobby ReturnCampaignMember(string membershipId,string eventId,AuthIdentity identity,string? deviceId)
    {
        lock(gate)
        {
            RefreshCampaignWeekend(membershipId);if(eventId!=membershipId)RefreshCampaignWeekend(eventId);var source=RequireLobby(membershipId);
            var original=source.Players.SingleOrDefault(p=>p.Uid==identity.Uid)
                ??throw new SessionFault(SessionFailure.Forbidden,"Only an existing campaign participant can return. Join the household first.");
            var lobby=RequireLobby(eventId);
            var player=lobby.Players.SingleOrDefault(p=>p.Id==original.Id&&p.Uid==identity.Uid)
                ??throw new SessionFault(SessionFailure.Forbidden,"This participant does not belong to this event.");
            var device=lobby.Devices.SingleOrDefault(d=>d.Id==deviceId&&d.Uid==identity.Uid);
            if(device is null)
            {
                device=new(Guid.NewGuid().ToString("N"),identity.Uid);lobby.Devices.Add(device);lobby.Revision++;
            }
            // Registration can happen while qualifying is locked. Every explicit authenticated
            // return must also consider binding, including a device already known to this lobby.
            var owned=lobby.Assignments.Where(p=>p.Value.PlayerId==player.Id&&
                !(p.Value.Approved&&(p.Value.DeviceId==device.Id||(p.Value.AdditionalDevices?.Contains(device.Id)??false)))).ToArray();
            var changed=false;
            if(lobby.Lifecycle is "lobby" or "preparing")
            {
                foreach(var pair in owned)
                {
                    lobby.Assignments[pair.Key]=pair.Value with{DeviceId=device.Id,AdditionalDevices=[],Approved=true,Revision=++lobby.Revision};
                    changed=true;
                }
            }
            else if(lobby.Race is {} race&&!race.Engine.GetState().Finished)
            {
                var pending=lobby.ModuleState.TryGetValue(CampaignRebind,out var json)?JsonSerializer.Deserialize<Dictionary<string,CampaignRebindIntent>>(json,BrowserReplay.JsonOptions)!:[];
                foreach(var pair in owned)
                {
                    // Retrying the same return must not postpone its already accepted boundary.
                    if(pending.TryGetValue(pair.Key,out var intent)&&intent.DeviceId==device.Id)continue;
                    lobby.Assignments[pair.Key]=pair.Value with{Approved=false,AdditionalDevices=[],Revision=++lobby.Revision};
                    pending[pair.Key]=new(device.Id,race.Phase+1);changed=true;
                }
                if(changed)lobby.ModuleState[CampaignRebind]=JsonSerializer.Serialize(pending,BrowserReplay.JsonOptions);
            }
            // Persist new registration even when qualifying prevents a private grant.
            if(changed||device.Id!=deviceId)Persist(eventId);
            return new(ProjectLobby(lobby),new(player.Id,device.Id,lobby.Host==identity.Uid));
        }
    }
    public (CampaignEvent[] Calendar,CampaignTeam[] Teams,Dictionary<string,WeekendPreset[]> Presets,Dictionary<string,RaceConfiguration> Conditions) FreezeCampaignContent(string campaignId,int laps)
    {
        var tracks=new[]{"track-monza","track-monaco"};var frozen=new Dictionary<string,WeekendPreset[]>();
        CampaignRules.ValidateShortCalendar(tracks);
        var conditions=new Dictionary<string,RaceConfiguration>();
        var calendar=tracks.Select((track,index)=>
        {
            var eventId=$"{campaignId}-event-{index+1:00}";
            var teams=ConstructorCatalogue.Select((team,i)=>
            {
                var drivers=PreparationDrivers[i];var opened=WeekendPresetCatalog.Open(track,drivers.First,drivers.Second);
                return opened with{Cars=opened.Cars.Select((car,slot)=>car with{TeamId=team.TeamId,EntryId=$"{team.TeamId}-car-{slot+1}"}).ToImmutableArray()};
            }).ToArray();
            foreach(var preset in teams)_=WeekendPreparation.Prepare(preset,6,new());
            var source=new Lobby("","","",new("open-equal",laps,123,"Competitive","Campaign"));
            conditions[eventId]=ApplyWeekendConditions(source,WeekendPreparation.Prepare(teams[0],laps,new())).Configuration;
            frozen.Add(eventId,teams);var circuit=teams[0].Circuit;
            return new CampaignEvent(eventId,index+1,track,circuit.Name,circuit.Slug,circuit.Version,$"{eventId}-weekend");
        }).ToArray();
        var roster=ConstructorCatalogue.Select((team,i)=>new CampaignTeam(team.TeamId,team.Id,team.Name,
            frozen[calendar[0].EventId][i].Cars.Select(car=>new CampaignDriver(car.Driver.Id,car.Driver.Name,car.Driver.Slug,car.EntryId,car.Driver.Version)).ToArray())).ToArray();
        return(calendar,roster,frozen,conditions);
    }
    public JoinedLobby EnsureCampaignLobby(string sessionId,AuthIdentity identity,CreateLobbyInput input,WeekendPreset[] frozen,RaceConfiguration conditions,bool acquisitionPending=false)
    {
        lock(gate)
        {
            EnsureRecovered(sessionId);
            if(lobbies.TryGetValue(sessionId,out var existing))
            {
                RequireHost(existing,identity);var member=existing.Players.Single(p=>p.Uid==identity.Uid);
                return new(ProjectLobby(existing),new(member.Id,existing.Devices.First(d=>d.Uid==identity.Uid).Id,true));
            }
            if(identity.Guest)throw new SessionFault(SessionFailure.Forbidden,"Use a permanent account to host a campaign.");
            var lobby=new Lobby(sessionId,Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4)),identity.Uid,input);
            lobby.CampaignPresets=frozen;
            lobby.CampaignConditions=conditions;
            if(acquisitionPending)lobby.ModuleState["campaign-personnel-pending-v1"]="pending";
            var player=new Player(Guid.NewGuid().ToString("N"),Name(input.Name),identity.Uid);var device=new Device(Guid.NewGuid().ToString("N"),identity.Uid);
            lobby.Players.Add(player);lobby.Devices.Add(device);lobbies.Add(sessionId,lobby);Persist(sessionId);
            return new(ProjectLobby(lobby),new(player.Id,device.Id,true));
        }
    }
    public PreparationPublicSnapshot BeginCampaignPreparation(string sessionId,AuthIdentity identity,IReadOnlyList<WeekendPreset> frozen)=>
        BeginPreparationCore(sessionId,identity,new(frozen[0].Circuit.Id),frozen);
    public void CommitCampaignPersonnel(string sessionId,WeekendPreset[] frozen)
    {
        lock(gate)
        {
            RefreshCampaignWeekend(sessionId);var lobby=RequireLobby(sessionId);
            if(!lobby.ModuleState.ContainsKey("campaign-personnel-pending-v1"))return;
            if(lobby.Lifecycle!="lobby")throw new SessionFault(SessionFailure.Conflict,"Personnel handoff requires the campaign lobby.");
            lobby.CampaignPresets=frozen;lobby.ModuleState.Remove("campaign-personnel-pending-v1");lobby.Revision++;Persist(sessionId);
        }
    }
}
