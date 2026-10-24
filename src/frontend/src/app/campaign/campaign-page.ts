import tracks from '../../../public/tracks/manifest.json';
import portraits from '../../../public/portraits/manifest.json';
import {Component,signal} from '@angular/core';
import type {User} from 'firebase/auth';
import {apiUrl} from '../api-url';
interface Event {eventId:string;order:number;trackId:string;trackName:string;trackSlug:string}
interface Team {teamId:string;constructorName:string;drivers:{personId:string;name:string;slug:string}[]}
interface Standing {id:string;position:number;points:number}
interface Result {eventId:string;result:{hash:string;classification:{personId:string;position:number;status:string;laps:number;eligible:boolean}[]};awards:{personId:string;points:number}[]}
interface Campaign {campaignId:string;lifecycle:string;contentVersion:string;calendarVersion:string;mode:string;calendar:Event[];teams:Team[];eventIndex:number;weekendId:string;canStart:boolean;readiness:{teamId:string;finalReady:boolean}[];points:{version:string;byPosition:number[]};standings:{drivers:Standing[];constructors:Standing[]};results:Result[];nextPreparation:{eventId:string;sessionId:string;trackName:string;canStart:boolean;missingTeamPolicy:string;readiness:{teamId:string;finalReady:boolean}[]}|null}
interface Joined {campaign:Campaign;weekend:{lobby:{sessionId:string};membership:{deviceId:string}}}
@Component({selector:'app-campaign-page',templateUrl:'./campaign-page.html',styleUrl:'./campaign-page.css'})
export class CampaignPage {
 driverName(id:string){return this.campaign()?.teams.flatMap(t=>t.drivers).find(d=>d.personId===id)?.name??id;}
 teamName(id:string){return this.campaign()?.teams.find(t=>t.teamId===id)?.constructorName??id;}
 awarded(result:Result,id:string){return result.awards.find(a=>a.personId===id)?.points??0;}
 reportName(id:string){return this.campaign()?.calendar.find(e=>e.eventId===id)?.trackName??id;}
 hasReport(id:string){return this.campaign()?.results.some(r=>r.eventId===id);}
 previousPoints(result:Result,id:string){let total=0;for(const report of this.campaign()?.results??[]){if(report.eventId===result.eventId)break;total+=this.awarded(report,id);}return total;}
 trackImage(id:string){return tracks.tracks.find(track=>track.id===id)?.url??'';}
 portrait(id:string){const asset=portraits.portraits.find(driver=>'driver-'+driver.driverId===id);return asset?'/'+asset.runtimeUrl:'';}
 readonly campaign=signal<Campaign|null>(null);readonly user=signal<User|null>(null);readonly error=signal('');readonly busy=signal(false);readonly ready=signal(false);
 private device='';private creation=localStorage.getItem('campaign-create-command')??crypto.randomUUID();private start=crypto.randomUUID();
 constructor(){void this.run(async()=>{
  const config=await this.request<{projectId:string;apiKey:string;emulatorUrl:string|null}>('/api/auth/config');
  const {initializeApp}=await import('firebase/app');const sdk=await import('firebase/auth');const auth=sdk.getAuth(initializeApp({projectId:config.projectId,apiKey:config.apiKey}));
  if(config.emulatorUrl)sdk.connectAuthEmulator(auth,config.emulatorUrl,{disableWarnings:true});
  await new Promise<void>(resolve=>{const off=sdk.onAuthStateChanged(auth,user=>{this.user.set(user);off();resolve();});});this.ready.set(true);
  const id=location.pathname.split('/')[2];if(id&&id!=='new'&&this.user()){
   const weekend=localStorage.getItem('campaign-weekend:'+id);this.device=weekend?localStorage.getItem('household-device:'+this.user()!.uid+':'+weekend)??'':'';
   const joined=await this.request<Joined['weekend']>('/api/campaigns/'+id+'/return',{deviceId:this.device||null});this.remember(id,joined);
   this.campaign.set(await this.request<Campaign>('/api/campaigns/'+id+'?deviceId='+encodeURIComponent(this.device)));
  }
 });}
 private async request<T>(path:string,input?:unknown):Promise<T>{const token=await this.user()?.getIdToken();const response=await fetch(apiUrl(path),{method:input===undefined?'GET':'POST',headers:{'Content-Type':'application/json',...(token?{Authorization:'Bearer '+token}:{})},body:input===undefined?undefined:JSON.stringify(input)});const result=await response.json();if(!response.ok)throw new Error(result.message??'Campaign unavailable. Retry.');return result;}
 private async run(action:()=>Promise<void>){this.busy.set(true);this.error.set('');try{await action();}catch(error){this.error.set(error instanceof Error?error.message:'Campaign unavailable.');}finally{this.busy.set(false);}}
 async create(name:string,mode:string){await this.run(async()=>{
  localStorage.setItem('campaign-create-command',this.creation);
  const joined=await this.request<Joined>('/api/campaigns',{commandId:this.creation,name,mode,calendar:'short',seed:123,laps:6});
  this.device=joined.weekend.membership.deviceId;localStorage.setItem('household-device:'+this.user()!.uid+':'+joined.weekend.lobby.sessionId,this.device);
  localStorage.setItem('campaign-weekend:'+joined.campaign.campaignId,joined.weekend.lobby.sessionId);localStorage.removeItem('campaign-create-command');
  history.replaceState(null,'','/campaigns/'+joined.campaign.campaignId);this.campaign.set(joined.campaign);
 });}
 async claim(teamId:string){await this.run(async()=>{await this.request('/api/lobbies/'+this.campaign()!.weekendId+'/claims',{teamId,deviceId:this.device});});}
 async prepare(){await this.change('/prepare',{});}
 async refresh(){await this.run(async()=>{this.campaign.set(await this.request<Campaign>('/api/campaigns/'+this.campaign()!.campaignId+'?deviceId='+encodeURIComponent(this.device)));});}
 async begin(){await this.change('/start',{commandId:this.start});}
 private remember(id:string,joined:Joined['weekend'],current=true){if(current){this.device=joined.membership.deviceId;localStorage.setItem('campaign-weekend:'+id,joined.lobby.sessionId);}localStorage.setItem('household-device:'+this.user()!.uid+':'+joined.lobby.sessionId,joined.membership.deviceId);localStorage.setItem('weekend-campaign:'+joined.lobby.sessionId,id);}
 async prepareNext(){await this.change('/next/prepare',{});}
 async openNext(){await this.run(async()=>{const id=this.campaign()!.campaignId;const joined=await this.request<Joined['weekend']>('/api/campaigns/'+id+'/return',{nextEvent:true});this.remember(id,joined,false);location.href='/shared/lobby/'+joined.lobby.sessionId;});}
 async startNext(){await this.change('/next/start',{commandId:this.start});const season=this.campaign();if(season){const device=localStorage.getItem('household-device:'+this.user()!.uid+':'+season.weekendId);if(device){this.device=device;localStorage.setItem('campaign-weekend:'+season.campaignId,season.weekendId);}}}
 private async change(path:string,input:unknown){await this.run(async()=>{this.campaign.set(await this.request<Campaign>('/api/campaigns/'+this.campaign()!.campaignId+path,input));});}
}
