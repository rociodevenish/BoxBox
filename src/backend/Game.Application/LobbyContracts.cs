namespace Game.Application;
public record AuthIdentity(string Uid, bool Guest);
public record CreateLobbyInput(string Preset,int? Laps,int? Seed,string Mode,string Name);
public record JoinLobbyInput(string Code,string Name,string? DeviceId=null);
public record LobbyMember(string PlayerId,string DeviceId,bool IsHost);
public record LobbyPlayer(string PlayerId,string Name,bool SharedParticipant);
public record LobbyTeam(string TeamId,string ConstructorId,string ConstructorName,string? PlayerId,string Control,bool Approved,long AssignmentRevision);
public record LobbySnapshot(string SessionId,string JoinCode,string JoinUrl,long Revision,string Lifecycle,string Mode,
 IReadOnlyList<LobbyPlayer> Players,IReadOnlyList<LobbyTeam> Teams,IReadOnlyList<string> RequiredTeams,IReadOnlyList<string> ScriptedTeams,string? DisplayUrl);
public record JoinedLobby(LobbySnapshot Lobby,LobbyMember Membership);
public record ClaimTeamInput(string TeamId,string DeviceId);
public record SharedParticipantInput(string Name,string DeviceId);
public record AssignTeamInput(string TeamId,string? PlayerId,string? DeviceId,string Control);
