import {test,expect} from '@playwright/test';
import {mkdirSync} from 'node:fs';
import {captures} from '../../playwright.runtime';

test('completion reserves explain blocked choices and a recovered final hire shows the frozen ready roster',async({page,request})=>{
 test.setTimeout(180000);mkdirSync(captures,{recursive:true});await page.setViewportSize({width:360,height:800});
 let authorization='';page.on('request',req=>{if(req.url().includes('/api/campaigns')&&req.headers()['authorization'])authorization=req.headers()['authorization'];});
 await page.goto('/shared/new');await page.getByLabel('Your name').fill('Completion host');await page.getByLabel('Email').fill('completion-'+Date.now()+'@example.test');await page.getByLabel('Password').fill('Local-test-password-123');await page.getByRole('button',{name:'Create account',exact:true}).click();await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 await page.route('**/api/campaigns',async route=>{await route.continue({postData:JSON.stringify({...route.request().postDataJSON(),seed:1})});},{times:1});
 await page.getByRole('link',{name:'Campaigns',exact:true}).click();await page.getByLabel('Personnel').selectOption('acquisition');await page.getByRole('button',{name:'Create campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Personnel acquisition',exact:true})).toBeVisible();
 const picker=(await page.getByText(/^Current picker:/).textContent())!.split(': ')[1].split(' · ')[0];await page.getByRole('button',{name:'Claim '+picker,exact:true}).click();await page.getByRole('button',{name:'Open personnel for '+picker,exact:true}).click();
 await expect(page.getByText('Completion reserve: 96 credits',{exact:true})).toBeVisible();await expect(page.getByText('7 mandatory places remaining',{exact:true})).toBeVisible();
 await expect(page.getByRole('complementary',{name:'Offer comparison advice'})).toContainText('lowest priced legal offer');
 const campaignId=new URL(page.url()).pathname.split('/')[2];
 const device=await page.evaluate(id=>{const weekend=localStorage.getItem('campaign-weekend:'+id)!;const key=Object.keys(localStorage).find(k=>k.startsWith('household-device:')&&k.endsWith(':'+weekend))!;return localStorage.getItem(key)!;},campaignId);
 const headers={Authorization:authorization};
 const get=async(path:string)=>(await request.get(path,{headers})).json();
 const post=async(path:string,data:unknown)=>{const response=await request.post(path,{headers,data});expect(response.ok(),await response.text()).toBeTruthy();return response.json();};
 const season=await get('/api/campaigns/'+campaignId+'?deviceId='+device);const weekend=season.weekendId;
 const firstTeam=season.acquisition.activeTeamId;
 for(const team of season.teams.filter((t:{teamId:string})=>t.teamId!==firstTeam)){
  const name='Shared '+team.teamId;const lobby=await post('/api/lobbies/'+weekend+'/participants',{name,deviceId:device});const playerId=lobby.players.find((p:{name:string})=>p.name===name).playerId;
  await post('/api/lobbies/'+weekend+'/assignments',{teamId:team.teamId,playerId,deviceId:device,control:'SharedTablet'});
 }
 let captured=false,driverCaptured=false,engineerCaptured=false,technicalCaptured=false;
 for(let pick=0;pick<77;pick++){
  const current=await get('/api/campaigns/'+campaignId+'?deviceId='+device);const team=current.acquisition.activeTeamId;const path='/api/campaigns/'+campaignId+'/acquisition/teams/'+team+'?deviceId='+device;let own=await get(path);if(own.scoutUnavailable===null)own=await post(path.replace('?','/scout?'),{commandId:crypto.randomUUID(),revision:own.revision,teamId:team});
  const illegal=own.hireOptions.find((o:{canHire:boolean})=>!o.canHire);
  if((own.slot==='race-engineer-1'&&!engineerCaptured)||(own.slot==='technical-engineer-1'&&!technicalCaptured)){
   await page.reload();const name=season.teams.find((t:{teamId:string})=>t.teamId===team).constructorName;await page.getByRole('button',{name:'Open personnel for '+name,exact:true}).click();const offers=page.getByRole('region',{name:'Reserved personnel offers'});await expect(offers.locator('.staff-fallback')).toHaveCount(3);
   if(own.slot==='race-engineer-1'){await expect(offers.getByText("Setup improves the assigned driver's Practice Assessment confidence. Tyre Reading, Strategy and Communication are inactive; no pace or error bonus.",{exact:true})).toHaveCount(3);engineerCaptured=true;}else{await expect(offers.getByText('Future economy effects · No current performance bonus',{exact:true})).toHaveCount(3);technicalCaptured=true;}
  }
  if(own.slot==='driver-1'&&!driverCaptured){
   await page.reload();const name=season.teams.find((t:{teamId:string})=>t.teamId===team).constructorName;await page.getByRole('button',{name:'Open personnel for '+name,exact:true}).click();
   const offers=page.getByRole('region',{name:'Reserved personnel offers'});await expect(offers.getByText('Race Pace',{exact:true})).toHaveCount(3);await expect(offers.getByText('Preferences describe compatibility, separately from quality.',{exact:true})).toHaveCount(3);
   await expect.poll(()=>offers.locator('img').evaluateAll(images=>images.every(image=>(image as HTMLImageElement).naturalWidth>0))).toBeTruthy();await offers.scrollIntoViewIfNeeded();await page.screenshot({path:captures+'/s12-04-driver-offers-360.png',fullPage:true});driverCaptured=true;
   const names=await offers.getByRole('heading').allTextContents();await page.route('**/portraits/*.webp',route=>route.abort());await page.reload();await page.getByRole('button',{name:'Open personnel for '+name,exact:true}).click();await expect(offers.getByRole('heading')).toHaveText(names);await expect.poll(()=>offers.locator('img').evaluateAll(images=>images.every(image=>(image as HTMLImageElement).hidden))).toBeTruthy();await expect(offers.getByRole('button',{name:/^Hire /})).toHaveCount(3);await page.unroute('**/portraits/*.webp');
  }
  if(illegal&&!captured){
   await page.reload();const name=season.teams.find((t:{teamId:string})=>t.teamId===team).constructorName;await page.getByRole('button',{name:'Open personnel for '+name,exact:true}).click();
   const card=own.offers.find((p:{id:string})=>p.id===illegal.personId);await expect(page.getByRole('button',{name:'Hire '+card.name+' · '+card.price+' credits',exact:true})).toBeDisabled();await expect(page.getByText(illegal.unavailable,{exact:true}).first()).toBeVisible();
   const rejected=await request.post(path.replace('?','/hire?'),{headers,data:{commandId:crypto.randomUUID(),revision:own.revision,teamId:team,personId:card.id}});expect(rejected.status()).toBe(409);
   expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await page.screenshot({path:captures+'/s12-03-reserve-360.png',fullPage:true});captured=true;
  }
  const legal=own.hireOptions.filter((o:{canHire:boolean})=>o.canHire).sort((a:{personId:string},b:{personId:string})=>own.offers.find((p:{id:string})=>p.id===b.personId).price-own.offers.find((p:{id:string})=>p.id===a.personId).price)[0];
  if(pick<76)await post(path.replace('?','/hire?'),{commandId:crypto.randomUUID(),revision:own.revision,teamId:team,personId:legal.personId});
  else{
   const name=season.teams.find((t:{teamId:string})=>t.teamId===team).constructorName;await page.reload();await page.getByRole('button',{name:'Open personnel for '+name,exact:true}).click();const card=own.offers.find((p:{id:string})=>p.id===legal.personId);
   await page.route('**/hire?*',async route=>{await route.fetch();await route.abort('connectionfailed');},{times:1});await page.getByRole('button',{name:'Hire '+card.name+' · '+card.price+' credits',exact:true}).click();await expect(page.getByRole('alert')).toBeVisible();await page.reload();await page.getByRole('button',{name:'Open personnel for '+name,exact:true}).click();await page.getByRole('button',{name:'Retry saved hire',exact:true}).click();await expect(page.getByRole('button',{name:'Retry saved hire',exact:true})).toHaveCount(0);
   await expect(page.getByRole('heading',{name:'Roster ready',exact:true})).toBeVisible();await expect(page.getByText('0 mandatory places remaining',{exact:true})).toBeVisible();await expect(page.getByText('Completion reserve: 18 credits',{exact:true})).toBeVisible();await expect(page.getByRole('region',{name:'Final personnel roster'}).getByRole('listitem')).toHaveCount(7);await page.screenshot({path:captures+'/s12-04-ready-360.png',fullPage:true});
   await page.getByRole('button',{name:'Hide private personnel',exact:true}).click();await page.setViewportSize({width:1920,height:1080});await page.evaluate(()=>window.scrollTo(0,0));await page.screenshot({path:captures+'/s12-03-public-top-1920.png'});await page.getByRole('region',{name:'Acquisition progress'}).scrollIntoViewIfNeeded();await expect(page.getByText('All required rosters are ready.',{exact:true})).toBeVisible();await expect(page.getByRole('region',{name:'Your private personnel'})).toHaveCount(0);await page.screenshot({path:captures+'/s12-03-ready-public-1920.png'});
   const roster=page.locator('section').filter({has:page.getByRole('heading',{name:'Acquired campaign drivers',exact:true})});await expect(roster.locator('img')).toHaveCount(22);await expect.poll(()=>roster.locator('img').evaluateAll(images=>images.every(image=>(image as HTMLImageElement).naturalWidth>0))).toBeTruthy();await page.getByRole('heading',{name:'Acquired campaign drivers',exact:true}).evaluate(heading=>heading.closest('section')!.scrollIntoView({block:'start'}));await page.screenshot({path:captures+'/s12-04-acquired-roster-1920.png'});
  }
 }
 expect(captured).toBeTruthy();
 expect(engineerCaptured).toBeTruthy();expect(technicalCaptured).toBeTruthy();
});
