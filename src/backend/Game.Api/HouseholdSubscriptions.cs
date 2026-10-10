using Game.Application;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
namespace Game.Api;
public sealed class HouseholdSubscriptions
{
 private readonly ConcurrentDictionary<(string Connection,string Session,string Team,long Revision),byte> streams=[];
 public void Add(string connection,string session,string team,long revision)=>streams[(connection,session,team,revision)]=0;
 public void Remove(string connection,string session,string team,long revision)=>streams.TryRemove((connection,session,team,revision),out _);
 public void RemoveConnection(string connection){foreach(var key in streams.Keys.Where(key=>key.Connection==connection))streams.TryRemove(key,out _);}
 public async Task CloseObsolete(IGroupManager groups,LobbySnapshot lobby)
 {foreach(var key in streams.Keys.Where(key=>key.Session==lobby.SessionId&&lobby.Teams.Any(team=>team.TeamId==key.Team&&team.AssignmentRevision!=key.Revision)))
  {await groups.RemoveFromGroupAsync(key.Connection,$"household:{key.Session}:{key.Team}:{key.Revision}");streams.TryRemove(key,out _);}}
}

