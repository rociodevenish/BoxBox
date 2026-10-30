import {test,expect} from '@playwright/test';
test('Quick join preserves a code and offers correction without hosting',async({page})=>{
 await page.goto('/'); await page.getByLabel('Join code',{exact:true}).fill('WRONG-CODE');
 await page.getByRole('button',{name:'Quick join',exact:true}).click();
 await expect(page).toHaveURL(/\/shared\/join\?code=WRONG-CODE$/);
 await expect(page.getByLabel('Join code',{exact:true})).toHaveValue('WRONG-CODE');
 await page.getByLabel('Your name').fill('Guest'); await page.getByRole('button',{name:'Join as guest'}).click();
 await expect(page.getByRole('alert')).toContainText(/check.*code|ask.*host/i);
 await expect(page.getByLabel('Join code',{exact:true})).toBeEditable();
 await expect(page.getByRole('heading',{name:'Household lobby',exact:true})).toHaveCount(0);
 await page.getByRole('link',{name:'Back to home',exact:true}).click();await expect(page).toHaveURL(/\/$/);
});

test('home account entry returns home, guest joins by code and invitation remains optional',async({page,browser})=>{
 const email='join-home-'+Date.now()+'@example.test';
 await page.goto('/');await page.getByRole('link',{name:'Sign in',exact:true}).click();
 await page.getByLabel('Your name').fill('Home host');await page.getByLabel('Email',{exact:true}).fill(email);await page.getByLabel('Password',{exact:true}).fill('Local-test-password-123');await page.getByRole('button',{name:'Create account',exact:true}).click();
 await expect(page).toHaveURL(/\/$/);await expect(page.getByRole('link',{name:'Join another game',exact:true})).toBeVisible();
 await page.getByRole('link',{name:'Create campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Create a short campaign'})).toBeVisible();
 await page.goto('/shared/new');await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();await page.getByRole('button',{name:'Create lobby',exact:true}).click();
 await expect(page.getByRole('heading',{name:'Household lobby',exact:true})).toBeVisible();
 const invitation=(await page.getByRole('link',{name:'Join link',exact:true}).getAttribute('href'))!;const code=new URL(invitation,page.url()).searchParams.get('code')!;
 const context=await browser.newContext({viewport:{width:360,height:800}});const guest=await context.newPage();
 await guest.goto('/');await guest.getByLabel('Join code',{exact:true}).fill(code);await guest.getByRole('button',{name:'Quick join',exact:true}).focus();await guest.keyboard.press('Enter');await expect(guest.getByLabel('Join code',{exact:true})).toHaveValue(code);
 await guest.getByLabel('Your name').fill('Home guest');await guest.getByRole('button',{name:'Join as guest',exact:true}).click();await expect(guest.getByRole('heading',{name:'Household lobby',exact:true})).toBeVisible();
 await expect(guest.getByRole('region',{name:'Host controls',exact:true})).toHaveCount(0);await expect(guest.getByRole('link',{name:/Private controller/})).toHaveCount(0);
 expect(await guest.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);
 await guest.screenshot({path:'.tools/reports/gallery/home-join-lobby-360.png',fullPage:true});
 await guest.getByRole('link',{name:'Back to home',exact:true}).click();await expect(guest.getByRole('button',{name:'Quick join',exact:true})).toBeVisible();await guest.screenshot({path:'.tools/reports/gallery/home-join-360.png',fullPage:true});
 const directContext=await browser.newContext();const direct=await directContext.newPage();await direct.goto(invitation);await expect(direct.getByLabel('Join code',{exact:true})).toHaveValue(code);await direct.getByLabel('Your name').fill('Invitation guest');await direct.getByRole('button',{name:'Join as guest',exact:true}).click();await expect(direct.getByRole('heading',{name:'Household lobby',exact:true})).toBeVisible();
 await page.goto('/');await page.getByRole('button',{name:'Sign out',exact:true}).click();await expect(page.getByRole('button',{name:'Quick join',exact:true})).toBeVisible();await expect(page.getByRole('link',{name:'Join another game',exact:true})).toHaveCount(0);
 await directContext.close();await context.close();
});
