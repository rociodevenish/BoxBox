import tracks from '../../../public/tracks/manifest.json';
import portraits from '../../../public/portraits/manifest.json';
import {Component,signal} from '@angular/core';
import type {User} from 'firebase/auth';
import {apiUrl} from '../api-url';
interface Event {eventId:string;order:number;trackId:string;trackName:string;trackSlug:string}
interface CreditEntry {commandId:string;credits:number;reason:string;personId:string|null;version:string|null;eventId:string|null}
interface EconomyTeam {personnelNames:Record<string,string>;teamId:string;balance:number;configuration:{version:string;creditsPerEvent:number};ledger:CreditEntry[]}
interface Team {teamId:string;constructorName:string;drivers:{personId:string;name:string;slug:string}[]}
interface Standing {id:string;position:number;points:number}
interface Result {eventId:string;result:{hash:string;classification:{personId:string;position:number;status:string;laps:number;eligible:boolean}[]};awards:{personId:string;points:number}[]}
interface PersonnelCard {id:string;name:string;role:string;price:number;effectLabel:string;attributes:Record<string,number>|null;driver?:{slug:string;ratings:Record<string,number>;preferences:{key:string;label:string}[]}}
interface AcquisitionTeam {advice:{version:string;personId:string;reason:string}|null;teamId:string;revision:number;balance:number;startingCredit:number;minimumCarReserve:number;slot:string|null;activeTeamId:string|null;poolVersion:string;priceVersion:string;offers:PersonnelCard[];hires:{slot:string;personId:string}[];ledger:{commandId:string;credits:number;reason:string;personId:string|null}[];requiredSlots:string[];roster:PersonnelCard[];scoutTokens:number;scoutUnavailable:string|null;cursor:number;totalPicks:number;drawCount:number;completionReserve:number;remainingSlots:string[];rosterReady:boolean;hireOptions:{personId:string;canHire:boolean;reserveAfterHire:number;unavailable:string|null}[];reserveVersion:string;constructionMapping:{version:string;sourceVersion:string;minimumBudgetUnits:number;creditsPerBudgetUnit:number}}
interface DevelopmentQuote {id:string;version:string;cost:number;option:{id:string;name:string;baseCost:number;attribute:string;minimumGain:number;maximumGain:number;stackingCap:number;compromise:string};outcomeRange:string;personnel:{rosterVersion:string;managerEfficiency:number;managerDevelopment:number;technicalEfficiency:number;technicalQuality:number}}
interface DevelopmentTeam {teamId:string;opportunityId:string;revision:number;assignmentRevision:number;balance:number;options:DevelopmentQuote[];draft:{revision:number;actionKind:string;optionId:string|null;quote:DevelopmentQuote|null};commitment:{actionKind:string;projectId:string;intendedEventId:string;quote:DevelopmentQuote|null}|null;timing:string}
interface Campaign {development?:{opportunityId:string;status:string;canClose:boolean;missingTeamPolicy:string;readiness:{teamId:string;committed:boolean;source:string}[]}|null;constructionReadiness?:{teamId:string;ready:boolean;source:string}[];constructionRequired:boolean;acquisition:{automaticEnabled:boolean;deadline:string|null;timeoutPolicy:string;version:string;activeTeamId:string|null;slot:string|null;requiredSlots:number;filledSlots:number;scheduleVersion:string;initialOrder:string[];picks:{teamId:string;slot:string}[];cursor:number;allRostersReady:boolean}|null;campaignId:string;lifecycle:string;contentVersion:string;calendarVersion:string;mode:string;calendar:Event[];teams:Team[];eventIndex:number;weekendId:string;canStart:boolean;readiness:{teamId:string;finalReady:boolean;source:string}[];points:{version:string;byPosition:number[]};standings:{drivers:Standing[];constructors:Standing[]};results:Result[];nextPreparation:{eventId:string;sessionId:string;trackName:string;canStart:boolean;missingTeamPolicy:string;readiness:{teamId:string;finalReady:boolean;source:string}[]}|null}
interface ConstructionOption {id:string;name:string;cost:number;summary:string;modifiers:Record<string,number>}
interface ConstructionRecommendation {version:string;optionIds:string[];cost:number;remainingCredits:number;explanation:string;affordableAlternatives:number}
interface ConstructionTeam {recommendation?:ConstructionRecommendation|null;confirmed:{id:string;cost:number;finalRevision:number;commandId:string}|null;teamId:string;catalogue:{version:string;sourceVersion:string;mappingVersion:string;categories:{id:string;label:string;options:ConstructionOption[]}[]};draft:{revision:number;optionIds:string[]};preview:{cost:number;balance:number;remainingCredits:number;affordable:boolean;ratings:Record<string,number>;groups:Record<string,number>;driverFits:{driverId:string;name:string;compatibility:number;description:string}[];tracks:{trackId:string;name:string;strengths:string[];compromises:string[];demandSummary:string}[];explanation:string}}
interface Joined {campaign:Campaign;weekend:{lobby:{sessionId:string};membership:{deviceId:string}}}
@Component({selector:'app-campaign-page',templateUrl:'./campaign-page.html',styleUrl:'./campaign-page.css'})
export class CampaignPage {
 readonly development=signal<DevelopmentTeam|null>(null);private developmentGeneration=0;private developmentDevice='';
 closeDevelopment(){this.developmentGeneration++;this.development.set(null);}
 async openDevelopment(teamId:string){this.closeEconomy();this.closePersonnel();this.closeConstruction();const generation=++this.developmentGeneration;this.development.set(null);await this.run(async()=>{
  const season=this.campaign()!;const joined=await this.request<Joined['weekend']>('/api/campaigns/'+season.campaignId+'/return',{nextEvent:true,deviceId:localStorage.getItem('household-device:'+this.user()!.uid+':'+season.nextPreparation!.sessionId)??this.device});
  this.remember(season.campaignId,joined,false);this.developmentDevice=joined.membership.deviceId;
  const own=await this.request<DevelopmentTeam>('/api/campaigns/'+season.campaignId+'/development/teams/'+teamId+'?deviceId='+encodeURIComponent(this.developmentDevice));if(generation===this.developmentGeneration)this.development.set(own);
 });}
 private developmentKey(kind:string){const own=this.development()!;return 'campaign-development:'+this.campaign()!.campaignId+':'+own.teamId+':'+own.opportunityId+':'+kind;}
 pendingDevelopment(kind:string){return !!this.development()&&localStorage.getItem(this.developmentKey(kind))!==null;}
 async draftDevelopment(optionId:string|null){await this.developmentCommand('draft',optionId);}
 async commitDevelopment(){await this.developmentCommand('commit');}
 private async developmentCommand(kind:'draft'|'commit',optionId?:string|null){if(this.busy()||!this.development())return;await this.run(async()=>{
  const own=this.development()!;const generation=this.developmentGeneration;const key=this.developmentKey(kind);let saved=localStorage.getItem(key);
  if(!saved){const quote=own.options.find(q=>q.option.id===optionId);saved=JSON.stringify({commandId:crypto.randomUUID(),opportunityId:own.opportunityId,revision:own.revision,assignmentRevision:own.assignmentRevision,...(kind==='commit'?{draftRevision:own.draft.revision,quoteId:own.draft.quote?.id??null}:{actionKind:optionId?'PermanentRefinement':'Save',optionId:optionId??null,quoteId:quote?.id??null})});localStorage.setItem(key,saved);}
  try{const updated=await this.request<DevelopmentTeam>('/api/campaigns/'+this.campaign()!.campaignId+'/development/teams/'+own.teamId+'/'+kind+'?deviceId='+encodeURIComponent(this.developmentDevice),JSON.parse(saved));if(generation===this.developmentGeneration)this.development.set(updated);}
  catch(error){const status=(error as {status?:number}).status;if(status&&status>=400&&status<500){localStorage.removeItem(key);if(status===403)this.closeDevelopment();}throw error;}
  localStorage.removeItem(key);this.campaign.set(await this.request<Campaign>('/api/campaigns/'+this.campaign()!.campaignId+'?deviceId='+encodeURIComponent(this.device)));
 });}
 readonly economy=signal<EconomyTeam|null>(null);private economyGeneration=0;
 async openEconomy(teamId:string){this.closeDevelopment();this.closePersonnel();this.closeConstruction();const generation=++this.economyGeneration;this.economy.set(null);await this.run(async()=>{const own=await this.request<EconomyTeam>('/api/campaigns/'+this.campaign()!.campaignId+'/economy/teams/'+teamId+'?deviceId='+encodeURIComponent(this.device));if(generation===this.economyGeneration)this.economy.set(own);});}
 closeEconomy(){this.economyGeneration++;this.economy.set(null);}
 creditLabel(entry:CreditEntry){return entry.reason==='starting-credit'?'Starting credit':entry.reason==='base-car-construction'?'Base Car construction':entry.reason==='event-income'?'Event income · '+this.reportName(entry.eventId!):entry.reason==='personnel-hire'?'Personnel hire · '+(this.economy()?.personnelNames[entry.personId!]??entry.personId):entry.reason;}
 creditBalance(index:number){return this.economy()!.ledger.slice(0,index+1).reduce((balance,entry)=>balance+entry.credits,0);}
 driverName(id:string){return this.campaign()?.teams.flatMap(t=>t.drivers).find(d=>d.personId===id)?.name??id;}
 teamName(id:string){return this.campaign()?.teams.find(t=>t.teamId===id)?.constructorName??id;}
 awarded(result:Result,id:string){return result.awards.find(a=>a.personId===id)?.points??0;}
 reportName(id:string){return this.campaign()?.calendar.find(e=>e.eventId===id)?.trackName??id;}
 hasReport(id:string){return this.campaign()?.results.some(r=>r.eventId===id);}
 previousPoints(result:Result,id:string){let total=0;for(const report of this.campaign()?.results??[]){if(report.eventId===result.eventId)break;total+=this.awarded(report,id);}return total;}
 trackImage(id:string){return tracks.tracks.find(track=>track.id===id)?.url??'';}
 portrait(id:string){const asset=portraits.portraits.find(driver=>'driver-'+driver.driverId===id);return asset?'/'+asset.runtimeUrl:'';}
 readonly failedConstructionAssets=signal<string[]>([]);
 constructionAssetFailed(id:string){this.failedConstructionAssets.update(ids=>ids.includes(id)?ids:[...ids,id]);}
 readonly constructionHelp=signal<'Assisted'|'Guided'>('Assisted');readonly construction=signal<ConstructionTeam|null>(null);readonly constructionSelections=signal<string[]>([]);private constructionGeneration=0;
 readonly personnel=signal<AcquisitionTeam|null>(null);readonly controlledTeams=signal<string[]>([]);
 readonly campaign=signal<Campaign|null>(null);readonly user=signal<User|null>(null);readonly error=signal('');readonly busy=signal(false);readonly ready=signal(false);
 private device='';private personnelGeneration=0;private creation=localStorage.getItem('campaign-create-command')??crypto.randomUUID();private start=crypto.randomUUID();
 constructor(){document.addEventListener('visibilitychange',()=>{if(document.hidden){this.closeDevelopment();this.closeEconomy();this.closePersonnel();this.closeConstruction();}});void this.run(async()=>{
  const config=await this.request<{projectId:string;apiKey:string;emulatorUrl:string|null}>('/api/auth/config');
  const {initializeApp}=await import('firebase/app');const sdk=await import('firebase/auth');const auth=sdk.getAuth(initializeApp({projectId:config.projectId,apiKey:config.apiKey}));
  if(config.emulatorUrl)sdk.connectAuthEmulator(auth,config.emulatorUrl,{disableWarnings:true});
  await new Promise<void>(resolve=>{const off=sdk.onAuthStateChanged(auth,user=>{this.user.set(user);off();resolve();});});this.ready.set(true);
  const id=location.pathname.split('/')[2];if(id&&id!=='new'&&this.user()){
   const weekend=localStorage.getItem('campaign-weekend:'+id);this.device=weekend?localStorage.getItem('household-device:'+this.user()!.uid+':'+weekend)??'':'';
   const joined=await this.request<Joined['weekend']>('/api/campaigns/'+id+'/return',{deviceId:this.device||null});this.remember(id,joined);
   this.campaign.set(await this.request<Campaign>('/api/campaigns/'+id+'?deviceId='+encodeURIComponent(this.device)));await this.loadControllers();
  }
 });}
 private async request<T>(path:string,input?:unknown):Promise<T>{const token=await this.user()?.getIdToken();const response=await fetch(apiUrl(path),{method:input===undefined?'GET':'POST',headers:{'Content-Type':'application/json',...(token?{Authorization:'Bearer '+token}:{})},body:input===undefined?undefined:JSON.stringify(input)});const result=await response.json();if(!response.ok)throw Object.assign(new Error(result.message??'Campaign unavailable. Retry.'),{status:response.status});return result;}
 private async run(action:()=>Promise<void>){this.busy.set(true);this.error.set('');try{await action();}catch(error){this.error.set(error instanceof Error?error.message:'Campaign unavailable.');}finally{this.busy.set(false);}}
 async create(name:string,mode:string,personnelMode="preset"){await this.run(async()=>{
  localStorage.setItem('campaign-create-command',this.creation);
  const joined=await this.request<Joined>('/api/campaigns',{commandId:this.creation,name,mode,personnelMode,calendar:'short',seed:123,laps:6});
  this.device=joined.weekend.membership.deviceId;localStorage.setItem('household-device:'+this.user()!.uid+':'+joined.weekend.lobby.sessionId,this.device);
  localStorage.setItem('campaign-weekend:'+joined.campaign.campaignId,joined.weekend.lobby.sessionId);localStorage.removeItem('campaign-create-command');
  history.replaceState(null,'','/campaigns/'+joined.campaign.campaignId);this.campaign.set(joined.campaign);await this.loadControllers();
 });}
 async claim(teamId:string){await this.run(async()=>{await this.request('/api/lobbies/'+this.campaign()!.weekendId+'/claims',{teamId,deviceId:this.device});await this.loadControllers();});}
 private async loadControllers(){const season=this.campaign();if(!season)return;const teams=await this.request<{teamId:string}[]>('/api/lobbies/'+season.weekendId+'/controllers?deviceId='+encodeURIComponent(this.device));this.controlledTeams.set(teams.map(t=>t.teamId));}
 async openPersonnel(teamId:string){this.closeDevelopment();const generation=++this.personnelGeneration;this.personnel.set(null);await this.run(async()=>{const own=await this.request<AcquisitionTeam>('/api/campaigns/'+this.campaign()!.campaignId+'/acquisition/teams/'+teamId+'?deviceId='+encodeURIComponent(this.device));if(generation===this.personnelGeneration)this.personnel.set(own);});}
 closePersonnel(){this.personnelGeneration++;this.personnel.set(null);}
 attributes(card:PersonnelCard){return Object.entries(card.driver?.ratings??card.attributes??{});}
 attributeLabel(key:string){const label=key.replace(/([a-z])([A-Z])/g,'$1 $2');return label.charAt(0).toUpperCase()+label.slice(1);}
 hireOption(id:string){return this.personnel()?.hireOptions.find(option=>option.personId===id);}
 rosterSlot(id:string){return this.slotLabel(this.personnel()?.hires.find(h=>h.personId===id)?.slot??'');}
 filledSlot(slot:string){return this.personnel()?.hires.some(h=>h.slot===slot)??false;}
 slotLabel(slot:string){return slot.replaceAll('-',' ');}
 private hireKey(){return 'campaign-hire:'+this.campaign()!.campaignId+':'+this.personnel()!.teamId;}
 pendingHire(){return this.personnel()?localStorage.getItem(this.hireKey())!==null:false;}
 async hire(personId?:string){await this.run(async()=>{
  if(await this.personnelCommand('hire',personId?{personId}:undefined))this.campaign.set(await this.request<Campaign>('/api/campaigns/'+this.campaign()!.campaignId+'?deviceId='+encodeURIComponent(this.device)));
 });}
 private scoutKey(){return 'campaign-scout:'+this.campaign()!.campaignId+':'+this.personnel()!.teamId;}
 pendingScout(){return this.personnel()?localStorage.getItem(this.scoutKey())!==null:false;}
 async scout(){await this.run(async()=>{await this.personnelCommand('scout',{});});}
 private async personnelCommand(kind:'hire'|'scout',input?:{personId?:string}):Promise<boolean>{
  const own=this.personnel()!;const generation=this.personnelGeneration;const key=kind==='hire'?this.hireKey():this.scoutKey();let saved=localStorage.getItem(key);
  if(!saved){if(!input)return false;saved=JSON.stringify({commandId:crypto.randomUUID(),revision:own.revision,teamId:own.teamId,...input});localStorage.setItem(key,saved);}
  try{const updated=await this.request<AcquisitionTeam>('/api/campaigns/'+this.campaign()!.campaignId+'/acquisition/teams/'+own.teamId+'/'+kind+'?deviceId='+encodeURIComponent(this.device),JSON.parse(saved));if(generation===this.personnelGeneration)this.personnel.set(updated);}catch(error){const status=(error as {status?:number}).status;if(status&&status>=400&&status<500)localStorage.removeItem(key);throw error;}
  localStorage.removeItem(key);return true;
 }
 baseCarReady(teamId:string){return this.campaign()?.constructionReadiness?.some(team=>team.teamId===teamId&&team.ready)??false;}
 async activatePersonnel(){await this.change('/acquisition/activate',{});}
 pendingAutomaticConstruction(){return !!this.campaign()&&localStorage.getItem('campaign-car-automatic:'+this.campaign()!.campaignId)!==null;}
 async automaticConstruction(){if(this.busy())return;this.closeConstruction();await this.run(async()=>{const key='campaign-car-automatic:'+this.campaign()!.campaignId;const commandId=localStorage.getItem(key)??crypto.randomUUID();localStorage.setItem(key,commandId);try{this.campaign.set(await this.request<Campaign>('/api/campaigns/'+this.campaign()!.campaignId+'/construction/automatic',{commandId}));localStorage.removeItem(key);}catch(error){const status=(error as {status?:number}).status;if(status&&status>=400&&status<500)localStorage.removeItem(key);throw error;}});}
 async prepare(){await this.change('/prepare',{});}
 async refresh(){this.closeDevelopment();this.closeEconomy();this.closePersonnel();this.closeConstruction();await this.run(async()=>{this.campaign.set(await this.request<Campaign>('/api/campaigns/'+this.campaign()!.campaignId+'?deviceId='+encodeURIComponent(this.device)));await this.loadControllers();});}
 async begin(){await this.change('/start',{commandId:this.start});}
 private remember(id:string,joined:Joined['weekend'],current=true){if(current){this.device=joined.membership.deviceId;localStorage.setItem('campaign-weekend:'+id,joined.lobby.sessionId);}localStorage.setItem('household-device:'+this.user()!.uid+':'+joined.lobby.sessionId,joined.membership.deviceId);localStorage.setItem('weekend-campaign:'+joined.lobby.sessionId,id);}
 async prepareNext(){await this.change('/next/prepare',{});}
 async prepareAbsent(teamId:string,nextEvent=false){await this.change('/absent/prepare',{teamId,nextEvent});}
 async openNext(){await this.run(async()=>{const id=this.campaign()!.campaignId;const joined=await this.request<Joined['weekend']>('/api/campaigns/'+id+'/return',{nextEvent:true});this.remember(id,joined,false);location.href='/shared/lobby/'+joined.lobby.sessionId;});}
 async startNext(){await this.change('/next/start',{commandId:this.start});const season=this.campaign();if(season){const device=localStorage.getItem('household-device:'+this.user()!.uid+':'+season.weekendId);if(device){this.device=device;localStorage.setItem('campaign-weekend:'+season.campaignId,season.weekendId);}}}
 private async change(path:string,input:unknown){await this.run(async()=>{this.campaign.set(await this.request<Campaign>('/api/campaigns/'+this.campaign()!.campaignId+path,input));});}
 async openConstruction(teamId:string){this.closeDevelopment();this.closePersonnel();const generation=++this.constructionGeneration;this.construction.set(null);await this.run(async()=>{const own=await this.request<ConstructionTeam>('/api/campaigns/'+this.campaign()!.campaignId+'/construction/teams/'+teamId+'?deviceId='+encodeURIComponent(this.device));if(generation===this.constructionGeneration){this.construction.set(own);this.constructionSelections.set(own.draft.optionIds);}});}
 closeConstruction(){this.constructionGeneration++;this.construction.set(null);this.constructionSelections.set([]);}
 constructionOption(index:number){const own=this.construction();return own?.catalogue.categories[index].options.find(o=>o.id===this.constructionSelections()[index]);}
 chooseConstruction(index:number,id:string){if(this.busy()||this.pendingDraft()||this.pendingConfirmation()||this.pendingRecommendation()||this.construction()?.confirmed)return;this.constructionSelections.update(values=>values.map((value,i)=>i===index?id:value));}
 constructionCost(){return this.constructionSelections().reduce((cost,_,i)=>cost+(this.constructionOption(i)?.cost??0),0);}
 constructionAttributes(values:Record<string,number>){return Object.entries(values);}
 private draftKey(){return 'campaign-car-draft:'+this.campaign()!.campaignId+':'+this.construction()!.teamId;}
 pendingDraft(){return this.construction()?localStorage.getItem(this.draftKey())!==null:false;}
 pendingConfirmation(){return this.construction()?localStorage.getItem(this.draftKey()+':confirm')!==null:false;}
 pendingRecommendation(){return this.construction()?localStorage.getItem(this.draftKey()+':recommend')!==null:false;}
 recommendedChoice(index:number){const own=this.construction();return own?.catalogue.categories[index].options.find(option=>option.id===own.recommendation?.optionIds[index])?.name??'No recommendation';}
 async recommendConstruction(){if(!this.construction()||this.busy()||this.construction()!.confirmed||this.pendingDraft()||this.pendingConfirmation())return;await this.run(async()=>{
  const own=this.construction()!;const generation=this.constructionGeneration;const key=this.draftKey()+':recommend';let saved=localStorage.getItem(key);
  if(!saved){saved=JSON.stringify({commandId:crypto.randomUUID(),revision:own.draft.revision,teamId:own.teamId,catalogueVersion:own.catalogue.version});localStorage.setItem(key,saved);}
  try{const next=await this.request<ConstructionTeam>('/api/campaigns/'+this.campaign()!.campaignId+'/construction/teams/'+own.teamId+'/recommend?deviceId='+encodeURIComponent(this.device),JSON.parse(saved));if(generation===this.constructionGeneration){this.construction.set(next);this.constructionSelections.set(next.draft.optionIds);}}
  catch(error){const status=(error as {status?:number}).status;if(status&&status>=400&&status<500){localStorage.removeItem(key);if(status===403&&generation===this.constructionGeneration)this.closeConstruction();}throw error;}
  localStorage.removeItem(key);
 });}
 constructionChanged(){return this.constructionSelections().some((id,index)=>id!==this.construction()?.draft.optionIds[index]);}
 async confirmConstruction(){await this.run(async()=>{
  const own=this.construction()!;const generation=this.constructionGeneration;const key=this.draftKey()+':confirm';let saved=localStorage.getItem(key);
  if(!saved){saved=JSON.stringify({commandId:crypto.randomUUID(),revision:own.draft.revision,teamId:own.teamId,catalogueVersion:own.catalogue.version});localStorage.setItem(key,saved);}
  try{const next=await this.request<ConstructionTeam>('/api/campaigns/'+this.campaign()!.campaignId+'/construction/teams/'+own.teamId+'/confirm?deviceId='+encodeURIComponent(this.device),JSON.parse(saved));if(generation===this.constructionGeneration){this.construction.set(next);this.constructionSelections.set(next.draft.optionIds);}}
  catch(error){const status=(error as {status?:number}).status;if(status&&status>=400&&status<500)localStorage.removeItem(key);throw error;}
  localStorage.removeItem(key);
  this.campaign.set(await this.request<Campaign>('/api/campaigns/'+this.campaign()!.campaignId+'?deviceId='+encodeURIComponent(this.device)));
 });} async saveConstruction(){await this.run(async()=>{
  const own=this.construction()!;const generation=this.constructionGeneration;const key=this.draftKey();let saved=localStorage.getItem(key);
  if(!saved){saved=JSON.stringify({commandId:crypto.randomUUID(),revision:own.draft.revision,teamId:own.teamId,catalogueVersion:own.catalogue.version,optionIds:this.constructionSelections()});localStorage.setItem(key,saved);}
  try{const next=await this.request<ConstructionTeam>('/api/campaigns/'+this.campaign()!.campaignId+'/construction/teams/'+own.teamId+'/draft?deviceId='+encodeURIComponent(this.device),JSON.parse(saved));if(generation===this.constructionGeneration){this.construction.set(next);this.constructionSelections.set(next.draft.optionIds);}}
  catch(error){const status=(error as {status?:number}).status;if(status&&status>=400&&status<500)localStorage.removeItem(key);throw error;}
  localStorage.removeItem(key);
 });}}
