namespace Game.Application;

/// <summary>Replaceable, conditional durable record boundary. Records contain only application/replay contracts.</summary>
public interface ISessionStore
{
    StoredSession? Read(string id);
    IReadOnlyList<string> FindByJoinCode(string code);
    IReadOnlyList<string> FindByMember(string uid)=>throw new NotSupportedException("Membership indexing is required for campaign discovery.");
    IReadOnlyList<string> ListSessions()=>throw new NotSupportedException("This persistence adapter must implement indexed scheduled-session enumeration before autonomous sweep recovery is enabled.");
    string Write(string id, string? expectedVersion, string payload, string? joinCode);
    string Write(string id,string? expectedVersion,string payload,string? joinCode,ScheduledWakeup? wakeup)=>Write(id,expectedVersion,payload,joinCode);
}
public sealed record StoredSession(string Version, string Payload);
public sealed class VolatileSessionStore : ISessionStore
{
    private readonly Dictionary<string,(StoredSession Record,string? Code)> records=[];
    private readonly HashSet<string> scheduled=[];
    private readonly Dictionary<string,string[]> members=[];
    private readonly Dictionary<string,HashSet<string>> memberSessions=[];
    public IReadOnlyList<string> FindByMember(string uid){lock(records)return memberSessions.TryGetValue(uid,out var ids)?ids.ToArray():[];}
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
            var indexed=SessionMembershipIndex.Members(payload);
            foreach(var uid in members.GetValueOrDefault(id)??[])if(memberSessions.TryGetValue(uid,out var old)){old.Remove(id);if(old.Count==0)memberSessions.Remove(uid);}
            foreach(var uid in indexed){if(!memberSessions.TryGetValue(uid,out var ids)){ids=[];memberSessions.Add(uid,ids);}ids.Add(id);}
            var version=Guid.NewGuid().ToString("N");members[id]=indexed;records[id]=(new(version,payload),joinCode);return version;
        }
    }
}
