namespace Game.Application;

public sealed class CampaignStore(ISessionStore sessions):ICampaignStore
{
 private const string StorageFormat="campaign-brotli-json-v1";
 public IReadOnlyList<string> ListScheduled()=>sessions.ListSessions().Where(id=>id.StartsWith("campaign-",StringComparison.Ordinal)).Select(id=>id[9..]).ToArray();
 public StoredCampaign? Read(string id)
 {
  var saved=sessions.Read("campaign-"+id);if(saved is null)return null;CampaignRecord? record;
  try{record=BoundedRecoveryJson.Decode<CampaignRecord>(saved.Payload,StorageFormat);if(record is not null)CampaignSaveIntegrity.Validate(record,id);}
  catch(Exception error) when(error is System.Text.Json.JsonException or ArgumentException or InvalidOperationException or NullReferenceException or KeyNotFoundException or InvalidDataException or FormatException){throw CampaignSaveIntegrity.Incompatible();}
  if(record is null||record.Schema!=CampaignCoordinator.Schema||record.CampaignId!=id)throw new SessionFault(SessionFailure.Conflict,"Campaign save is incompatible. Preserve it for the compatible deployment.");
  return new(saved.Version,record);
 }
 public string Write(string id,string? expectedVersion,CampaignRecord record)=>sessions.Write("campaign-"+id,expectedVersion,BoundedRecoveryJson.Encode(record,StorageFormat,Oversize),null,record.AcquisitionWakeup);
 private static SessionFault Oversize()=>new(SessionFailure.InvalidInput,"Campaign recovery exceeds supported storage bounds. No saved data was changed. Preserve the current campaign for diagnostics; reduce new advice requests or restore a compatible deployment with a reviewed larger storage format.");
}
