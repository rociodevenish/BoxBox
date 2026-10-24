import {test,expect,Page} from '@playwright/test';
import {mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
const evidence=resolve(__dirname,'../../../../.tools/reports/gallery/s10-05');
async function household(page:Page,preset:string){
 await page.setViewportSize({width:360,height:800});await page.emulateMedia({reducedMotion:'reduce'});await page.goto('/shared/new');
 await page.getByLabel('Your name',{exact:true}).fill('Weather host');await page.getByLabel('Email',{exact:true}).fill('weather-advice-'+Date.now()+'@example.test');await page.getByLabel('Password',{exact:true}).fill('Local-test-password-123');await page.getByRole('button',{name:'Create account',exact:true}).click();
 await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();await page.getByRole('combobox',{name:'Preset',exact:true}).selectOption(preset);await page.getByRole('combobox',{name:'Race distance',exact:true}).selectOption('10');await page.getByRole('button',{name:'Create lobby',exact:true}).click();await page.getByRole('button',{name:'Claim McLaren',exact:true}).click();
 const settings=page.getByRole('region',{name:'Player help settings'});await settings.getByRole('combobox',{name:'Help level',exact:true}).selectOption('Guided');await settings.getByRole('button',{name:'Save help for Weather host',exact:true}).click();
 return new URL(page.url()).pathname.split('/').at(-1)!;
}
async function confirm(page:Page){await page.getByRole('button',{name:'Approve Plan',exact:true}).click();await page.getByRole('button',{name:'Confirm plan',exact:true}).click();}
for(const preset of ['open-equal','restricted-equal','open-mixed'])test(preset+' Guided weather decisions preserve uncertainty, two-car controls and public facts',async({page,context})=>{
 mkdirSync(evidence,{recursive:true});const id=await household(page,preset);
 await page.getByRole('button',{name:'Start race',exact:true}).click();await page.getByRole('link',{name:'Private controller for McLaren',exact:true}).click();
 const help=page.getByRole('region',{name:'Player assistance'});await expect(help).toContainText('Guided help');await expect(help).toContainText('uncertain');
 await expect(page.getByRole('region',{name:'Team forecast'})).toContainText('uncertainty range');await expect(page.locator('app-two-car-controller .car')).toHaveCount(2);await confirm(page);
 await expect(page.getByRole('region',{name:'Weather reaction'})).toBeVisible();await expect(help).toContainText('coarse current suitability choice');
 const before=await(await page.request.get('/api/sessions/'+id)).json();await page.reload();await expect(help).toContainText('uncertain');const after=await(await page.request.get('/api/sessions/'+id)).json();expect(after.weatherReaction.identity).toBe(before.weatherReaction.identity);
 const cars=page.locator('app-two-car-controller .car');await expect(cars).toHaveCount(2);
 if(preset==='open-mixed'){
  expect(before.weatherReaction.causes).toEqual(['Weather','SafetyCar']);await expect(page.getByRole('region',{name:'Weather reaction'})).toContainText('Conserve and Neutral');
  for(let i=0;i<2;i++)if(await cars.nth(i).getByRole('button',{name:'Conserve',exact:true}).isEnabled()){await expect(cars.nth(i).getByRole('group',{name:'Pace',exact:true}).getByRole('button')).toHaveCount(1);}
 }else{
  await help.getByRole('button',{name:'Use suggested two-car plan',exact:true}).click();
  
  if(preset==='open-equal')for(let i=0;i<2;i++){await cars.nth(i).getByRole('button',{name:'Pit',exact:true}).click();await cars.nth(i).getByRole('button',{name:'New compound Intermediate',exact:true}).click();}
 }
 await page.screenshot({path:resolve(evidence,preset+'-guided-phone-360.png'),fullPage:true});expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);
 const display=await context.newPage();await display.setViewportSize({width:1920,height:1080});await display.emulateMedia({reducedMotion:'reduce'});await display.goto('/race/display/'+id);
 await expect(display.getByRole('region',{name:'Team forecast'})).toHaveCount(0);await expect(display.getByRole('region',{name:'Player assistance'})).toHaveCount(0);await expect(display.getByText(/Observed .*band at/)).toBeVisible();await expect(display.getByText('No overtakes or pit events reported in this race window.')).toHaveCount(0);
 const rows=display.locator('app-timing-tower tbody tr');await expect(rows).toHaveCount(22);for(const row of await rows.all()){const bounds=await row.boundingBox();expect(bounds!.y+bounds!.height).toBeLessThanOrEqual(1080);}
 await display.screenshot({path:resolve(evidence,preset+'-public-1920x1080.png'),fullPage:false});
 await confirm(page);await expect.poll(async()=>(await(await page.request.get('/api/sessions/'+id)).json()).raceTime).toBeGreaterThan(before.raceTime);
 if(preset==='open-mixed'){const next=await(await page.request.get('/api/sessions/'+id)).json();if(next.weatherReaction?.safetyCarActive)expect(next.weatherReaction.safetyCarUntil).toBe(before.weatherReaction.safetyCarUntil??before.raceTime+180);}
 else if(preset==='open-equal'){await expect(page.getByRole('heading',{name:'Observed conditions changed: Wet'})).toBeVisible();await expect(cars.nth(0)).toContainText('Committed Intermediate stop queued');}
 else await expect(page.getByRole('heading',{name:'Observed conditions changed: Dry'})).toBeVisible();
});
