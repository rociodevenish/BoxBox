import {test,expect} from '@playwright/test';
test('outcome precedes next decisions with updated one-lap options and replay never locks them',async({page})=>{
 await page.goto('/race/new');await page.getByRole('button',{name:'Create race'}).click();await page.goto((await page.getByRole('link',{name:'Open controller'}).getAttribute('href'))!);
 await expect(page.getByRole('heading',{name:'Decisions for the next 5 laps',exact:true})).toBeVisible();
 await page.getByRole('button',{name:'Approve Plan',exact:true}).click();await page.getByRole('button',{name:'Confirm plan',exact:true}).click();
 await expect(page.getByText('Resolving play…',{exact:true})).toBeVisible();await expect(page.getByRole('button',{name:'Approve Plan',exact:true})).toHaveCount(0);
 await page.getByRole('button',{name:'Skip playback'}).click();await expect(page.getByRole('heading',{name:'Decisions for the next 1 lap',exact:true})).toBeVisible();
 await expect(page.getByRole('button',{name:'Approve Plan',exact:true})).toBeEnabled();await expect(page.locator('main').first()).not.toContainText(/\bphase\b/i);
 await page.getByRole('button',{name:'Replay feedback'}).click();await expect(page.getByRole('button',{name:'Approve Plan',exact:true})).toBeEnabled();
 await page.getByRole('button',{name:'Skip playback'}).click();await page.reload();await expect(page.getByRole('heading',{name:'Decisions for the next 1 lap',exact:true})).toBeVisible();await expect(page.getByRole('button',{name:'Skip playback'})).toHaveCount(0);
});
test('controller exposes bounded vicinity, source card details, recorded pace and completed stops',async({page,context})=>{
 await page.setViewportSize({width:360,height:800});await page.emulateMedia({reducedMotion:'reduce'});
 await page.goto('/race/new');await page.getByRole('button',{name:'Create race'}).click();const url=await page.getByRole('link',{name:'Open public display'}).getAttribute('href');await page.goto((await page.getByRole('link',{name:'Open controller'}).getAttribute('href'))!);
 const info=page.getByRole('region',{name:'Driver 1 race information',exact:true});await expect(info.getByRole('row')).toHaveCount(4);await expect(info).toContainText('No full lap times recorded yet.');await expect(page.getByRole('region',{name:'Car 1 — Driver 1',exact:true}).getByRole('img',{name:'Driver 1 portrait'})).toBeVisible();
 await info.getByRole('button',{name:'Driver 2',exact:true}).click();const dialog=page.getByRole('dialog',{name:'Driver analysis',exact:true});await expect(dialog).toBeVisible();await expect(dialog).toContainText('Race Pace');await expect(dialog).toContainText('/ 100');await dialog.getByRole('button',{name:'Close driver analysis',exact:true}).click();
 const car=page.getByRole('region',{name:'Car 2 — Driver 2'});await car.getByRole('button',{name:'Pit',exact:true}).click();const compound=car.getByRole('button',{name:'New compound Hard',exact:true});await page.keyboard.press("Tab");await compound.focus();await page.keyboard.press('Space');await expect(compound).toHaveAttribute('aria-pressed','true');
 await page.getByRole('button',{name:'Approve Plan',exact:true}).click();await page.getByRole('button',{name:'Confirm plan',exact:true}).click();await expect(page.getByRole('heading',{name:'Decisions for the next 1 lap',exact:true})).toBeVisible();
 await expect(info.getByRole('list',{name:'Recent recorded lap times'}).getByRole('listitem')).toHaveCount(4);await expect(info).not.toContainText('Lap 1 ·');
 const display=await context.newPage();await display.setViewportSize({width:1920,height:1080});await display.emulateMedia({reducedMotion:'reduce'});await display.goto(url!);await expect(display.getByRole('row').filter({has:display.getByText('Driver 2',{exact:true})}).getByRole('cell',{name:'1 completed stop',exact:true})).toBeVisible();await expect(display.getByLabel('Fastest lap',{exact:true})).toHaveCount(1);
 const {mkdirSync}=await import('node:fs');mkdirSync('../../.tools/reports/controller-followup',{recursive:true});await page.screenshot({path:'../../.tools/reports/controller-followup/controller-360.png',fullPage:true});await display.screenshot({path:'../../.tools/reports/controller-followup/display-1920.png'});
});
