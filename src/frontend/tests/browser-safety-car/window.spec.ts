import {test,expect} from '@playwright/test';
import {mkdirSync} from 'node:fs';
test('Safety Car offers narrow both-car choices, preserves next-lap queue on refresh and has readable reduced-motion display',async({page,browser})=>{
    await page.setViewportSize({width:360,height:800});await page.emulateMedia({reducedMotion:'reduce'});
    await page.goto('/race/new');await page.getByLabel('Race distance').selectOption('10');
    await page.getByRole('button',{name:'Create race'}).click();const controller=(await page.getByRole('link',{name:'Open controller'}).getAttribute('href'))!;
    await page.goto(controller);await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
    await expect(page.getByRole('heading',{name:'Safety Car deployed'})).toBeVisible();
    await expect(page.getByText('Safety Car pit window',{exact:true})).toBeVisible();
    await expect(page.getByRole('group',{name:'Pace',exact:true})).toHaveCount(0);
    const first=page.getByRole('region',{name:'Car 1 — Driver 1'});const second=page.getByRole('region',{name:'Car 2 — Driver 2'});
    await first.getByRole('button',{name:'Pit',exact:true}).click();await first.getByRole('button',{name:'New compound Hard',exact:true}).click();
    await expect(second.getByRole('button',{name:'Stay Out',exact:true})).toHaveAttribute('aria-pressed','true');
    mkdirSync('../../.tools/reports/s09-03',{recursive:true});await page.screenshot({path:'../../.tools/reports/s09-03/safety-car-360.png',fullPage:true});
    await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
    await expect(first).toContainText('Committed Hard stop queued');await page.reload();await expect(first).toContainText('Committed Hard stop queued');
    await expect(first.getByRole('button',{name:'Pit',exact:true})).toBeDisabled();
    const id=new URL(page.url()).pathname.split('/').at(-1);const display=await browser.newPage({viewport:{width:1920,height:1080},reducedMotion:'reduce'});
    await display.goto('/race/display/'+id);await expect(display.getByRole('heading',{name:'Safety Car deployed'})).toBeVisible();
    await expect(display.getByText('Safety Car choices locked',{exact:true})).toBeVisible();
    await display.screenshot({path:'../../.tools/reports/s09-03/safety-car-1920.png',fullPage:true});await display.close();
});
test('queued stop and retired car retain factual status and require no replacement choice',async({page})=>{
    await page.setViewportSize({width:360,height:800});await page.emulateMedia({reducedMotion:'reduce'});
    for(const preset of ['open-equal','restricted-equal']){
        await page.goto('/race/new');await page.getByLabel('Preset').selectOption(preset);await page.getByLabel('Race distance').selectOption('10');
        await page.getByRole('button',{name:'Create race'}).click();await page.goto((await page.getByRole('link',{name:'Open controller'}).getAttribute('href'))!);
        if(preset==='open-equal'){const second=page.getByRole('region',{name:'Car 2 — Driver 2'});await second.getByRole('button',{name:'Pit',exact:true}).click();await second.getByRole('button',{name:'New compound Hard',exact:true}).click();}
        await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
        await expect(page.getByText('Safety Car pit window',{exact:true})).toBeVisible();
        const unavailable=page.locator('.car').filter({hasText:preset==='open-equal'?'Committed Hard stop queued':'Retired - no instruction required.'});
        await expect(unavailable).toHaveCount(preset==='open-equal'?1:2);for(const car of await unavailable.all()){await expect(car.getByRole('button',{name:'Pit',exact:true})).toBeDisabled();await expect(car.getByRole('button',{name:'Stay Out',exact:true})).toBeDisabled();}
        await page.screenshot({path:'../../.tools/reports/s09-03/'+(preset==='open-equal'?'queued':'retired')+'-360.png',fullPage:true});
        if(preset==='open-equal'){await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();}
        await expect(page.getByText('Safety Car choices locked',{exact:true})).toBeVisible();
    }
});
