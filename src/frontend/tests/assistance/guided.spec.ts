import {test,expect} from '@playwright/test';
import {captures} from '../../playwright.runtime';
import {join} from 'node:path';
test('Guided player sees a fresh executable two-car plan, retains uncertain choices and locks it on a phone',async({page,browser})=>{
 await page.setViewportSize({width:360,height:800});await page.goto('/shared/new');
 await page.getByLabel('Your name').fill('Guided host');await page.getByLabel('Email').fill('guided-'+Date.now()+'@example.test');await page.getByLabel('Password').fill('Local-test-password-123');await page.getByRole('button',{name:'Create account',exact:true}).click();
 await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 const creation=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/lobbies'&&r.request().method()==='POST');await page.getByRole('button',{name:'Create lobby',exact:true}).click();const created=await(await creation).json();
 await page.getByRole('region',{name:'McLaren',exact:true}).getByRole('button',{name:'Claim McLaren',exact:true}).click();
 const settings=page.getByRole('region',{name:'Player help settings'});await settings.getByRole('combobox',{name:'Help level',exact:true}).selectOption('Guided');await settings.getByLabel('Accept saved recommendation on expiry').check();await settings.getByRole('button',{name:'Save help for Guided host'}).click();await expect(settings.getByRole('combobox',{name:'Help level',exact:true})).toHaveValue('Guided');
 await page.getByLabel('Shared participant name').fill('Sam');await page.getByRole('button',{name:'Add shared participant'}).click();
 const ferrari=page.getByRole('region',{name:'Ferrari',exact:true});await ferrari.getByRole('combobox',{name:'Assign Ferrari',exact:true}).selectOption({label:'Sam · shared participant'});await ferrari.getByRole('combobox',{name:'Control arrangement',exact:true}).selectOption('SharedTablet');await ferrari.getByRole('button',{name:'Save assignment'}).click();await expect(ferrari).toContainText('Approved');
 await page.getByRole('button',{name:'Start race',exact:true}).click();const link=await page.getByRole('link',{name:'Private controller for McLaren',exact:true}).getAttribute('href');await page.goto(link!);
 const help=page.getByRole('region',{name:'Player assistance'});await expect(help).toContainText('Guided help');await expect(help).toContainText('Saved for Phase 1');await expect(help).toContainText('Tyre condition is an estimate');await expect(help).toContainText('All legal choices remain available');
 const first=page.getByRole('region',{name:'Car 1 — Driver 1',exact:true});await expect(first.getByRole('group',{name:'Pace',exact:true}).getByRole('button')).toHaveCount(3);
 await page.screenshot({path:join(captures,'assistance-guided-phone-360.png'),fullPage:true});expect(await page.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);
 await help.getByRole('button',{name:'Use suggested two-car plan'}).click();await expect(first.getByRole('button',{name:'Pit',exact:true})).toHaveAttribute('aria-pressed','true');await expect(first.getByRole('button',{name:'New compound Soft'})).toHaveAttribute('aria-pressed','true');
 await page.getByRole('button',{name:'Approve Plan',exact:true}).click();await page.getByRole('button',{name:'Confirm plan',exact:true}).click();await expect(page.getByRole('heading',{name:'Waiting for other teams…',exact:true})).toBeVisible();await page.reload();await expect(page.getByRole('heading',{name:'Waiting for other teams…',exact:true})).toBeVisible();
 const display=await browser.newPage({viewport:{width:1920,height:1080}});await display.goto('/race/display/'+created.lobby.sessionId);await expect(display.getByRole('region',{name:'Player assistance'})).toHaveCount(0);await display.screenshot({path:join(captures,'assistance-public-1920.png'),fullPage:true});expect(await display.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(1920);await display.close();
});



