namespace Game.Application;
public sealed record ControlRequest(string CommandId,long Revision,string Mode);
public sealed record TeamControlStatus(string TeamId,string Mode,long Revision,string? PendingMode=null,int? EffectivePhase=null,int? UntilPhase=null);
