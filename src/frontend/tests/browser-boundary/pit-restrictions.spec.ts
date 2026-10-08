import {test,expect} from '@playwright/test';
import {mkdirSync} from 'node:fs';
for(const {preset,status,compound} of [{preset:'open-equal',status:'queued',compound:'Medium'},{preset:'restricted-equal',status:'ongoing',compound:'Hard'}]) {
  test('real '+status+' stop disables replacement and rejected wire plan preserves both drafts',async({page})=>{
    await page.setViewportSize({width:360,height:800});
    await page.goto('/race/new');await page.getByLabel('Preset').selectOption(preset);await page.getByLabel('Race distance').selectOption('10');
    await page.getByRole('button',{name:'Create race'}).click();const link=await page.getByRole('link',{name:'Open controller'}).getAttribute('href');await page.goto(link!);
    const second=page.getByRole('region',{name:'Car 2 — Driver 02'});const first=page.getByRole('region',{name:'Car 1 — Driver 01'});
    await second.getByRole('button',{name:'Pit',exact:true}).click();await second.getByLabel('New compound').selectOption('Hard');
    await page.getByRole('button',{name:'Review team plan'}).click();await page.getByRole('button',{name:'Run phase with both cars'}).click();
    await expect(page.getByRole('heading',{name:'Phase 2 instructions'})).toBeVisible();
    await expect(page.getByRole('button',{name:'Continue to next phase'})).toHaveCount(0);
    await expect(second).toContainText('Pit stop '+status);await expect(second.getByRole('button',{name:'Pit',exact:true})).toBeDisabled();await expect(second.getByRole('button',{name:'Stay Out',exact:true})).toBeDisabled();
    await expect(second.getByLabel('Tyre compound '+compound,{exact:true})).toBeVisible();await expect(first.getByRole('button',{name:'Push',exact:true})).toBeEnabled();
    const id=new URL(page.url()).pathname.split('/').at(-1);const capability=new URL(page.url()).hash.slice('#capability='.length);const route='/api/sessions/'+id;
    const headers={Authorization:'Bearer '+capability};const beforePublic=await(await page.request.get(route)).text();const beforeTeam=await(await page.request.get(route+'/team',{headers})).text();
    await first.getByRole('button',{name:'Push',exact:true}).click();
    await page.route('**/plans',async request=>{const body=request.request().postDataJSON();body.cars[1].pit='Pit';body.cars[1].compound='Soft';await request.continue({postData:JSON.stringify(body)});});
    await page.getByRole('button',{name:'Review team plan'}).click();await page.getByRole('button',{name:'Run phase with both cars'}).click();
    await expect(page.getByRole('alert')).toContainText('pit instruction is unavailable');
    await expect(first.getByRole('button',{name:'Push',exact:true})).toHaveAttribute('aria-pressed','true');await expect(second).toContainText('Standard · Stay Out · Neutral');
    expect(await(await page.request.get(route)).text()).toBe(beforePublic);expect(await(await page.request.get(route+'/team',{headers})).text()).toBe(beforeTeam);
    mkdirSync('../../.tools/reports/browser-race/review-fixes',{recursive:true});
    await page.screenshot({path:'../../.tools/reports/browser-race/review-fixes/'+status+'-controller-360.png',fullPage:true});
  });
}
