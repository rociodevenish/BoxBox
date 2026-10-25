using System.Collections.Concurrent;
using Game.Application;

namespace Game.Api.Tests;

// All coordinators in one scenario share this scope and the real durable store.
// A manual clock may sweep its own sessions, never another test's weekend.
internal sealed class CampaignTestSessionScope(ISessionStore inner):ISessionStore
{
 private readonly ConcurrentDictionary<string,byte> ids=new();
 public StoredSession? Read(string id)
 {
  var saved=inner.Read(id);if(saved is not null)ids.TryAdd(id,0);return saved;
 }
 public IReadOnlyList<string> FindByJoinCode(string code)=>inner.FindByJoinCode(code).Where(ids.ContainsKey).ToArray();
 public IReadOnlyList<string> ListSessions()=>ids.Keys.Where(id=>!id.StartsWith("campaign-",StringComparison.Ordinal)).ToArray();
 public string Write(string id,string? version,string payload,string? code)=>Write(id,version,payload,code,null);
 public string Write(string id,string? version,string payload,string? code,ScheduledWakeup? wakeup)
 {
  // An uncertain acknowledgement can follow a durable write: retain the identity
  // before invoking the adapter so restart/retry enumeration still sees it.
  ids.TryAdd(id,0);return inner.Write(id,version,payload,code,wakeup);
 }
}
