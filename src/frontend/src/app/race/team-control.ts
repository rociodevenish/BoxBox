export interface TeamControlStatus {teamId:string;mode:'Human'|'TemporaryAi'|'PermanentAi';revision:number;pendingMode?:string|null;effectivePhase?:number|null;untilPhase?:number|null}
export function controlName(mode:string){return mode==='TemporaryAi'?'Temporary AI':mode==='PermanentAi'?'Permanent AI':'Human';}
