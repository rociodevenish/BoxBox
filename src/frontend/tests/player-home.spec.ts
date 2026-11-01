import {test,expect} from '@playwright/test';
import {captures} from '../playwright.runtime';
test('player home explains the game and opens campaign setup on phone and desktop',async({page})=>{
 for(const width of [360,1920]){
  await page.setViewportSize({width,height:width===360?800:1080});await page.goto('/');
  await expect(page.getByRole('heading',{name:'Your team. Both drivers. Every decision.'})).toBeVisible();
  await expect(page.getByRole('link',{name:'Join a game',exact:true})).toHaveAttribute('href','/shared/join');
  expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(width);
  await page.screenshot({path:captures+'/player-home-'+width+'.png',fullPage:true});
  await page.getByRole('link',{name:'Create campaign',exact:true}).focus();await page.keyboard.press('Enter');await expect(page).toHaveURL(/\/campaigns\/new$/);
 }
});
test('gallery has an explicit development route and legacy fixture queries resolve there',async({page})=>{
 await page.goto('/dev/gallery');await expect(page.getByRole('heading',{name:'Driver gallery',exact:true})).toBeVisible();
 for(const view of ['public','controller','results']){await page.goto('/?view='+view);await expect(page).toHaveURL(new RegExp('/dev/gallery\\?view='+view+'$'));await expect(page.getByRole('link',{name:'Back to gallery',exact:true})).toHaveAttribute('href','/dev/gallery');if(view==='public'){await page.setViewportSize({width:1920,height:1080});await page.screenshot({path:captures+'/player-home-public-1920.png',fullPage:true});}}
});
