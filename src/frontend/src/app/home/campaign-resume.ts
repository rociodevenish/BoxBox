import {Component,Input,OnDestroy,signal} from '@angular/core';
import type {User} from 'firebase/auth';
import {apiUrl} from '../api-url';
interface Summary {campaignId:string;hostName:string;role:'host'|'member';lifecycle:string;eventNumber:number;trackName:string}
@Component({selector:'app-campaign-resume',templateUrl:'./campaign-resume.html',styleUrl:'./campaign-resume.css'})
export class CampaignResume implements OnDestroy {
 private account:User|null=null;private generation=0;private request:AbortController|null=null;
 readonly summaries=signal<Summary[]>([]);readonly state=signal<'signed-out'|'loading'|'ready'|'error'>('signed-out');
 @Input() set user(value:User|null){if(this.account===value)return;this.account=value;this.clear();if(value&&!value.isAnonymous)void this.load();}
 private clear(){this.generation++;this.request?.abort();this.request=null;this.summaries.set([]);this.state.set('signed-out');}
 ngOnDestroy(){this.clear();}
 hosted(){return this.summaries().filter(campaign=>campaign.role==='host');}
 joined(){return this.summaries().filter(campaign=>campaign.role==='member');}
 context(campaign:Summary){return campaign.lifecycle==='season-complete'?'Completed · Results available':'Event '+campaign.eventNumber+' · '+campaign.trackName+' · '+this.lifecycle(campaign.lifecycle);}
 private lifecycle(value:string){return ({created:'Ready to prepare',acquiring:'Choosing personnel',constructing:'Building cars','event-preparation':'Preparing','active-weekend':'Weekend active','event-result-committed':'Event results ready'} as Record<string,string>)[value]??'In progress';}
 async load(){
  const account=this.account;if(!account||account.isAnonymous)return;const generation=++this.generation;this.request?.abort();const request=new AbortController();this.request=request;this.summaries.set([]);this.state.set('loading');
  try{
   const token=await account.getIdToken();if(generation!==this.generation||this.account!==account)return;
   const response=await fetch(apiUrl('/api/campaigns'),{headers:{Authorization:'Bearer '+token},cache:'no-store',signal:request.signal});
   if(!response.ok)throw new Error('Campaign listing unavailable');const summaries=await response.json() as Summary[];
   if(generation!==this.generation||this.account!==account)return;this.summaries.set(summaries);this.state.set('ready');
  }catch{if(generation===this.generation&&this.account===account)this.state.set('error');}
 }
}
