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
