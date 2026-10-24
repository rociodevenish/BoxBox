using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Game.Domain;
namespace Game.Application;

/// <summary>Durable campaign orchestration over the public household weekend operations.</summary>
public sealed class CampaignCoordinator(SessionCoordinator weekends,ICampaignStore store)
{
    public const string Schema="campaign-record-v1";
    public const string ContentVersion="preset-campaign-v1/provisional-2026.10.08";
    public const string CalendarVersion="authored-short-monza-monaco-v1";
    private readonly object gate=new();
    public CampaignJoined Create(AuthIdentity identity,CreateCampaignInput input)
    {
        if(identity.Guest)throw new SessionFault(SessionFailure.Forbidden,"Use a permanent account to host a campaign.");
        if(input is null||!Guid.TryParse(input.CommandId,out var command)||command==Guid.Empty||string.IsNullOrWhiteSpace(input.Name)||input.Name.Trim().Length>40
            ||input.Calendar!="short"||input.Mode is not("Competitive" or "Family" or "Mixed")||input.Laps is not(6 or 10 or 20 or 40 or 50))
            throw new SessionFault(SessionFailure.InvalidInput,"Choose the short fixture calendar, a player name, supported distance and privacy mode. Full calendar awaits an approved manifest.");
        var id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity.Uid+"/"+command.ToString("D"))))[..32].ToLowerInvariant();
        var fingerprint=JsonSerializer.Serialize(input with{CommandId=command.ToString("D"),Name=input.Name.Trim()});
        lock(gate)
        {
            var saved=ReadOptional(id);
            if(saved is null)
            {
                var content=weekends.FreezeCampaignContent(id,input.Laps);
                var record=new CampaignRecord(Schema,id,identity.Uid,fingerprint,input,1,"created",ContentVersion,CalendarVersion,content.Calendar,content.Teams,content.Presets,content.Conditions);
                try{Write(id,null,record);}catch(SessionFault fault) when(fault.Failure==SessionFailure.Conflict){ }
                saved=Require(id);
            }
            if(saved.Record.HostUid!=identity.Uid||saved.Record.CreationFingerprint!=fingerprint)
                throw new SessionFault(SessionFailure.Conflict,"Creation identity already belongs to different campaign inputs.");
            var joined=EnsureWeekend(saved.Record,identity);
            return new(Project(saved.Record),joined);
        }
    }
    public CampaignSnapshot Snapshot(string id,AuthIdentity identity,string deviceId)
    {
        lock(gate){var saved=Require(id);_=weekends.LobbyMembership(saved.Record.Calendar[saved.Record.EventIndex].SessionId,identity,deviceId);return Project(saved.Record);}
    }
    public CampaignSnapshot Prepare(string id,AuthIdentity identity)
    {
        lock(gate)
        {
            var saved=RequireHost(id,identity);var record=saved.Record;
            EnsureWeekend(record,identity);
            var current=record.Calendar[record.EventIndex];
            weekends.BeginCampaignPreparation(current.SessionId,identity,record.FrozenPresets[current.EventId]);
            if(record.Lifecycle=="created"){record=record with{Lifecycle="event-preparation",Revision=record.Revision+1};Write(id,saved.Version,record);}
            return Project(record);
        }
    }
    public CampaignSnapshot Start(string id,AuthIdentity identity,CampaignStartInput input)
    {
        if(input is null||!Guid.TryParse(input.CommandId,out var command)||command==Guid.Empty)throw new SessionFault(SessionFailure.InvalidInput,"Supply a start command identity.");
        lock(gate)
        {
            var saved=RequireHost(id,identity);var record=saved.Record;var current=record.Calendar[record.EventIndex];
            // Commit the stable operation identity before calling the weekend. A lost acknowledgement or restart retries this same operation/session.
            if(record.StartCommandId is null)
            {
                var preparation=weekends.PreparationPublic(current.SessionId);
                if(!preparation.Readiness.All(t=>t.FinalReady))throw new SessionFault(SessionFailure.Conflict,"Every team must be final-ready before the host starts the weekend.");
                record=record with{StartCommandId=command.ToString("D"),Revision=record.Revision+1};
                Write(id,saved.Version,record);saved=Require(id);record=saved.Record;
            }
            var lobby=weekends.LobbyPublic(current.SessionId);
            if(lobby.Lifecycle=="preparing")weekends.StartQualifying(current.SessionId,identity,new(record.StartCommandId!,lobby.Revision));
            if(record.Lifecycle!="active-weekend") {record=record with{Lifecycle="active-weekend",Revision=record.Revision+1};Write(id,saved.Version,record);}
            return Project(record);
        }
    }
    private JoinedLobby EnsureWeekend(CampaignRecord record,AuthIdentity identity)=>weekends.EnsureCampaignLobby(record.Calendar[record.EventIndex].SessionId,identity,
        new("open-equal",record.Input.Laps,record.Input.Seed,record.Input.Mode,record.Input.Name),record.FrozenPresets[record.Calendar[record.EventIndex].EventId],record.EventConditions[record.Calendar[record.EventIndex].EventId]);
    private CampaignSnapshot Project(CampaignRecord record)
    {
        var current=record.Calendar[record.EventIndex];var lobby=weekends.LobbyPublic(current.SessionId);
        IReadOnlyList<PreparationReadiness> readiness=lobby.Lifecycle=="lobby"?[]:weekends.PreparationPublic(current.SessionId).Readiness;
        var lifecycle=CampaignRules.Lifecycle(record.Lifecycle,lobby.Lifecycle);
        return new(record.CampaignId,record.Revision,lifecycle,record.ContentVersion,record.CalendarVersion,record.Input.Mode,record.Calendar,record.Teams,
            record.EventIndex,current.SessionId,readiness,CampaignRules.CanStart(lifecycle,readiness.Select(t=>t.FinalReady).ToArray()));
    }
    private StoredCampaign RequireHost(string id,AuthIdentity identity){var saved=Require(id);if(saved.Record.HostUid!=identity.Uid)throw new SessionFault(SessionFailure.Forbidden,"Only this campaign's host can begin an event.");return saved;}
    private StoredCampaign Require(string id)=>ReadOptional(id)??throw new SessionFault(SessionFailure.Unavailable,"Campaign unavailable.");
    private StoredCampaign? ReadOptional(string id){try{return store.Read(id);}catch(SessionFault){throw;}catch(Exception){throw new SessionFault(SessionFailure.StorageUnavailable,"Campaign storage unavailable. Retry the same command.");}}
    private void Write(string id,string? version,CampaignRecord record){try{store.Write(id,version,record);}catch(SessionFault){throw;}catch(Exception){throw new SessionFault(SessionFailure.StorageUnavailable,"Campaign save confirmation unavailable. Retry the same command.");}}
}
