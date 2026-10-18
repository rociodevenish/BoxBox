import {test,expect} from '@playwright/test';
import {mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
const captures=resolve(__dirname,'../../../../.tools/reports/gallery');
test('weather reaction retains complete both-car controls and refreshes the same observed window',async({page,context})=>{
  await page.setViewportSize({width:360,height:800});await page.emulateMedia({reducedMotion:'reduce'});
  await page.goto('/race/new');await page.getByLabel('Race distance').selectOption('10');
  await page.getByRole('button',{name:'Create race'}).click();
  const controller=(await page.getByRole('link',{name:'Open controller'}).getAttribute('href'))!;
  const display=(await page.getByRole('link',{name:'Open public display'}).getAttribute('href'))!;
  await page.goto(controller);await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
  await expect(page.getByRole('heading',{name:'Observed conditions changed: Intermediate'})).toBeVisible();
  await expect(page.getByRole('group',{name:'Pace',exact:true})).toHaveCount(2);
  await page.reload();await expect(page.getByRole('region',{name:'Weather reaction'})).toContainText('22.0s');
  const cars=page.locator('app-two-car-controller .car');
  for(const car of [cars.nth(0),cars.nth(1)]){
    await car.getByRole('button',{name:'Pit',exact:true}).click();await car.getByRole('button',{name:'New compound Intermediate',exact:true}).click();
  }
  await cars.nth(0).getByRole('button',{name:'Push',exact:true}).click();await cars.nth(1).getByRole('button',{name:'Conserve',exact:true}).click();
  mkdirSync(captures,{recursive:true});await page.screenshot({path:resolve(captures,'s10-03-controller-360.png'),fullPage:true});
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
  const publicPage=await context.newPage();await publicPage.setViewportSize({width:1920,height:1080});await publicPage.emulateMedia({reducedMotion:'reduce'});await publicPage.goto(display);
  await expect(publicPage.getByRole('region',{name:'Weather reaction'})).toContainText('22.0s');
  await expect(publicPage.getByRole('region',{name:'Team forecast'})).toHaveCount(0);
  await publicPage.screenshot({path:resolve(captures,'s10-03-display-1920.png'),fullPage:true});
  await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
  await expect(page.getByRole('heading',{name:'Observed conditions changed: Wet'})).toBeVisible();
  await expect(page.getByRole('region',{name:'Weather reaction'})).toContainText('72.0s');
  await expect(cars.nth(0)).toContainText('Committed Intermediate stop queued');
  await expect(cars.nth(1)).toContainText('Committed Intermediate stop queued');
  await expect(cars.nth(0).getByRole('button',{name:'Pit',exact:true})).toBeDisabled();
});

test('drying opens a fresh complete window at the lower hysteresis edge',async({page})=>{
  await page.setViewportSize({width:360,height:800});await page.emulateMedia({reducedMotion:'reduce'});
  await page.goto('/race/new');await page.getByLabel('Preset').selectOption('restricted-equal');await page.getByLabel('Race distance').selectOption('10');
  await page.getByRole('button',{name:'Create race'}).click();await page.goto((await page.getByRole('link',{name:'Open controller'}).getAttribute('href'))!);
  await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
  await expect(page.getByRole('region',{name:'Weather reaction'})).toContainText('12.0s');
  await expect(page.getByRole('heading',{name:'Observed conditions changed: Intermediate'})).toBeVisible();
  await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
  await expect(page.getByRole('heading',{name:'Observed conditions changed: Dry'})).toBeVisible();
  await expect(page.getByRole('region',{name:'Weather reaction'})).toContainText('62.0s');
  await page.reload();await expect(page.getByRole('group',{name:'Pace',exact:true})).toHaveCount(2);
});
