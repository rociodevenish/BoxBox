namespace Game.Application;

/// <summary>Replaceable, conditional durable record boundary. Records contain only application/replay contracts.</summary>
public interface ISessionStore
{
    StoredSession? Read(string id);
    IReadOnlyList<string> FindByJoinCode(string code);
    string Write(string id, string? expectedVersion, string payload, string? joinCode);
}
public sealed record StoredSession(string Version, string Payload);
public sealed class VolatileSessionStore : ISessionStore
{
    private readonly Dictionary<string,(StoredSession Record,string? Code)> records=[];
    public StoredSession? Read(string id) { lock(records) return records.GetValueOrDefault(id).Record; }
    public IReadOnlyList<string> FindByJoinCode(string code) { lock(records) return records.Where(p=>p.Value.Code==code).Select(p=>p.Key).ToArray(); }
    public string Write(string id,string? expectedVersion,string payload,string? joinCode)
    {
        lock(records)
        {
            if(records.GetValueOrDefault(id).Record?.Version!=expectedVersion)throw new SessionFault(SessionFailure.Conflict,"Saved session changed. Reconnect before retrying.");
            var version=Guid.NewGuid().ToString("N");records[id]=(new(version,payload),joinCode);return version;
        }
    }
}
