import {AssistanceSummary} from '../assistance/assistance-summary';
import type {TeamAssistance} from '../assistance/assistance.model';
import {Component,signal} from '@angular/core';
import {HubConnection,HubConnectionBuilder} from '@microsoft/signalr';
import {TimingTower} from '../ui/timing/timing-tower';
import {PublicClassification} from '../ui/timing/public-classification.model';
import {PublicState} from '../race/race-session';
import {CompoundBadge} from '../ui/compound-badge';
import {DriverCard} from '../ui/driver-card';
import {raceDriverCard} from '../race/race-driver-card';
interface OpenTeam {
 assistance?:TeamAssistance;
 sessionId:string;teamId:string;phase:number;revision:number;assignmentRevision:number;lifecycle:string;
 cars:{driverId:string;compound:'Soft'|'Medium'|'Hard';tyreAge:number;estimatedTyreCondition:number;inPits:boolean;stopQueued:boolean;battleOptions:string[]}[];
 confirmedPlan:{cars:{driverId:string;pace:string;pit:string;compound:string|null;battle:string}[]}|null;
}
interface Lobby {sessionId:string;mode:string;teams:{teamId:string;constructorName:string;control:string;playerId:string|null;approved:boolean;assignmentRevision:number}[];players:{playerId:string;name:string}[]}
@Component({selector:'app-open-display',imports:[TimingTower,CompoundBadge,DriverCard,AssistanceSummary],templateUrl:'./open-display.html',styleUrl:'./open-display.css'})
export class OpenDisplay {
 readonly id=location.pathname.split('/')[3];readonly lobby=signal<Lobby|null>(null);readonly race=signal<PublicState|null>(null);readonly teams=signal<OpenTeam[]>([]);readonly status=signal('Connecting');readonly error=signal('');
 readonly driverCard=raceDriverCard;
 private hub?:HubConnection;
 constructor(){void this.connect();}
 ngOnDestroy(){const hub=this.hub;this.hub=undefined;void hub?.stop();}
 private acceptTeam(state:OpenTeam){const assignment=this.lobby()?.teams.find(t=>t.teamId===state.teamId&&t.control==='Open'&&t.approved);if(state.sessionId!==this.id||!assignment||state.assignmentRevision!==assignment.assignmentRevision)return;this.teams.update(teams=>{const previous=teams.find(t=>t.teamId===state.teamId);return previous&&previous.revision>=state.revision?teams:[...teams.filter(t=>t.teamId!==state.teamId),state].sort((a,b)=>a.teamId.localeCompare(b.teamId));});}
 async connect(){this.error.set('');this.status.set('Connecting');await this.hub?.stop();const hub=new HubConnectionBuilder().withUrl('/hubs/race').withAutomaticReconnect([0,1000,3000]).build();this.hub=hub;
  let recoveryGeneration=0;const recover=async()=>{const generation=++recoveryGeneration;const response=await fetch('/api/lobbies/'+this.id,{cache:'no-store'});if(!response.ok)throw new Error('Household unavailable. Return to the lobby.');const lobby:Lobby=await response.json();if(this.hub!==hub||generation!==recoveryGeneration)return;this.lobby.set(lobby);this.teams.set([]);const race=await hub.invoke<PublicState>('SubscribePublic',this.id);if(this.hub!==hub||generation!==recoveryGeneration)return;if(race.revision>=(this.race()?.revision??-1))this.race.set(race);for(const team of lobby.teams.filter(t=>t.control==='Open'&&t.playerId&&t.approved))this.acceptTeam(await hub.invoke<OpenTeam>('SubscribeOpenTeam',this.id,team.teamId));this.status.set('Connected');};
  hub.on('lobbyRevision',()=>{void recover().catch(error=>this.fail(error));});
  hub.on('publicSnapshot',(state:PublicState)=>{if(this.hub===hub&&state.sessionId===this.id&&state.revision>(this.race()?.revision??-1))this.race.set(state);});hub.on('openTeamSnapshot',(state:OpenTeam)=>{if(this.hub===hub)this.acceptTeam(state);});
  hub.onreconnecting(()=>{this.status.set('Reconnecting');this.teams.set([]);});hub.onreconnected(()=>void recover().catch(error=>this.fail(error)));hub.onclose(()=>{if(this.hub===hub)this.status.set('Disconnected');});try{await hub.start();await recover();}catch(error){this.fail(error);}
 }
 private fail(error:unknown){this.teams.set([]);this.status.set('Unavailable');this.error.set(error instanceof Error?error.message:'Unable to recover household display.');}
 owner(id:string){const team=this.lobby()?.teams.find(t=>t.teamId===id);return this.lobby()?.players.find(p=>p.playerId===team?.playerId)?.name??'Household player';}
 name(id:string){return this.lobby()?.teams.find(t=>t.teamId===id)?.constructorName??id;}
 percent(value:number){return Math.round(value*100)+'%';}
 classification():PublicClassification {const state=this.race()!;return {phase:state.phase+1,leaderLap:state.leaderLap,totalLaps:state.totalLaps,gapReference:'ahead',compact:true,rows:state.classification.map(car=>({position:car.position,driver:{id:car.driverId,code:car.driverId.slice(-2),name:car.name,team:{name:this.name(car.teamId),accent:'#ff685b'}},gap:car.gapToAhead,compound:car.compound,tyreAge:car.tyreAge,completedLaps:car.completedLaps,eligibleAtFinish:car.eligibleAtFinish,completedPitStops:car.completedPitStops,isFastestLap:car.isFastestLap,stopStatus:car.pitState==='in-pit-lane'?'in-pit-lane':'on-track'}))};}
}
