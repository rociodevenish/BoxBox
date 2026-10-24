import {test,expect} from '@playwright/test';
import {mkdirSync} from 'node:fs';
import {randomUUID} from 'node:crypto';
import {apiPort,captures} from '../../playwright.runtime';
test('finished first weekend has durable explained standings and report',async({page,request})=>{
 mkdirSync(captures,{recursive:true});await page.setViewportSize({width:360,height:800});await page.goto('/shared/new');
 const email='campaign-result-'+Date.now()+'@example.test',password='Local-test-password-123';await page.getByLabel('Your name').fill('Campaign host');await page.getByLabel('Email').fill(email);await page.getByLabel('Password').fill(password);await page.getByRole('button',{name:'Create account',exact:true}).click();await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 await page.getByRole('link',{name:'Campaigns',exact:true}).click();await expect(page.getByRole('button',{name:'Create campaign',exact:true})).toBeEnabled();
 await page.getByRole('button',{name:'Create campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Campaign created',exact:true})).toBeVisible();const campaign=page.url();
 await expect(page.getByRole('heading',{name:'Autodromo Nazionale Monza',exact:true})).toBeVisible();await expect(page.getByRole('heading',{name:'Circuit de Monaco',exact:true})).toBeVisible();
 for(const image of await page.locator('img').all())await expect.poll(()=>image.evaluate((img:HTMLImageElement)=>img.complete&&img.naturalWidth>0)).toBeTruthy();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await page.screenshot({path:captures+'/s11-02-calendar-360.png',fullPage:true});
 await page.getByRole('button',{name:'Claim McLaren',exact:true}).click();await page.getByRole('button',{name:'Begin event preparation',exact:true}).click();await expect(page.getByRole('heading',{name:'Event preparation',exact:true})).toBeVisible();await page.reload();await expect(page.getByRole('heading',{name:'Event preparation',exact:true})).toBeVisible();
 await page.getByRole('link',{name:'Open team preparation in household',exact:true}).click();await page.getByRole('link',{name:'Team setup for McLaren',exact:true}).click();await expect(page.getByRole('heading',{name:'One setup. Both cars.',exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Confirm provisional setup and assess practice',exact:true}).click();await expect(page.getByRole('region',{name:'Frozen practice assessment',exact:true})).toBeVisible();await page.getByRole('button',{name:'Confirm final-ready setup',exact:true}).click();await expect(page.getByText('Final-ready - awaiting qualifying',{exact:true})).toBeVisible();
 expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await page.screenshot({path:captures+'/s11-02-prepared-360.png',fullPage:true});
 const weekend=new URL(page.url()).pathname.split('/')[2];const display=await page.context().newPage();await display.setViewportSize({width:1920,height:1080});await display.goto('/preparation/'+weekend);await expect(display.getByRole('heading',{name:'Teams are preparing',exact:true})).toBeVisible();await display.screenshot({path:captures+'/s11-02-preparation-public-1920.png'});await display.close();
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
 expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await page.screenshot({path:captures+'/s11-02-standings-360.png',fullPage:true});
 await page.reload();await expect(page.getByRole('heading',{name:'Championship standings',exact:true})).toBeVisible();
 await page.setViewportSize({width:1920,height:1080});await page.screenshot({path:captures+'/s11-02-standings-1920.png',fullPage:true});expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(1920);
});
