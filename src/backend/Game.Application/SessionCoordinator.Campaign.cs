using System.Collections.Immutable;
using Game.Domain;
using Game.Simulation;
namespace Game.Application;

public sealed partial class SessionCoordinator
{
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
