using System.Text.Json;
namespace Game.Application;

public sealed class CampaignStore(ISessionStore sessions):ICampaignStore
{
    public StoredCampaign? Read(string id)
    {
        var saved=sessions.Read("campaign-"+id);if(saved is null)return null;
        CampaignRecord? record;
        try{record=JsonSerializer.Deserialize<CampaignRecord>(saved.Payload,BrowserReplay.JsonOptions);if(record is not null)CampaignResultIntegrity.Validate(record);}
        catch(Exception error) when(error is JsonException or ArgumentException or InvalidOperationException){throw new SessionFault(SessionFailure.Conflict,"Campaign save is incompatible. Preserve it for the compatible deployment.");}
        if(record is null||record.Schema!=CampaignCoordinator.Schema||record.CampaignId!=id)
            throw new SessionFault(SessionFailure.Conflict,"Campaign save is incompatible. Preserve it for the compatible deployment.");
        return new(saved.Version,record);
    }
    public string Write(string id,string? expectedVersion,CampaignRecord record)=>
        sessions.Write("campaign-"+id,expectedVersion,JsonSerializer.Serialize(record,BrowserReplay.JsonOptions),null);
}
