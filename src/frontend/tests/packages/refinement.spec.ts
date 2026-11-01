import {test,expect} from '@playwright/test';
import {apiPort,captures} from '../../playwright.runtime';
import {mkdirSync} from 'node:fs';
for(const [kind,label] of [['strengthen','Strengthen'],['reduce-compromise','Reduce compromise'],['broaden-usefulness','Broaden usefulness']]){
 test('synthetic public-domain refinement boundary: '+label,async({page,request})=>{
  mkdirSync(captures,{recursive:true});await page.setViewportSize({width:360,height:800});await page.goto('/shared/new');
  await page.getByLabel('Your name').fill('Refinement fixture');await page.getByLabel('Email').fill('refinement-'+kind+'-'+Date.now()+'@example.test');await page.getByLabel('Password').fill('Local-test-password-123');await page.getByRole('button',{name:'Create account',exact:true}).click();await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
  const fixture='http://127.0.0.1:'+apiPort+'/__test/package-refinement';
  const reset=await request.post(fixture,{data:{operation:'reset',command:{}}});expect(reset.ok(),await reset.text()).toBeTruthy();let state=await reset.json();
  const drafts:Record<string,unknown>[]=[];const commits:string[]=[];
  await page.route('**/api/lobbies/synthetic-*/controllers?*',route=>route.fulfill({json:[{teamId:'team-01'}]}));
  await page.route('**/api/campaigns/synthetic-refinement**',async route=>{
   const url=new URL(route.request().url()),data=route.request().method()==='POST'?route.request().postDataJSON():null;
   if(url.pathname.endsWith('/return'))return route.fulfill({json:{lobby:{sessionId:data?.nextEvent?'synthetic-third':'synthetic-second'},membership:{deviceId:'synthetic-device'}}});
   const operation=url.pathname.endsWith('/draft')?'draft':url.pathname.endsWith('/commit')?'commit':url.pathname.endsWith('/close')?'close':null;
   if(operation){if(operation==='draft')drafts.push(data);if(operation==='commit')commits.push(data.commandId);const result=await request.post(fixture,{data:{operation,command:data}});expect(result.ok(),await result.text()).toBeTruthy();state=await result.json();if(operation==='commit'&&commits.length===1)return route.abort('failed');return route.fulfill({json:operation==='close'?state.campaign:state.development});}
   return route.fulfill({json:url.pathname.includes('/development/teams/')?state.development:state.campaign});
  });
  await page.goto('/campaigns/synthetic-refinement');await expect(page.getByText('Synthetic public-domain fixture privacy',{exact:false})).toBeVisible();await page.getByRole('button',{name:'Open private development for McLaren',exact:true}).click();
  const own=page.getByRole('region',{name:'Your private development',exact:true});await expect(own.getByText('82 credits available',{exact:true})).toBeVisible();
  const expectedQuote=state.development.refinementOptions.find((quote:{refinement:string})=>quote.refinement===kind).id;await own.getByRole('button',{name:'Draft '+label+' from v1',exact:true}).click();await expect(own).toContainText('Saved refinement: '+label);expect(drafts).toHaveLength(1);expect(drafts[0]).toMatchObject({actionKind:'PackageRefinement',package:{packageId:state.source.packageId,version:1},refinement:kind,quoteId:expectedQuote});if(kind==='strengthen')await own.screenshot({path:captures+'/s15-03-synthetic-refinement-options-360.png'});await expect(own.getByText('82 credits available',{exact:true})).toBeVisible();
  await own.getByRole('button',{name:'Commit package refinement · 10 credits',exact:true}).click();await expect(page.getByRole('alert')).toBeVisible();await own.getByRole('button',{name:'Retry same development commitment',exact:true}).click();expect(commits).toHaveLength(2);expect(commits[0]).toBe(commits[1]);await expect(own.getByText('72 credits available',{exact:true})).toBeVisible();
  await page.reload();await page.getByRole('button',{name:'Open private development for McLaren',exact:true}).click();await expect(own).toContainText('reserved v2');await page.getByRole('button',{name:'Host: close development',exact:true}).click();await page.getByRole('button',{name:'Review private project summary for McLaren',exact:true}).click();await expect(own).toContainText('purchased source v1 preserved');
  const result=await(await request.post(fixture,{data:{operation:'snapshot',command:{}}})).json();expect(result.library.map((p:{version:number})=>p.version)).toEqual([1,2]);expect(result.ledger.filter((entry:{reason:string})=>entry.reason==='race-package-development')).toHaveLength(2);expect(result.frozen.source.selectedPackage.version).toBe(1);expect(result.frozen.cars[0].source.car.ratings.highSpeedAero).toBe(86);
  expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await own.screenshot({path:captures+'/s15-03-synthetic-'+kind+'-360.png'});
 });
}
