import {test,expect} from '@playwright/test';
import {mkdirSync} from 'node:fs';
import {randomUUID} from 'node:crypto';
import {apiPort,captures} from '../../playwright.runtime';
test('a household creates a package, survives lost acknowledgement and freely selects its completed version',async({page:initialPage,request,browser})=>{
 let page=initialPage;test.setTimeout(180000);
 mkdirSync(captures,{recursive:true});await page.setViewportSize({width:360,height:800});await page.goto('/shared/new');
 const email='campaign-result-'+Date.now()+'@example.test',password='Local-test-password-123';await page.getByLabel('Your name').fill('Campaign host');await page.getByLabel('Email').fill(email);await page.getByLabel('Password').fill(password);await page.getByRole('button',{name:'Create account',exact:true}).click();await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 await page.getByRole('link',{name:'Campaigns',exact:true}).click();await expect(page.getByRole('button',{name:'Create campaign',exact:true})).toBeEnabled();
 await page.getByRole('button',{name:'Create campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Campaign created',exact:true})).toBeVisible({timeout:20000});const campaign=page.url();
 await expect(page.getByRole('heading',{name:'Autodromo Nazionale Monza',exact:true})).toBeVisible();await expect(page.getByRole('heading',{name:'Circuit de Monaco',exact:true})).toBeVisible();
 for(const image of await page.locator('img').all())await expect.poll(()=>image.evaluate((img:HTMLImageElement)=>img.complete&&img.naturalWidth>0)).toBeTruthy();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await page.screenshot({path:captures+'/s15-02-calendar-360.png',fullPage:true});
 await page.getByRole('button',{name:'Claim McLaren',exact:true}).click();await page.getByRole('button',{name:'Begin event preparation',exact:true}).click();await expect(page.getByRole('heading',{name:'Event preparation',exact:true})).toBeVisible();await page.reload();await expect(page.getByRole('heading',{name:'Event preparation',exact:true})).toBeVisible();
 await page.getByRole('link',{name:'Open team preparation in household',exact:true}).click();await page.getByRole('link',{name:'Team setup for McLaren',exact:true}).click();await expect(page.getByRole('heading',{name:'One setup. Both cars.',exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Confirm provisional setup and assess practice',exact:true}).click();await expect(page.getByRole('region',{name:'Frozen practice assessment',exact:true})).toBeVisible();await page.getByRole('button',{name:'Confirm final-ready setup',exact:true}).click();await expect(page.getByText('Final-ready - awaiting qualifying',{exact:true})).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await page.screenshot({path:captures+'/s15-02-prepared-360.png',fullPage:true});
 const weekend=new URL(page.url()).pathname.split('/')[2];const display=await page.context().newPage();await display.setViewportSize({width:1920,height:1080});await display.goto('/preparation/'+weekend);await expect(display.getByRole('heading',{name:'Teams are preparing',exact:true})).toBeVisible();await display.screenshot({path:captures+'/s15-02-preparation-public-1920.png'});await display.close();
 await page.goto(campaign);await expect(page.getByRole('button',{name:'Start first weekend',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Start first weekend',exact:true}).click();await expect(page.getByRole('heading',{name:'Weekend active',exact:true})).toBeVisible();await page.reload();await expect(page.getByRole('heading',{name:'Weekend active',exact:true})).toBeVisible();await page.getByRole('link',{name:'Open qualifying',exact:true}).click();await expect(page.getByRole('heading',{name:'Q1',exact:false})).toBeVisible();
 const config=await(await request.get('/api/auth/config')).json();const login=await request.post(config.emulatorUrl+'/identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key='+config.apiKey,{data:{email,password,returnSecureToken:true}});expect(login.ok()).toBeTruthy();const headers={Authorization:'Bearer '+(await login.json()).idToken};
 for(let i=0;i<30;i++){
  expect((await request.post('http://127.0.0.1:'+apiPort+'/__test/clock',{data:{seconds:3600}})).ok()).toBeTruthy();
  const race=await request.get('/api/sessions/'+weekend);
  if(race.ok()){
   const state=await race.json();if(state.lifecycle==='finished')break;
   const action=state.lifecycle==='phase-result'?'continue':'advance-due';
   const result=await request.post('/api/lobbies/'+weekend+'/'+action,{headers,data:{commandId:randomUUID(),phase:state.phase,windowRevision:state.windowRevision}});expect(result.ok(),await result.text()).toBeTruthy();
  }else{
   await request.post('/api/lobbies/'+weekend+'/pause',{headers,data:{}});await request.post('/api/lobbies/'+weekend+'/resume',{headers,data:{}});
   const q=await(await request.get('/api/lobbies/'+weekend+'/qualifying')).json();
   if(q.lifecycle==='session-result'){const result=await request.post('/api/lobbies/'+weekend+'/qualifying/continue',{headers,data:{commandId:randomUUID(),phase:Number(q.session.substring(1)),windowRevision:q.windowRevision}});expect(result.ok(),await result.text()).toBeTruthy();}
  }
  if(i===29)throw new Error('Real weekend did not finish');
 }
 await page.goto(campaign);await expect(page.getByRole('heading',{name:'Championship standings',exact:true})).toBeVisible();await expect(page.getByRole('heading',{name:'Event report',exact:true})).toBeVisible();await expect(page.getByText('Lando Norris',{exact:false}).first()).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await page.screenshot({path:captures+'/s15-02-standings-360.png',fullPage:true});
 await page.reload();await expect(page.getByRole('heading',{name:'Championship standings',exact:true})).toBeVisible();
 await page.setViewportSize({width:1920,height:1080});await page.screenshot({path:captures+'/s15-02-standings-1920.png',fullPage:true});expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(1920);
 await page.getByRole('link',{name:'Review Autodromo Nazionale Monza report',exact:true}).click();await expect(page.getByText('Championship 0 →',{exact:false}).first()).toBeVisible();
 await page.getByRole('button',{name:'Open next-event preparation',exact:true}).click();await expect(page.getByRole('heading',{name:'Next event · Circuit de Monaco',exact:true})).toBeVisible();
 await page.setViewportSize({width:360,height:800});
 await expect(page.getByRole('heading',{name:'Between-event development',exact:true})).toBeVisible();
 await expect(page.getByRole('button',{name:'Return to my next-event preparation',exact:true})).toBeDisabled();
 await page.getByRole('button',{name:'Open private development for McLaren',exact:true}).click();
 const own=page.getByRole('region',{name:'Your private development',exact:true});
 await expect(own.getByText('106 credits available',{exact:true})).toBeVisible();
 await expect(own.getByRole('heading',{name:'Low-drag package',exact:true})).toBeVisible();await expect(own.getByRole('heading',{name:'Cooling / reliability package',exact:true})).toBeVisible();await own.getByRole('button',{name:'Compare Downforce package as free draft',exact:true}).click();
 await expect(own.getByText(/Saved draft revision 1: Downforce package/)).toBeVisible();
 await expect(own.getByText('106 credits available',{exact:true})).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);
 await own.screenshot({path:captures+'/s15-02-development-draft-360.png'});
 const commandIds:string[]=[];await page.route('**/development/teams/*/commit?*',async route=>{commandIds.push(route.request().postDataJSON().commandId);if(commandIds.length===1){const result=await route.fetch();expect(result.ok()).toBeTruthy();await route.abort('failed');}else await route.continue();});
 await own.getByRole('button',{name:'Commit Race Package · 18 credits',exact:true}).click();await expect(page.getByRole('alert')).toBeVisible();await own.getByRole('button',{name:'Retry same development commitment',exact:true}).click();expect(commandIds).toHaveLength(2);expect(commandIds[0]).toBe(commandIds[1]);
 await expect(own.getByText('88 credits available',{exact:true})).toBeVisible();
 await expect(own.getByText(/Investment committed · 18 credits paid once/)).toBeVisible();
 await page.reload();await page.getByRole('button',{name:'Open private development for McLaren',exact:true}).click();
 await expect(own.getByText('88 credits available',{exact:true})).toBeVisible();
 await expect(own.getByText('Project pending preparation close. No gain has been applied yet.',{exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Host: close development · Save for missing teams',exact:true}).click();
 await expect(page.getByRole('button',{name:'Return to my next-event preparation',exact:true})).toBeEnabled();
 await page.getByRole('button',{name:'Review private project summary for McLaren',exact:true}).click();
 await expect(own.getByRole('heading',{name:/Completed Race Package/})).toBeVisible();

 await own.getByText('Full current and target attributes',{exact:true}).click();
 await expect(own.getByText(/Top Speed: current 82/)).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);
 await own.screenshot({path:captures+'/s15-02-outcome-'+(process.env['F1_DEVELOPMENT_DELAY_FIXTURE']==='1'?'delay':'normal')+'-360.png'});
 await own.getByRole('button',{name:'Hide private development',exact:true}).click();await expect(own).toHaveCount(0);
 await page.setViewportSize({width:1920,height:1080});
 await page.getByRole('region',{name:'Development readiness',exact:true}).scrollIntoViewIfNeeded();
 await page.screenshot({path:captures+'/s15-02-readiness-1920.png'});
  expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(1920);
 await page.setViewportSize({width:360,height:800});await page.getByRole('button',{name:'Return to my next-event preparation',exact:true}).click();await page.getByRole('link',{name:'Team setup for McLaren',exact:true}).click();
 const library=page.getByRole('region',{name:'Race Package library',exact:true});await expect(library).toContainText('Downforce package');await expect(library).toContainText('Neutral baseline');await library.getByRole('button',{name:'Select Downforce package v1',exact:true}).click();await expect(page.getByRole('status')).toContainText('No credits or development decision spent');await page.reload();await expect(library).toContainText('Selected for this weekend');await library.screenshot({path:captures+'/s15-02-completed-library-360.png'});
 await page.getByRole('button',{name:'Confirm provisional setup and assess practice',exact:true}).click();await expect(page.getByRole('region',{name:'Frozen practice assessment',exact:true})).toContainText('Assessed Race Package: Downforce package');await page.getByRole('button',{name:'Confirm final-ready setup',exact:true}).click();await expect(page.getByRole('status')).toContainText('Final-ready confirmed');
 const second=new URL(page.url()).pathname.split('/')[2];const publicDisplay=await page.context().newPage();await publicDisplay.setViewportSize({width:1920,height:1080});await publicDisplay.goto('/preparation/'+second);await expect(publicDisplay.getByRole('heading',{name:'Teams are preparing',exact:true})).toBeVisible();await expect(publicDisplay.getByRole('region',{name:'Race Package library',exact:true})).toHaveCount(0);await publicDisplay.screenshot({path:captures+'/s15-02-public-display-1920.png'});await publicDisplay.close();
 await page.goto(campaign);await expect(page.getByRole('button',{name:'Start next weekend',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Start next weekend',exact:true}).click();await page.goto('/preparation/'+second+'?team=team-01');await expect(page.getByText('Locked for qualifying',{exact:true})).toBeVisible();await expect(page.getByRole('button',{name:'Select Downforce package v1',exact:true})).toBeDisabled();
});
