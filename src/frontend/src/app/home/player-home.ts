import {Component,signal} from '@angular/core';
import type {Auth,User,Unsubscribe} from 'firebase/auth';
import {CampaignResume} from './campaign-resume';
import {apiUrl} from '../api-url';
@Component({selector:'app-player-home',imports:[CampaignResume],templateUrl:'./player-home.html',styleUrl:'./player-home.css'})
export class PlayerHome {
 readonly user=signal<User|null>(null);
 readonly ready=signal(false);
 readonly error=signal('');
 private auth?:Auth;
 private unsubscribe?:Unsubscribe;
 private destroyed=false;
 constructor(){void this.loadAccount();}
 async loadAccount(){
  this.error.set('');this.ready.set(false);
  try{
   const response=await fetch(apiUrl('/api/auth/config'));
   if(!response.ok)throw new Error('Account unavailable');
   const config=await response.json() as {projectId:string;apiKey:string;emulatorUrl:string|null};
   const app=await import('firebase/app');const sdk=await import('firebase/auth');
   if(this.destroyed)return;
   const existing=app.getApps().length>0;
   this.auth=sdk.getAuth(existing?app.getApp():app.initializeApp({projectId:config.projectId,apiKey:config.apiKey}));
   if(config.emulatorUrl&&!existing)sdk.connectAuthEmulator(this.auth,config.emulatorUrl,{disableWarnings:true});
   this.unsubscribe?.();this.unsubscribe=sdk.onAuthStateChanged(this.auth,user=>{this.user.set(user?.isAnonymous?null:user);this.ready.set(true);});
  }catch{if(!this.destroyed){this.ready.set(true);this.error.set('Account details unavailable. You can still join with a code.');}}
 }
 quickJoin(code:string){const value=code.trim();if(value)location.assign('/shared/join?code='+encodeURIComponent(value));}
 async signOut(){try{const sdk=await import('firebase/auth');await sdk.signOut(this.auth!);}catch{this.error.set('Unable to sign out. Try again.');}}
 ngOnDestroy(){this.destroyed=true;this.unsubscribe?.();}
}
