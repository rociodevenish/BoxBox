import {AssistanceSettings} from '../assistance/assistance-settings';
import type {PlayerHelp} from '../assistance/assistance.model';
import { Component, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import type { Auth, User } from 'firebase/auth';
interface Player {playerId:string;name:string;sharedParticipant:boolean;assistance?:PlayerHelp}
interface Team {teamId:string;constructorId:string;constructorName:string;playerId:string|null;control:string;approved:boolean;assignmentRevision:number}
interface Lobby {sessionId:string;joinCode:string;joinUrl:string;revision:number;lifecycle:string;mode:string;players:Player[];teams:Team[];requiredTeams:string[];scriptedTeams:string[];displayUrl:string|null;paused:boolean;timingPolicy?:{effectiveSeconds:number;decisionSeconds:number;handoffSeconds:number;additionalSharedTeams:number;timeoutPolicy:string}}
interface Membership {playerId:string;deviceId:string;isHost:boolean}
interface Joined {lobby:Lobby;membership:Membership}
@Component({selector:'app-shared-lobby',imports:[AssistanceSettings],templateUrl:'./shared-lobby.html',styleUrl:'./shared-lobby.css'})
export class SharedLobby {
 readonly controllerDevices=signal<{deviceId:string;playerName:string;teamIds:string[]}[]>([]);
 readonly authorisedTeams=signal<string[]>([]);
 hasOpenTeams(){return this.lobby()?.teams.some(team=>team.control==='Open'&&team.playerId&&team.approved)??false;}
 readonly hasSharedTeam=(team:Team)=>team.control==='SharedTablet' && !!team.playerId;
 readonly path=location.pathname;readonly joinMode=this.path.includes('/join');readonly id=this.path.startsWith('/shared/lobby/')?this.path.split('/').at(-1):undefined;
 readonly lobby=signal<Lobby|null>(null);readonly membership=signal<Membership|null>(null);readonly user=signal<User|null>(null);readonly ready=signal(false);readonly busy=signal(false);readonly error=signal('');readonly qr=signal('');readonly connection=signal('Connecting');
 readonly code=new URL(location.href).searchParams.get('code')??'';readonly accountName=signal(localStorage.getItem('household-player-name')??'');
 private auth?:Auth;private hub?:HubConnection;
 constructor(){void this.run(async()=>{
  const config=await this.request<{projectId:string;apiKey:string;emulatorUrl:string|null}>('/api/auth/config',undefined,false);
  const {initializeApp}=await import('firebase/app');const sdk=await import('firebase/auth');this.auth=sdk.getAuth(initializeApp({projectId:config.projectId,apiKey:config.apiKey}));
  if(config.emulatorUrl)sdk.connectAuthEmulator(this.auth,config.emulatorUrl,{disableWarnings:true});
  await new Promise<void>(resolve=>{const unsubscribe=sdk.onAuthStateChanged(this.auth!,user=>{this.user.set(user);unsubscribe();resolve();});});this.ready.set(true);
  if(this.id&&this.user()){const device=localStorage.getItem(this.deviceKey(this.id));if(device)await this.accept(await this.request<Joined>('/api/lobbies/'+this.id+'/membership?deviceId='+encodeURIComponent(device)));else this.error.set('Join this lobby on this device before claiming a team.');}
 });}
 ngOnDestroy(){void this.hub?.stop();}
 private deviceKey(id:string){return 'household-device:'+this.user()?.uid+':'+id;}
 private async request<T>(url:string,input?:unknown,authenticated=true):Promise<T>{const response=await fetch(url,{method:input===undefined?'GET':'POST',headers:{'Content-Type':'application/json',...(authenticated?{'Authorization':'Bearer '+await this.user()?.getIdToken()}: {})},body:input===undefined?undefined:JSON.stringify(input)});const result=await response.json();if(!response.ok)throw new Error(result.message??'Request unavailable. Try again.');return result;}
 private async run(action:()=>Promise<void>){if(this.busy())return;this.busy.set(true);this.error.set('');try{await action();}catch(error){this.error.set(error instanceof Error?error.message:'Unable to complete this action.');}finally{this.busy.set(false);}}
 async account(name:string,email:string,password:string,create:boolean){await this.run(async()=>{const sdk=await import('firebase/auth');const credential=await(create?sdk.createUserWithEmailAndPassword(this.auth!,email,password):sdk.signInWithEmailAndPassword(this.auth!,email,password));this.user.set(credential.user);this.accountName.set(name);localStorage.setItem('household-player-name',name);});}
 async signOut(){await this.run(async()=>{const sdk=await import('firebase/auth');await sdk.signOut(this.auth!);this.user.set(null);});}
 async create(name:string,preset:string,laps:string,seed:string,mode:string,decisionSeconds:string,handoffSeconds:string){await this.run(async()=>{await this.accept(await this.request<Joined>('/api/lobbies',{name,preset,laps:Number(laps),seed:Number(seed),mode,decisionSeconds:Number(decisionSeconds),handoffSeconds:Number(handoffSeconds)}));});}
 async join(name:string,code:string){await this.run(async()=>{if(!this.user()){const sdk=await import('firebase/auth');const credential=await sdk.signInAnonymously(this.auth!);this.user.set(credential.user);}this.accountName.set(name);localStorage.setItem('household-player-name',name);await this.accept(await this.request<Joined>('/api/lobbies/join',{name,code}));});}
 private async accept(joined:Joined){this.membership.set(joined.membership);this.apply(joined.lobby);localStorage.setItem(this.deviceKey(joined.lobby.sessionId),joined.membership.deviceId);history.replaceState(null,'','/shared/lobby/'+joined.lobby.sessionId);await this.connect();}
 private apply(state:Lobby){if(this.lobby()&&this.lobby()!.revision>state.revision)return;this.lobby.set(state);if(this.membership()?.isHost)void this.request<{deviceId:string;playerName:string;teamIds:string[]}[]>("/api/lobbies/"+state.sessionId+"/controller-devices").then(devices=>{if(this.lobby()?.revision===state.revision)this.controllerDevices.set(devices);}).catch(()=>undefined);this.authorisedTeams.set([]);if(state.lifecycle==='started'&&this.membership())void this.request<Team[]>('/api/lobbies/'+state.sessionId+'/controllers?deviceId='+encodeURIComponent(this.membership()!.deviceId)).then(teams=>{if(this.lobby()?.sessionId===state.sessionId&&this.lobby()?.revision===state.revision)this.authorisedTeams.set(teams.map(team=>team.teamId));}).catch(()=>undefined);void import('qrcode').then(({default:qr})=>qr.toDataURL(new URL(state.joinUrl,location.origin).href,{width:200,margin:2})).then(image=>this.qr.set(image));}
 private async connect(){await this.hub?.stop();const lobby=this.lobby()!;const member=this.membership()!;const hub=new HubConnectionBuilder().withUrl('/hubs/race',{accessTokenFactory:()=>this.user()!.getIdToken()}).withAutomaticReconnect().build();this.hub=hub;hub.on('lobbySnapshot',(state:Lobby)=>{if(state.sessionId===lobby.sessionId)this.apply(state);});const rejoin=async()=>{const current=await hub.invoke<Joined>('SubscribeLobby',lobby.sessionId,member.deviceId);this.membership.set(current.membership);this.apply(current.lobby);this.connection.set('Connected');};hub.onreconnecting(()=>this.connection.set('Reconnecting'));hub.onreconnected(()=>{void rejoin().catch(()=>this.connection.set('Unavailable'));});hub.onclose(()=>this.connection.set('Disconnected'));await hub.start();await rejoin();}
 async claim(team:Team){await this.change('/claims',{teamId:team.teamId,deviceId:this.membership()!.deviceId});}
 async assign(team:Team,playerId:string,control:string){const player=this.lobby()!.players.find(p=>p.playerId===playerId);await this.change('/assignments',{teamId:team.teamId,playerId:playerId||null,deviceId:player?.sharedParticipant||control!=='Private'?this.membership()!.deviceId:null,control});}
 async approve(team:Team){await this.assign(team,team.playerId!,team.control);}
 async addParticipant(name:string){await this.change('/participants',{name,deviceId:this.membership()!.deviceId});}
 async help(input:{playerId:string;helpLevel:string;autoAcceptRecommendation:boolean}){await this.change("/players/"+encodeURIComponent(input.playerId)+"/assistance",input);}
 async pause(){await this.change(this.lobby()?.paused?"/resume":"/pause",{});}
 async revoke(deviceId:string){await this.change("/devices/"+encodeURIComponent(deviceId)+"/revoke",{});}
 async start(){await this.change('/start',{});}
 private async change(path:string,input:unknown){await this.run(async()=>this.apply(await this.request<Lobby>('/api/lobbies/'+this.lobby()!.sessionId+path,input)));}
 owner(team:Team){return this.lobby()!.players.find(p=>p.playerId===team.playerId)?.name??'Unclaimed · scripted at start';}
 canClaim(team:Team){return !this.busy()&&this.connection()==='Connected'&&this.lobby()?.lifecycle==='lobby'&&!team.playerId&&!this.lobby()!.teams.some(t=>t.playerId===this.membership()?.playerId);}
 canStart(){return !this.busy()&&this.connection()==='Connected'&&this.lobby()!.teams.some(t=>t.playerId)&&this.lobby()!.teams.every(t=>!t.playerId||t.approved);}
}
