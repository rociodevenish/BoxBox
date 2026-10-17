import {Component,input,signal,OnInit,OnDestroy} from '@angular/core';
import {apiUrl} from '../api-url';
export interface OpeningOutcome {version:string;boundary:string;cars:{entryId:string;name:string;slug:string;teamId:string;gridPosition:number;position:number;positionsGained:number}[]}
interface OpeningState {phase:number;windowRevision:number;lifecycle:string;paused:boolean;opening:OpeningOutcome|null}

@Component({selector:'app-opening-handoff',templateUrl:'./opening-handoff.html',styleUrl:'./opening-handoff.css'})
export class OpeningHandoff implements OnInit,OnDestroy {
 readonly sessionId=input.required<string>();readonly racePath=input.required<string>();readonly host=input(false);
 readonly state=signal<OpeningState|null>(null);readonly revealing=signal(true);readonly error=signal('');readonly failed=signal<string[]>([]);readonly busy=signal(false);
 private timer?:ReturnType<typeof setTimeout>;private poll?:ReturnType<typeof setInterval>;private disposed=false;
 ngOnInit(){if(matchMedia('(prefers-reduced-motion: reduce)').matches)this.revealing.set(false);else this.timer=setTimeout(()=>this.revealing.set(false),3000);void this.refresh();this.poll=setInterval(()=>void this.refresh(),2000);}
 ngOnDestroy(){this.disposed=true;clearTimeout(this.timer);clearInterval(this.poll);}
 private async refresh(){try{const response=await fetch(apiUrl('/api/sessions/'+this.sessionId()),{cache:'no-store'});if(!response.ok)throw new Error('Opening unavailable. Refresh to recover the recorded result.');const state=await response.json() as OpeningState;if(!this.disposed)this.state.set(state);}catch(error){if(!this.disposed)this.error.set(error instanceof Error?error.message:'Opening unavailable.');}}
 portraitFailed(entry:string){this.failed.update(ids=>[...ids,entry]);}
 skip(){clearTimeout(this.timer);this.revealing.set(false);}
 async continue(){const state=this.state();if(!state||!this.host()||state.paused||this.busy())return;this.busy.set(true);this.error.set('');try{const {getAuth}=await import('firebase/auth');const user=getAuth().currentUser;if(!user)throw new Error('Sign in as the host to continue.');const response=await fetch(apiUrl('/api/lobbies/'+this.sessionId()+'/continue'),{method:'POST',headers:{'Content-Type':'application/json',Authorization:'Bearer '+await user.getIdToken()},body:JSON.stringify({commandId:crypto.randomUUID(),phase:state.phase,windowRevision:state.windowRevision})});if(!response.ok)throw new Error('Continue unavailable. Refresh to check the current race.');this.skip();await this.refresh();}catch(error){this.error.set(error instanceof Error?error.message:'Continue unavailable.');}finally{this.busy.set(false);}}
}
