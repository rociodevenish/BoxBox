namespace Game.Application;

/// <summary>Replaceable, conditional durable record boundary. Records contain only application/replay contracts.</summary>
public interface ISessionStore
{
    StoredSession? Read(string id);
    IReadOnlyList<string> FindByJoinCode(string code);
    IReadOnlyList<string> ListSessions()=>throw new NotSupportedException("This persistence adapter must implement indexed scheduled-session enumeration before autonomous sweep recovery is enabled.");
    string Write(string id, string? expectedVersion, string payload, string? joinCode);
    string Write(string id,string? expectedVersion,string payload,string? joinCode,ScheduledWakeup? wakeup)=>Write(id,expectedVersion,payload,joinCode);
}
public sealed record StoredSession(string Version, string Payload);
public sealed class VolatileSessionStore : ISessionStore
{
    private readonly Dictionary<string,(StoredSession Record,string? Code)> records=[];
    private readonly HashSet<string> scheduled=[];
    public StoredSession? Read(string id) { lock(records) return records.GetValueOrDefault(id).Record; }
    public IReadOnlyList<string> FindByJoinCode(string code) { lock(records) return records.Where(p=>p.Value.Code==code).Select(p=>p.Key).ToArray(); }
    public IReadOnlyList<string> ListSessions(){lock(records)return scheduled.ToArray();}
    public string Write(string id,string? expectedVersion,string payload,string? joinCode,ScheduledWakeup? wakeup)
    {
        lock(records){var version=Write(id,expectedVersion,payload,joinCode);if(wakeup is null)scheduled.Remove(id);else scheduled.Add(id);return version;}
    }
    public string Write(string id,string? expectedVersion,string payload,string? joinCode)
    {
        lock(records)
        {
            if(records.GetValueOrDefault(id).Record?.Version!=expectedVersion)throw new SessionFault(SessionFailure.Conflict,"Saved session changed. Reconnect before retrying.");
            var version=Guid.NewGuid().ToString("N");records[id]=(new(version,payload),joinCode);return version;
        }
    }
}
