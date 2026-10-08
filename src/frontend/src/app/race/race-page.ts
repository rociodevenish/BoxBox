import { Component, OnDestroy, signal, viewChild, ElementRef } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { TimingTower } from '../ui/timing/timing-tower';
import { PublicClassification, PublicGap, TyreCompound } from '../ui/timing/public-classification.model';
interface PublicState { sessionId:string; phase:number; revision:number; lifecycle:string; leaderLap:number; totalLaps:number; classification:{driverId:string; name:string; teamId:string; position:number; gapToLeader:PublicGap; compound:TyreCompound; pitState:string}[]; events:{id:string; kind:string; driverId:string; lap:number}[] }
interface TeamState {phase:number;revision:number;lifecycle:string;cars:{driverId:string;compound:TyreCompound;tyreAge:number;estimatedTyreCondition:number;inPits:boolean;stopQueued:boolean;battleOptions:string[]}[]}
interface Choice { driverId:string; pace:string; pit:string; compound:string; battle:string }
interface Created {displayUrl:string;controllerUrl:string}
@Component({selector:'app-race-page',imports:[TimingTower],templateUrl:'./race-page.html',styleUrl:'./race-page.css'})
export class RacePage implements OnDestroy {
 readonly mode=location.pathname.split('/')[2];
 readonly id=location.pathname.split('/')[3];
 readonly capability=new URLSearchParams(location.hash.slice(1)).get('capability') ?? '';
 readonly created=signal<Created|null>(null); readonly publicState=signal<PublicState|null>(null); readonly team=signal<TeamState|null>(null);
 readonly error=signal(''); readonly busy=signal(false); readonly choices=signal<Choice[]>([]);
 readonly dialog=viewChild<ElementRef<HTMLDialogElement>>('confirmation');
 private connection?:HubConnection;
 constructor(){if(this.mode!=='new')void this.connect();}
 async create(preset:string,laps:string,seed:string){this.error.set('');try{this.created.set(await this.request<Created>('/api/sessions',{preset,laps:Number(laps),seed:Number(seed)}));}catch(error){this.fail(error);}}
 async connect(){try{
  const connection=new HubConnectionBuilder().withUrl('/hubs/race').build();this.connection=connection;
  connection.on('publicSnapshot',(state:PublicState)=>this.publicState.set(state));
  connection.on('teamSnapshot',(state:TeamState)=>this.acceptTeam(state));
  await connection.start();
  this.publicState.set(await connection.invoke<PublicState>('SubscribePublic',this.id));
  if(this.mode==='controller')this.acceptTeam(await connection.invoke<TeamState>('SubscribeTeam',this.id,this.capability));
 }catch(error){this.fail(error);}}
 private acceptTeam(state:TeamState){if(!this.team())this.choices.set(state.cars.map(car=>({driverId:car.driverId,pace:'Standard',pit:'StayOut',compound:'Hard',battle:'Neutral'})));this.team.set(state);}
 editable(){return !this.busy() && this.team()?.lifecycle==='awaiting-decisions';}
 choose(index:number,key:'pace'|'pit'|'compound'|'battle',event:Event){const target=event.target;if(target instanceof HTMLSelectElement)this.choices.update(items=>items.map((item,i)=>i===index?{...item,[key]:target.value}:item));}
 review(){this.dialog()?.nativeElement.showModal();}
 close(){this.dialog()?.nativeElement.close();}
 async submit(){this.close();const team=this.team();if(!team || !this.editable())return;this.busy.set(true);this.error.set('');try{await this.request('/api/sessions/'+this.id+'/plans',{commandId:crypto.randomUUID(),phase:team.phase,revision:team.revision,cars:this.choices().map(car=>({...car,pit:car.pit==='Pit'?'Pit':'Stay Out',compound:car.pit==='Pit'?car.compound:null}))},this.capability);}catch(error){this.fail(error);}finally{this.busy.set(false);}}
 classification():PublicClassification {const state=this.publicState()!;return {phase:state.phase,leaderLap:state.leaderLap,totalLaps:state.totalLaps,rows:state.classification.map(car=>({position:car.position,driver:{id:car.driverId,code:car.driverId.slice(-2),name:car.name,team:{name:car.teamId,accent:'#ff685b'}},gap:car.gapToLeader,compound:car.compound,stopStatus:car.pitState==='in-pit-lane'?'in-pit-lane':'on-track'}))};}
 private async request<T>(url:string,body:unknown,capability?:string):Promise<T>{const response=await fetch(url,{method:'POST',headers:{'Content-Type':'application/json',...(capability?{Authorization:'Bearer '+capability}:{})},body:JSON.stringify(body)});const value=await response.json();if(!response.ok)throw new Error(value.message??'Request failed');return value;}
 private fail(error:unknown){this.error.set(error instanceof Error?error.message:'Unable to connect. Create a fresh session if the server restarted.');}
 ngOnDestroy(){void this.connection?.stop();}
}

