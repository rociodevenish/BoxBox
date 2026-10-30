import {test,expect} from '@playwright/test';
test('create keeps its destination across registration and subsequent sign-in',async({page})=>{
 const email='home-'+Date.now()+'@example.test';
 await page.goto('/');await page.getByRole('link',{name:'Create campaign',exact:true}).click();await page.getByRole('link',{name:'Sign in',exact:true}).click();
 await page.getByLabel('Your name').fill('Host');await page.getByLabel('Email',{exact:true}).fill(email);await page.getByLabel('Password',{exact:true}).fill('Local-test-password-123');await page.getByRole('button',{name:'Create account',exact:true}).click();
 await expect(page).toHaveURL(/\/campaigns\/new$/);await expect(page.getByRole('heading',{name:'Create a short campaign'})).toBeVisible();
 await page.goto('/shared/create');await page.getByRole('button',{name:'Sign out',exact:true}).click();await expect(page.getByRole('button',{name:'Create account',exact:true})).toBeVisible();await page.goto('/campaigns/new');await page.getByRole('link',{name:'Sign in',exact:true}).click();
 await page.getByLabel('Email',{exact:true}).fill(email);await page.getByLabel('Password',{exact:true}).fill('Local-test-password-123');await page.getByRole('button',{name:'Sign in',exact:true}).click();await expect(page).toHaveURL(/\/campaigns\/new$/);
 await page.goto('/');await page.getByRole('link',{name:'Create campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Create a short campaign'})).toBeVisible();
});

for(const create of [false,true])for(const destination of ['/', '/campaigns/new'])test('guest Quick join can '+(create?'register':'sign in')+' and retain '+destination,async({page,browser})=>{
 const host=await browser.newContext();const hostPage=await host.newPage();const email='guest-transition-'+Date.now()+'@example.test';
 await hostPage.goto('/shared/create');await hostPage.getByLabel('Your name').fill('Existing host');await hostPage.getByLabel('Email',{exact:true}).fill(email);await hostPage.getByLabel('Password',{exact:true}).fill('Local-test-password-123');await hostPage.getByRole('button',{name:'Create account',exact:true}).click();await expect(hostPage.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();await hostPage.getByRole('button',{name:'Create lobby',exact:true}).click();
 const invite=await hostPage.getByRole('link',{name:'Join link',exact:true}).getAttribute('href');const code=new URL(invite!,hostPage.url()).searchParams.get('code')!;
 await page.goto('/');await page.getByLabel('Join code',{exact:true}).fill(code);await page.getByRole('button',{name:'Quick join',exact:true}).click();await page.getByLabel('Your name').fill('Guest');await page.getByRole('button',{name:'Join as guest',exact:true}).click();await expect(page.getByRole('heading',{name:'Household lobby',exact:true})).toBeVisible();
 await page.goto('/');if(destination!=='/')await page.getByRole('link',{name:'Create campaign',exact:true}).click();await page.getByRole('link',{name:'Sign in',exact:true}).click();await expect(page.getByRole('button',{name:'Create account',exact:true})).toBeEnabled();
 await page.getByLabel('Email',{exact:true}).fill(create?'new-'+email:email);await page.getByLabel('Password',{exact:true}).fill('Local-test-password-123');await page.getByRole('button',{name:create?'Create account':'Sign in',exact:true}).click();await expect(page).toHaveURL(new RegExp(destination==='/'?'/$':'/campaigns/new$'));
 if(destination==='/')await expect(page.getByRole('button',{name:'Sign out',exact:true})).toBeVisible();else await expect(page.getByRole('heading',{name:'Create a short campaign'})).toBeVisible();await host.close();
});
