import {test,expect} from '@playwright/test';
import {mkdirSync} from 'node:fs';
import {resolve} from 'node:path';
const captures=resolve(__dirname,'../../../../.tools/reports/gallery');
test('observed conditions and uncertain own forecast survive refresh and advance only with the race',async({page,context})=>{
  await page.setViewportSize({width:360,height:800});await page.emulateMedia({reducedMotion:'reduce'});
  await page.goto('/race/new');await page.getByLabel('Race distance').selectOption('10');
  await page.getByRole('button',{name:'Create race'}).click();
  const controller=(await page.getByRole('link',{name:'Open controller'}).getAttribute('href'))!;
  const display=(await page.getByRole('link',{name:'Open public display'}).getAttribute('href'))!;
  await page.goto(controller);
  await expect(page.getByRole('region',{name:'Observed weather'})).toContainText('Track wetness 25%');
  const forecast=page.getByRole('region',{name:'Team forecast'});
  await expect(forecast).toContainText('Predicted rain');await expect(forecast).toContainText('next 5 minutes');
  await expect(forecast).toContainText('Low confidence');
  const before=(await forecast.textContent())!;await page.reload();await expect(forecast).toHaveText(before);
  mkdirSync(captures,{recursive:true});
  await page.screenshot({path:resolve(captures,'s10-01-controller-360.png'),fullPage:true});
  expect(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth)).toBe(true);
  const publicPage=await context.newPage();await publicPage.setViewportSize({width:1920,height:1080});await publicPage.goto(display);
  await expect(publicPage.getByRole('region',{name:'Observed weather'})).toBeVisible();
  await expect(publicPage.getByRole('region',{name:'Team forecast'})).toHaveCount(0);
  await publicPage.screenshot({path:resolve(captures,'s10-01-display-1920.png'),fullPage:true});
  await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
  await expect(page.getByRole('region',{name:'Observed weather'})).not.toContainText('Track wetness 25%');
  await expect(forecast).not.toHaveText(before);
});

