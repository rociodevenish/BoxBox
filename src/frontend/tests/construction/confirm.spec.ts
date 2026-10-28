import {test,expect} from '@playwright/test';
import {captures} from '../../playwright.runtime';

test('confirmation retries retain one locked car and hand off only after all teams',async({page,request})=>{
 await page.setViewportSize({width:360,height:800});
 let authorization='';page.on('request',req=>{if(req.url().includes('/api/campaigns')&&req.headers()['authorization'])authorization=req.headers()['authorization'];});
 await page.goto('/shared/new');await page.getByLabel('Your name').fill('Construction host');await page.getByLabel('Email').fill('construction-'+Date.now()+'@example.test');await page.getByLabel('Password').fill('Local-test-password-123');await page.getByRole('button',{name:'Create account',exact:true}).click();await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 await page.getByRole('link',{name:'Campaigns',exact:true}).click();await page.getByLabel('Personnel').selectOption('acquisition');await page.getByRole('button',{name:'Create campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Personnel acquisition',exact:true})).toBeVisible();
 const campaignId=new URL(page.url()).pathname.split('/')[2];const device=await page.evaluate(id=>{const weekend=localStorage.getItem('campaign-weekend:'+id)!;const key=Object.keys(localStorage).find(k=>k.startsWith('household-device:')&&k.endsWith(':'+weekend))!;return localStorage.getItem(key)!;},campaignId);
 const headers={Authorization:authorization};const get=async(path:string)=>(await request.get(path,{headers})).json();const post=async(path:string,data:unknown)=>{const response=await request.post(path,{headers,data});expect(response.ok(),await response.text()).toBeTruthy();return response.json();};
 const season=await get('/api/campaigns/'+campaignId+'?deviceId='+device);const weekend=season.weekendId;
 for(const team of season.teams){const name='Shared '+team.teamId;const lobby=await post('/api/lobbies/'+weekend+'/participants',{name,deviceId:device});const playerId=lobby.players.find((p:{name:string})=>p.name===name).playerId;await post('/api/lobbies/'+weekend+'/assignments',{teamId:team.teamId,playerId,deviceId:device,control:'SharedTablet'});}
 for(let pick=0;pick<77;pick++){
  const campaign=await get('/api/campaigns/'+campaignId+'?deviceId='+device);const team=campaign.acquisition.activeTeamId;const path='/api/campaigns/'+campaignId+'/acquisition/teams/'+team+'?deviceId='+device;const own=await get(path);const choices=own.hireOptions.filter((o:{canHire:boolean})=>o.canHire);choices.sort((a:{personId:string},b:{personId:string})=>own.offers.find((p:{id:string})=>p.id===b.personId).price-own.offers.find((p:{id:string})=>p.id===a.personId).price);await post(path.replace('?','/hire?'),{commandId:crypto.randomUUID(),revision:own.revision,teamId:team,personId:choices[0].personId});
 }
 await page.getByRole('button',{name:'Refresh campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Base Car construction',exact:true})).toBeVisible();
 const candidates=await Promise.all(season.teams.map(async(team:{teamId:string;constructorName:string})=>({team,own:await get('/api/campaigns/'+campaignId+'/construction/teams/'+team.teamId+'?deviceId='+device)})));candidates.sort((a,b)=>a.own.preview.balance-b.own.preview.balance);const team=candidates[0].team;const teamName=team.constructorName;expect(candidates[0].own.preview.balance).toBeLessThan(24);
 await page.getByRole('button',{name:'Open construction for '+teamName,exact:true}).click();const panel=page.getByRole('region',{name:'Your private construction'});
 await expect(panel.getByText('Saved draft revision 1',{exact:false})).toBeVisible();await expect(panel.locator('.construction-categories').getByRole('combobox')).toHaveCount(4);await expect(panel.getByRole('heading',{name:'Both-driver compatibility',exact:true})).toBeVisible();
 const path='/api/campaigns/'+campaignId+'/construction/teams/'+team.teamId+'?deviceId='+device;const original=await get(path);const ledgerBefore=await get('/api/campaigns/'+campaignId+'/acquisition/teams/'+team.teamId+'?deviceId='+device);
  for(const [index,category] of original.catalogue.categories.entries()){const expensive=category.options.find((option:{cost:number})=>option.cost===6);await panel.locator('.construction-categories').getByRole('combobox').nth(index).selectOption(expensive.id);}
 await expect(panel.getByRole('button',{name:'Save free draft and compare',exact:true})).toBeDisabled();await expect(panel.getByRole('status')).toContainText('exceeds your available credits');expect((await get(path)).draft.revision).toBe(1);
 for(const [index,id] of original.draft.optionIds.entries())await panel.locator('.construction-categories').getByRole('combobox').nth(index).selectOption(id);
 await panel.getByLabel('Aero Concept choice').selectOption('car-aero-high-downforce');
 await page.route('**/construction/teams/*/draft?*',async route=>{await route.fetch();await route.abort('connectionfailed');},{times:1});
 await panel.getByRole('button',{name:'Save free draft and compare',exact:true}).click();await expect(page.getByRole('alert')).toBeVisible();
 expect((await get(path)).draft.optionIds[0]).toBe('car-aero-high-downforce');await page.reload();await page.getByRole('button',{name:'Open construction for '+teamName,exact:true}).click();await panel.getByRole('button',{name:'Retry saved draft',exact:true}).click();await expect(panel.getByText('Saved draft revision 2',{exact:false})).toBeVisible();await expect(panel.getByLabel('Aero Concept choice')).toHaveValue('car-aero-high-downforce');
 await panel.getByRole('button',{name:'Save free draft and compare',exact:true}).click();await expect(panel.getByText('Saved draft revision 3',{exact:false})).toBeVisible();const after=await get(path);expect(after.preview.balance).toBe(original.preview.balance);expect((await get('/api/campaigns/'+campaignId+'/acquisition/teams/'+team.teamId+'?deviceId='+device)).ledger).toEqual(ledgerBefore.ledger);
 await panel.getByRole('button',{name:'Save free draft and compare',exact:true}).scrollIntoViewIfNeeded();expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await page.screenshot({path:captures+'/s13-02-draft-360.png',fullPage:true});
 await expect(panel.getByLabel('Simple car attribute groups').getByText('Mechanical Grip',{exact:true})).toBeVisible();await panel.getByText('Saved draft · Detailed attributes',{exact:true}).click();await expect(panel.getByText('Reliability',{exact:true}).last()).toBeVisible();await panel.getByRole('heading',{name:'Both-driver compatibility',exact:true}).evaluate(heading=>heading.scrollIntoView({block:'start'}));await expect.poll(()=>panel.locator('.construction-drivers img').evaluateAll(images=>images.every(image=>(image as HTMLImageElement).naturalWidth>0))).toBeTruthy();await page.screenshot({path:captures+'/s13-02-fit-360.png'});
 const forbidden=await request.post('/api/campaigns/'+campaignId+'/prepare',{headers,data:{}});expect(forbidden.status()).toBe(409);const direct=await request.post('/api/lobbies/'+weekend+'/preparation',{headers,data:{circuitId:'track-monza'}});expect(direct.status()).toBe(409);
 await panel.getByRole('button',{name:'Hide private construction',exact:true}).click();await page.setViewportSize({width:1920,height:1080});await page.evaluate(()=>window.scrollTo(0,0));await page.screenshot({path:captures+'/s13-02-public-1920.png'});await expect(panel).toHaveCount(0);
 await page.getByRole('button',{name:'Open construction for '+teamName,exact:true}).click();await panel.scrollIntoViewIfNeeded();await page.screenshot({path:captures+'/s13-02-draft-1920.png'});
 await page.setViewportSize({width:360,height:800});
 await page.route('**/construction/teams/*/confirm?*',async route=>{await route.fetch();await route.abort('connectionfailed');},{times:1});
 await panel.getByRole('button',{name:/^Confirm Base Car for/}).click();await expect(page.getByRole('alert')).toBeVisible();
 const frozen=(await get(path)).confirmed;expect(frozen.cost).toBe(18);
 await page.reload();await page.getByRole('button',{name:'Open construction for '+teamName,exact:true}).click();
 await panel.getByRole('button',{name:'Retry Base Car confirmation',exact:true}).click();
 await expect(panel.getByRole('status')).toContainText('Base Car confirmed');await expect(panel.locator('.construction-categories').getByRole('combobox').first()).toBeDisabled();
 expect((await get('/api/campaigns/'+campaignId+'/acquisition/teams/'+team.teamId+'?deviceId='+device)).ledger.filter((entry:{reason:string})=>entry.reason==='base-car-construction')).toHaveLength(1);
 await panel.getByRole('status').scrollIntoViewIfNeeded();expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await page.screenshot({path:captures+'/s13-02-confirmed-360.png'});
 for(const candidate of candidates.filter(candidate=>candidate.team.teamId!==team.teamId))await post('/api/lobbies/'+weekend+'/assignments',{teamId:candidate.team.teamId,playerId:null,deviceId:device,control:'Private'});
 await page.getByRole('button',{name:'Build untouched unclaimed teams automatically',exact:true}).click();await expect(panel).toHaveCount(0);const built=await get('/api/campaigns/'+campaignId+'?deviceId='+device);expect(built.constructionReadiness.filter((entry:{source:string})=>entry.source==='unclaimed-ai')).toHaveLength(10);
 await page.getByRole('button',{name:'Refresh campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Campaign created',exact:true})).toBeVisible();await expect(page.getByRole('button',{name:'Refresh campaign',exact:true})).toBeEnabled();
 await page.getByRole('button',{name:'Open personnel for '+teamName,exact:true}).click();
 const personnel=page.getByRole('region',{name:'Your private personnel',exact:true});
 const ledger=personnel.getByRole('region',{name:'Your personnel ledger',exact:true});
 await expect(personnel.getByText('0 credits remaining',{exact:true})).toBeVisible();
 await expect(ledger.getByText('-18 credits · Car construction',{exact:true})).toHaveCount(1);
 await expect(ledger.getByText(/Personnel hire/)).toHaveCount(7);
 await expect(personnel.getByRole('region',{name:'Final personnel roster',exact:true})).toContainText('Your Base Car is already paid.');
 await expect(personnel).not.toContainText('Your minimum car reserve remains protected.');
 await ledger.scrollIntoViewIfNeeded();await page.screenshot({path:captures+'/s13-remediation-personnel-360.png'});
 await page.getByRole('button',{name:'Hide private personnel',exact:true}).click();
 await page.setViewportSize({width:1920,height:1080});await page.evaluate(()=>window.scrollTo(0,0));await page.screenshot({path:captures+'/s13-02-handoff-1920.png'});});
