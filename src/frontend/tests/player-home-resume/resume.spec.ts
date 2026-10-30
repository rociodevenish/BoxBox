import {test,expect} from '@playwright/test';
import {captures} from '../../playwright.runtime';
test('fresh browser discovers hosted games, resumes through return, and home retries without stale account data',async({page,browser})=>{
 const email='resume-'+Date.now()+'@example.test',password='Local-test-password-123';
 await page.goto('/shared/new');await page.getByLabel('Your name').fill('Returning Alex');await page.getByLabel('Email').fill(email);await page.getByLabel('Password').fill(password);await page.getByRole('button',{name:'Create account',exact:true}).click();await expect(page.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 await page.getByRole('link',{name:'Campaigns',exact:true}).click();await page.getByRole('button',{name:'Create campaign',exact:true}).click();await expect(page.getByRole('heading',{name:'Campaign created',exact:true})).toBeVisible();const campaign=page.url();
 const fresh=await browser.newContext({viewport:{width:360,height:800}});const phone=await fresh.newPage();
 await phone.goto('/shared/new');await phone.getByLabel('Your name').fill('Returning Alex');await phone.getByLabel('Email').fill(email);await phone.getByLabel('Password').fill(password);await phone.getByRole('button',{name:'Sign in',exact:true}).click();await expect(phone.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 await phone.goto('/');await expect(phone.getByRole('heading',{name:'Campaigns you host',exact:true})).toBeVisible();await expect(phone.getByRole('link',{name:/Reopen campaign/})).toHaveAttribute('href',new URL(campaign).pathname);
 expect(await phone.evaluate(()=>document.documentElement.scrollWidth)).toBeLessThanOrEqual(360);await phone.screenshot({path:captures+'/s20-03-resume-360.png',fullPage:true});
 await phone.getByRole('link',{name:/Reopen campaign/}).click();await expect(phone.getByRole('heading',{name:'Campaign created',exact:true})).toBeVisible();
 await phone.route('**/api/campaigns',route=>route.fulfill({status:503,json:{message:'Unavailable'}}));await phone.goto('/');await expect(phone.getByText('Your campaigns could not be loaded.')).toBeVisible();await phone.unroute('**/api/campaigns');await phone.getByRole('button',{name:'Retry campaigns',exact:true}).click();await expect(phone.getByRole('link',{name:/Reopen campaign/})).toBeVisible();
 await phone.setViewportSize({width:1920,height:1080});await phone.screenshot({path:captures+'/s20-03-resume-1920.png',fullPage:true});
 let release!:()=>void;const gate=new Promise<void>(resolve=>release=resolve);
 await phone.route('**/api/campaigns',async route=>{const response=await route.fetch();await gate;await route.fulfill({response});});
 await phone.reload();await expect(phone.getByRole('status').filter({hasText:'Loading your campaigns'})).toBeVisible();await phone.getByRole('button',{name:'Sign out',exact:true}).click();await expect(phone.getByRole('heading',{name:'Quick join',exact:true})).toBeVisible();release();
 await expect(phone.getByRole('heading',{name:'Your campaigns',exact:true})).toHaveCount(0);await expect(phone.getByText('Returning Alex’s household campaign')).toHaveCount(0);await phone.unroute('**/api/campaigns');
 await phone.goto('/shared/new');await phone.getByLabel('Your name').fill('Another account');await phone.getByLabel('Email').fill('empty-'+Date.now()+'@example.test');await phone.getByLabel('Password').fill(password);await phone.getByRole('button',{name:'Create account',exact:true}).click();await expect(phone.getByRole('button',{name:'Create lobby',exact:true})).toBeEnabled();
 await phone.goto('/');await expect(phone.getByText('You have no campaigns yet. Create a campaign or join another game to get started.')).toBeVisible();await expect(phone.getByRole('link',{name:/Reopen campaign/})).toHaveCount(0);await expect(phone.getByRole('link',{name:'Create campaign',exact:true})).toBeVisible();await expect(phone.getByRole('link',{name:'Join another game',exact:true})).toBeVisible();
 await fresh.close();
});

test('member reenters an existing household campaign from a separate home group',async({page,request})=>{
 const config=await(await request.get('/api/auth/config')).json();
 const signup=await request.post(config.emulatorUrl+'/identitytoolkit.googleapis.com/v1/accounts:signUp?key='+config.apiKey,{data:{email:'host-'+Date.now()+'@example.test',password:'Local-test-password-123',returnSecureToken:true}});expect(signup.ok()).toBeTruthy();const headers={Authorization:'Bearer '+(await signup.json()).idToken};
 const creation=await request.post('/api/campaigns',{headers,data:{commandId:crypto.randomUUID(),name:'Household host',laps:6,personnelMode:'preset',calendar:'short',mode:'Competitive',seed:123}});expect(creation.ok(),await creation.text()).toBeTruthy();const campaign=await creation.json();
 await page.goto('/shared/join?code='+encodeURIComponent(campaign.weekend.lobby.joinCode));await page.getByLabel('Your name').fill('Returning member');await page.getByLabel('Email').fill('member-'+Date.now()+'@example.test');await page.getByLabel('Password').fill('Local-test-password-123');await page.getByRole('button',{name:'Create account',exact:true}).click();await page.getByRole('button',{name:'Join as guest',exact:true}).click();await expect(page).toHaveURL(/shared\/lobby\//);
 await page.goto('/');await expect(page.getByRole('heading',{name:'Campaigns you joined',exact:true})).toBeVisible();await expect(page.getByRole('heading',{name:'Campaigns you host',exact:true})).toHaveCount(0);await expect(page.getByRole('link',{name:'Join another game',exact:true})).toBeVisible();
 await page.getByRole('link',{name:/Reenter campaign/}).click();await expect(page).toHaveURL('/campaigns/'+campaign.campaign.campaignId);await expect(page.getByRole('heading',{name:'Campaign created',exact:true})).toBeVisible();
});
