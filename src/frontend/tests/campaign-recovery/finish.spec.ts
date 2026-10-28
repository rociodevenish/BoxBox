import {test,expect} from '@playwright/test';
import {mkdirSync} from 'node:fs';
import {randomUUID} from 'node:crypto';
import {apiPort,captures} from '../../playwright.runtime';
test('returning host finishes two events and preserves final champions and history',async({page:initialPage,request,browser})=>{
 let page=initialPage;
 mkdirSync(captures,{recursive:true});await page.setViewportSize({width:360,height:800});await page.goto('/shared/new');
 const email='campaign-result-'+Date.now()+'@example.test',password='Local-test-password-123';await page.getByLabel('Your name').fill('Campaign host');await page.getByLabel('Email').fill(email);await page.getByLabel('Password').fill(password);await page.getByRole('button',{name:'Create account',exact:true}).click();await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 await page.getByRole('link',{name:'Campaigns',exact:true}).click();await expect(page.getByRole('button',{name:'Create campaign',exact:true})).toBeEnabled();
 await page.getByRole('button',{name:'Create campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Campaign created',exact:true})).toBeVisible();const campaign=page.url();
 await expect(page.getByRole('heading',{name:'Autodromo Nazionale Monza',exact:true})).toBeVisible();await expect(page.getByRole('heading',{name:'Circuit de Monaco',exact:true})).toBeVisible();
 for(const image of await page.locator('img').all())await expect.poll(()=>image.evaluate((img:HTMLImageElement)=>img.complete&&img.naturalWidth>0)).toBeTruthy();
 await page.getByRole('button',{name:'Claim McLaren',exact:true}).click();await page.getByRole('button',{name:'Begin event preparation',exact:true}).click();await expect(page.getByRole('heading',{name:'Event preparation',exact:true})).toBeVisible();await page.reload();await expect(page.getByRole('heading',{name:'Event preparation',exact:true})).toBeVisible();
 await page.getByRole('link',{name:'Open team preparation in household',exact:true}).click();await page.getByRole('link',{name:'Team setup for McLaren',exact:true}).click();await expect(page.getByRole('heading',{name:'One setup. Both cars.',exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Confirm provisional setup and assess practice',exact:true}).click();await expect(page.getByRole('region',{name:'Frozen practice assessment',exact:true})).toBeVisible();await page.getByRole('button',{name:'Confirm final-ready setup',exact:true}).click();await expect(page.getByText('Final-ready - awaiting qualifying',{exact:true})).toBeVisible();
 await page.goto(campaign);await expect(page.getByRole('button',{name:'Start first weekend',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Start first weekend',exact:true}).click();await expect(page.getByRole('heading',{name:'Weekend active',exact:true})).toBeVisible();await page.reload();await expect(page.getByRole('heading',{name:'Weekend active',exact:true})).toBeVisible();await page.getByRole('link',{name:'Open qualifying',exact:true}).click();await expect(page.getByRole('heading',{name:'Q1',exact:false})).toBeVisible();
 const weekend=new URL(page.url()).pathname.split('/')[2];
 const config=await(await request.get('/api/auth/config')).json();const login=await request.post(config.emulatorUrl+'/identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key='+config.apiKey,{data:{email,password,returnSecureToken:true}});expect(login.ok()).toBeTruthy();const headers={Authorization:'Bearer '+(await login.json()).idToken};
 const finishWeekend=async(weekend:string)=>{
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
 };
 await finishWeekend(weekend);
 await page.goto(campaign);await expect(page.getByRole('heading',{name:'Championship standings',exact:true})).toBeVisible();await expect(page.getByRole('heading',{name:'Event report',exact:true})).toBeVisible();await expect(page.getByText('Lando Norris',{exact:false}).first()).toBeVisible();
 await page.reload();await expect(page.getByRole('heading',{name:'Championship standings',exact:true})).toBeVisible();
 await page.getByRole('link',{name:'Review Autodromo Nazionale Monza report',exact:true}).click();await expect(page.getByText('Championship 0 →',{exact:false}).first()).toBeVisible();
 await page.getByRole('button',{name:'Open next-event preparation',exact:true}).click();await expect(page.getByRole('heading',{name:'Next event · Circuit de Monaco',exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Open private development for McLaren',exact:true}).click();
 await page.getByRole('button',{name:'Commit Save · 0 credits',exact:true}).click();
 await page.getByRole('button',{name:'Host: close development · Save for missing teams',exact:true}).click();
 await expect(page.getByRole('button',{name:'Start next weekend',exact:true})).toBeDisabled();
 await page.getByRole('button',{name:'Return to my next-event preparation',exact:true}).click();await page.getByRole('link',{name:'Team setup for McLaren',exact:true}).click();
 await expect(page.getByRole('heading',{name:'Circuit de Monaco',exact:true})).toBeVisible();await page.getByLabel('Aero: Downforce',{exact:true}).check();await page.getByRole('button',{name:'Save provisional setup',exact:true}).click();
 await page.getByRole('link',{name:'Return to championship',exact:true}).click();await expect(page.getByRole('heading',{name:'Next event · Circuit de Monaco',exact:true})).toBeVisible();
 const returning=await browser.newContext({viewport:{width:360,height:800}});page=await returning.newPage();await page.goto(new URL('/shared/new',campaign).href);await page.getByLabel('Your name').fill('Campaign host');await page.getByLabel('Email').fill(email);await page.getByLabel('Password').fill(password);await page.getByRole('button',{name:'Sign in',exact:true}).click();await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();await page.goto(campaign);await expect(page.getByRole('heading',{name:'Next event · Circuit de Monaco',exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Return to my next-event preparation',exact:true}).click();await page.getByRole('link',{name:'Team setup for McLaren',exact:true}).click();await expect(page.getByLabel('Aero: Downforce',{exact:true})).toBeChecked();
 await page.getByRole('button',{name:'Confirm provisional setup and assess practice',exact:true}).click();await page.getByRole('button',{name:'Confirm final-ready setup',exact:true}).click();
 await page.getByRole('link',{name:'Return to championship',exact:true}).click();await expect(page.getByRole('button',{name:'Start next weekend',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Start next weekend',exact:true}).click();await expect(page.getByRole('heading',{name:'Event 2 weekend',exact:true})).toBeVisible();await page.reload();await expect(page.getByRole('heading',{name:'Event 2 weekend',exact:true})).toBeVisible();
 await page.getByRole('link',{name:'Open qualifying',exact:true}).click();
 await expect(page.getByRole('heading',{name:'Q1',exact:false})).toBeVisible();
 const secondWeekend=new URL(page.url()).pathname.split('/')[2];
 expect(secondWeekend).not.toBe(weekend);
 await finishWeekend(secondWeekend);
 await page.goto(campaign);
 const assertCompleted=async()=>{
  await expect(page.getByRole('heading',{name:'Season complete',exact:true})).toBeVisible();
  const champions=page.getByRole('region',{name:'Season champions',exact:true});
  await expect(champions.getByRole('heading',{level:2,name:/^Constructors' champion · [A-Za-z]/})).toBeVisible();
  await expect(champions.getByText(/^Primary championship · \d+ points$/)).toBeVisible();
  await expect(champions.getByRole('heading',{level:3,name:/^Drivers' champion · [A-Za-z]/})).toBeVisible();
  await expect(champions.getByText(/^Secondary championship · \d+ points$/)).toBeVisible();
  await expect(page.getByRole('heading',{name:'Event report',exact:true})).toHaveCount(2);
  await expect(page.getByRole('link',{name:'Review Autodromo Nazionale Monza report',exact:true})).toBeVisible();
  await expect(page.getByRole('link',{name:'Review Circuit de Monaco report',exact:true})).toBeVisible();
  await expect(page.getByRole('button',{name:/Start .*weekend|Open next-event preparation|Begin event preparation/})).toHaveCount(0);
 };
 await assertCompleted();
 const preserved=await page.locator('section[id^="report-"]').allTextContents();
 const championText=await page.getByRole('region',{name:'Season champions'}).innerText();
 await page.getByRole('button',{name:'Refresh campaign',exact:true}).click();
 await assertCompleted();
 await page.reload();
 await assertCompleted();
 expect(await page.locator('section[id^="report-"]').allTextContents()).toEqual(preserved);
 expect(await page.getByRole('region',{name:'Season champions'}).innerText()).toBe(championText);
 for(const width of [360,1920]){
  await page.setViewportSize({width,height:width===360?800:1080});
  expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
  await page.evaluate(()=>window.scrollTo(0,0));
  const bounds=await page.getByRole('region',{name:'Season champions'}).boundingBox();
  expect(bounds).not.toBeNull();expect(bounds!.y+bounds!.height).toBeLessThanOrEqual(width===360?800:1080);
  await page.screenshot({path:captures+'/s11-05-top-viewport-'+width+'.png'});
  await page.screenshot({path:captures+'/s11-05-season-history-'+width+'.png',fullPage:true});
  await page.route('**/{portraits,tracks}/**',route=>route.abort());
  await page.reload();await assertCompleted();
  for(const image of await page.locator('img').all())await expect(image).toBeHidden();
  await expect(page.getByRole('heading',{name:'Autodromo Nazionale Monza',exact:true}).first()).toBeVisible();
  await page.evaluate(()=>window.scrollTo(0,0));
  await page.screenshot({path:captures+'/s11-05-missing-images-'+width+'.png'});
  await page.locator('.calendar').screenshot({path:captures+'/s11-05-calendar-fallback-'+width+'.png'});
  await page.unroute('**/{portraits,tracks}/**');await page.reload();await assertCompleted();
 }
 await returning.close();
});
