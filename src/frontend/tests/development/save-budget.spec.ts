import {test,expect} from '@playwright/test';
import {randomUUID} from 'node:crypto';
import {apiPort,captures} from '../../playwright.runtime';

test('shared household retains expensive preseason spending, rejects overspend and commits explicit Save',async({page,request})=>{
 test.skip(process.env['F1_DEVELOPMENT_DELAY_FIXTURE']==='1','The separate delay flow covers the forced outcome contract.');
 test.setTimeout(180000);await page.setViewportSize({width:360,height:800});await page.goto('/shared/new');
 const email='economy-save-'+Date.now()+'@example.test',password='Local-test-password-123';
 await page.getByLabel('Your name').fill('Budget household');await page.getByLabel('Email').fill(email);await page.getByLabel('Password').fill(password);await page.getByRole('button',{name:'Create account',exact:true}).click();await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 const config=await(await request.get('/api/auth/config')).json();
 const login=await request.post(config.emulatorUrl+'/identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key='+config.apiKey,{data:{email,password,returnSecureToken:true}});expect(login.ok()).toBeTruthy();const identity=await login.json();const headers={Authorization:'Bearer '+identity.idToken};
 const get=async(path:string)=>{const response=await request.get(path,{headers});expect(response.ok(),await response.text()).toBeTruthy();return response.json();};
 const post=async(path:string,data:unknown)=>{const response=await request.post(path,{headers,data});expect(response.ok(),await response.text()).toBeTruthy();return response.json();};
 const joined=await post('/api/campaigns',{commandId:randomUUID(),name:'Budget household',mode:'Mixed',personnelMode:'acquisition',seed:101,laps:6});
 const id=joined.campaign.campaignId,first=joined.campaign.weekendId,device=joined.weekend.membership.deviceId;
 for(let i=1;i<=11;i++){
  const name='Shared participant '+i,lobby=await post('/api/lobbies/'+first+'/participants',{name,deviceId:device});
  const player=lobby.players.find((p:{name:string})=>p.name===name);
  await post('/api/lobbies/'+first+'/assignments',{teamId:'team-'+String(i).padStart(2,'0'),playerId:player.playerId,deviceId:device,control:'SharedTablet'});
 }
 for(let i=0;i<77;i++){
  const state=await get('/api/campaigns/'+id+'?deviceId='+device),team=state.acquisition.activeTeamId;
  const own=await get('/api/campaigns/'+id+'/acquisition/teams/'+team+'?deviceId='+device);
  const legal=own.offers.filter((p:{id:string})=>own.hireOptions.some((o:{personId:string;canHire:boolean})=>o.personId===p.id&&o.canHire));
  legal.sort((a:{price:number;id:string},b:{price:number;id:string})=>(team==='team-01'?b.price-a.price:a.price-b.price)||a.id.localeCompare(b.id));
  await post('/api/campaigns/'+id+'/acquisition/teams/'+team+'/hire?deviceId='+device,{commandId:randomUUID(),revision:own.revision,teamId:team,personId:legal[0].id});
 }
 for(let i=1;i<=11;i++){
  const team='team-'+String(i).padStart(2,'0'),own=await get('/api/campaigns/'+id+'/construction/teams/'+team+'?deviceId='+device);
  await post('/api/campaigns/'+id+'/construction/teams/'+team+'/confirm?deviceId='+device,{commandId:randomUUID(),revision:own.draft.revision,teamId:team,catalogueVersion:own.catalogue.version});
 }
 await post('/api/campaigns/'+id+'/prepare',{});
 for(let i=1;i<=11;i++)await post('/api/campaigns/'+id+'/absent/prepare',{teamId:'team-'+String(i).padStart(2,'0')});
 await post('/api/campaigns/'+id+'/start',{commandId:randomUUID()});
 for(let i=0;i<40;i++){
  expect((await request.post('http://127.0.0.1:'+apiPort+'/__test/clock',{data:{seconds:3600}})).ok()).toBeTruthy();
  const race=await request.get('/api/sessions/'+first);
  if(race.ok()){
   const state=await race.json();if(state.lifecycle==='finished')break;
   await post('/api/lobbies/'+first+'/'+(state.lifecycle==='phase-result'?'continue':'advance-due'),{commandId:randomUUID(),phase:state.phase,windowRevision:state.windowRevision});
  }else{
   await post('/api/lobbies/'+first+'/pause',{});await post('/api/lobbies/'+first+'/resume',{});
   const q=await get('/api/lobbies/'+first+'/qualifying');if(q.lifecycle==='session-result')await post('/api/lobbies/'+first+'/qualifying/continue',{commandId:randomUUID(),phase:Number(q.session.substring(1)),windowRevision:q.windowRevision});
  }
  if(i===39)throw new Error('Actual first weekend did not finish');
 }
 await post('/api/campaigns/'+id+'/commit-result?deviceId='+device,{});const next=await post('/api/campaigns/'+id+'/next/prepare',{});
 await page.evaluate(({id,first,device,uid})=>{localStorage.setItem('campaign-weekend:'+id,first);localStorage.setItem('household-device:'+uid+':'+first,device);},{id,first,device,uid:identity.localId});
 await page.goto('/campaigns/'+id);await page.getByRole('button',{name:'Open private development for McLaren',exact:true}).click();
 const own=page.getByRole('region',{name:'Your private development',exact:true});
 await own.getByRole('button',{name:'Compare Straight-line refinement as free draft',exact:true}).click();
 const nextDevice=await page.evaluate(({uid,session})=>localStorage.getItem('household-device:'+uid+':'+session),{uid:identity.localId,session:next.nextPreparation.sessionId});
 const endpoint='/api/campaigns/'+id+'/development/teams/team-01?deviceId='+nextDevice,current=await get(endpoint),quote=current.draft.quote;
 expect(current.balance).toBeLessThan(quote.cost);
 const rejection=page.waitForResponse(response=>response.url().includes('/development/teams/team-01/commit'));
 await own.getByRole('button',{name:'Commit permanent refinement · '+quote.cost+' credits',exact:true}).click();
 expect((await rejection).status()).toBe(409);await expect(page.getByRole('alert')).toContainText('Insufficient credits');
 const unchanged=await get(endpoint);expect(unchanged.balance).toBe(current.balance);expect(unchanged.commitment).toBeNull();
 await own.screenshot({path:captures+'/s14-05-unaffordable-360.png'});
 await own.getByRole('button',{name:'Choose Save as free draft',exact:true}).click();
 await own.getByRole('button',{name:'Commit Save · 0 credits',exact:true}).click();await expect(own.getByText(/Save committed/)).toBeVisible();
 await expect(own.getByText(current.balance+' credits available',{exact:true})).toBeVisible();
 await own.screenshot({path:captures+'/s14-05-shared-save-360.png'});
 expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);
 await page.screenshot({path:captures+'/s14-05-phone-flow-360.png',fullPage:true});
 await own.getByRole('button',{name:'Hide private development',exact:true}).click();await expect(own).toHaveCount(0);
 await page.setViewportSize({width:1920,height:1080});await page.getByRole('region',{name:'Development readiness',exact:true}).scrollIntoViewIfNeeded();
 await page.screenshot({path:captures+'/s14-05-public-readiness-1920.png'});
 const publicState=await get('/api/campaigns/'+id+'?deviceId='+device);expect(JSON.stringify(publicState.development)).not.toMatch(/quote|balance|personnel|entropy/i);
});
