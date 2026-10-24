using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;

public sealed partial class SessionCoordinator
{
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
                foreach(var pair in lobby.Assignments.Where(p=>p.Value.PlayerId==player.Id&&lobby.Lifecycle is "lobby" or "preparing").ToArray())
                {
                    // Explicit authenticated reclaim replaces prior devices; accepted setup and practice stay intact.
                    lobby.Assignments[pair.Key]=pair.Value with{DeviceId=device.Id,AdditionalDevices=[],Revision=lobby.Revision};
                }
                Persist(eventId);
            }
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
    public JoinedLobby EnsureCampaignLobby(string sessionId,AuthIdentity identity,CreateLobbyInput input,WeekendPreset[] frozen,RaceConfiguration conditions)
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
            var player=new Player(Guid.NewGuid().ToString("N"),Name(input.Name),identity.Uid);var device=new Device(Guid.NewGuid().ToString("N"),identity.Uid);
            lobby.Players.Add(player);lobby.Devices.Add(device);lobbies.Add(sessionId,lobby);Persist(sessionId);
            return new(ProjectLobby(lobby),new(player.Id,device.Id,true));
        }
    }
    public PreparationPublicSnapshot BeginCampaignPreparation(string sessionId,AuthIdentity identity,IReadOnlyList<WeekendPreset> frozen)=>
        BeginPreparationCore(sessionId,identity,new(frozen[0].Circuit.Id),frozen);
}
