import tracks from '../../../public/tracks/manifest.json';
import portraits from '../../../public/portraits/manifest.json';
import {Component,signal} from '@angular/core';
import type {User} from 'firebase/auth';
import {apiUrl} from '../api-url';
interface Event {eventId:string;order:number;trackId:string;trackName:string;trackSlug:string}
interface Team {teamId:string;constructorName:string;drivers:{personId:string;name:string;slug:string}[]}
interface Campaign {campaignId:string;lifecycle:string;contentVersion:string;calendarVersion:string;mode:string;calendar:Event[];teams:Team[];eventIndex:number;weekendId:string;canStart:boolean;readiness:{teamId:string;finalReady:boolean}[]}
interface Joined {campaign:Campaign;weekend:{lobby:{sessionId:string};membership:{deviceId:string}}}
@Component({selector:'app-campaign-page',templateUrl:'./campaign-page.html',styleUrl:'./campaign-page.css'})
export class CampaignPage {
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
 private async change(path:string,input:unknown){await this.run(async()=>{this.campaign.set(await this.request<Campaign>('/api/campaigns/'+this.campaign()!.campaignId+path,input));});}
}

