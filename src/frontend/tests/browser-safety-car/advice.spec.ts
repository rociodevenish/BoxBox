import {test,expect} from '@playwright/test';
import {captures} from '../../playwright.runtime';
import {join} from 'node:path';

test('Assisted Safety Car advice stays narrow, executable and frozen across accepted-lock refresh',async({page,browser})=>{
 test.skip(!process.env['F1_SAFETY_CAR_ADVICE_AUTH'],'Requires isolated Auth/Firestore emulators.');
 await page.setViewportSize({width:360,height:800});await page.emulateMedia({reducedMotion:'reduce'});await page.goto('/shared/new');
 await page.getByLabel('Your name').fill('Incident host');await page.getByLabel('Email').fill('incident-'+Date.now()+'@example.test');await page.getByLabel('Password').fill('Local-test-password-123');await page.getByRole('button',{name:'Create account',exact:true}).click();
 await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();await page.getByLabel('Race distance').selectOption('10');
 const creation=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/lobbies'&&r.request().method()==='POST');await page.getByRole('button',{name:'Create lobby',exact:true}).click();const response=await creation;const created=await response.json();const headers={Authorization:response.request().headers()['authorization']};
 await page.getByRole('button',{name:'Claim McLaren',exact:true}).click();
 const settings=page.getByRole('region',{name:'Player help settings'});await settings.getByRole('combobox',{name:'Help level',exact:true}).selectOption('Assisted');await settings.getByRole('button',{name:'Save help for Incident host'}).click();
 await page.getByLabel('Shared participant name').fill('Sam');await page.getByRole('button',{name:'Add shared participant'}).click();const ferrari=page.getByRole('region',{name:'Ferrari',exact:true});await ferrari.getByRole('combobox',{name:'Assign Ferrari',exact:true}).selectOption({label:'Sam · shared participant'});await ferrari.getByRole('combobox',{name:'Control arrangement',exact:true}).selectOption('SharedTablet');await ferrari.getByRole('button',{name:'Save assignment'}).click();
 await page.getByRole('button',{name:'Start race',exact:true}).click();const controller=(await page.getByRole('link',{name:'Private controller for McLaren',exact:true}).getAttribute('href'))!;
 const id=created.lobby.sessionId,device=created.membership.deviceId;const teamRoute=`/api/lobbies/${id}/teams/team-02?deviceId=${device}`;const second=await(await page.request.get(teamRoute,{headers})).json();
 expect((await page.request.post(`/api/lobbies/${id}/teams/team-02/plans?deviceId=${device}`,{headers,data:{commandId:crypto.randomUUID(),phase:second.phase,revision:second.commandRevision,cars:second.cars.map((car:any)=>({driverId:car.driverId,pace:'Standard',pit:'Stay Out',compound:null,battle:'Neutral'}))}})).ok()).toBeTruthy();
 await page.goto(controller);await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();await expect(page.getByText('Safety Car pit window',{exact:true})).toBeVisible();
 const help=page.getByRole('region',{name:'Player assistance'});await expect(help).toContainText('Assisted help');await expect(help).toContainText('Racing is neutralised');await expect(help).not.toContainText('Neutral is the only');
 const frozen=await help.innerText();await expect(page.getByText(/Component condition: Good/).first()).toBeVisible();await expect(page.getByRole('group',{name:'Pace',exact:true})).toHaveCount(0);
 await help.getByRole('button',{name:'Use suggested two-car plan'}).click();await page.screenshot({path:join(captures,'s09-review-advice-360.png'),fullPage:true});expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);
 await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();await expect(page.getByText('Waiting for other teams…',{exact:true})).toBeVisible();await page.reload();await expect(page.getByText('Waiting for other teams…',{exact:true})).toBeVisible();await expect(help).toHaveText(frozen,{useInnerText:true});
 await expect(help.getByRole('button',{name:'Use suggested two-car plan'})).toBeDisabled();
 const display=await browser.newPage({viewport:{width:1920,height:1080},reducedMotion:'reduce'});await display.goto('/race/display/'+id);await expect(display.getByRole('region',{name:'Player assistance'})).toHaveCount(0);await expect(display.getByText(/Component condition:/)).toHaveCount(0);await display.close();
});
