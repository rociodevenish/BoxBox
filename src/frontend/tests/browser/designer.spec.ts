import { test, expect } from '@playwright/test';
test('browser feedback reports separate client presentation samples and designer replay reproduces its race', async ({page,context})=>{
  await page.goto('/race/new');await page.getByRole('button',{name:'Create race'}).click();
  const controller=await page.getByRole('link',{name:'Open controller'}).getAttribute('href');
  const publicUrl=await page.getByRole('link',{name:'Open public display'}).getAttribute('href');
  const id=publicUrl!.split('/').at(-1);
  const display=await context.newPage();await display.goto(publicUrl!);await display.getByLabel('Playback seconds').fill('10');
  await page.goto(controller!);await page.getByRole('button',{name:'Review team plan'}).click();await page.getByRole('button',{name:'Run phase with both cars'}).click();
  await expect(display.getByRole('button',{name:'Skip playback'})).toBeVisible();await display.getByRole('button',{name:'Skip playback'}).click();
  const headers={'X-Designer-Key':'ephemeral-browser-test-key'};
  const record=async()=> (await page.request.get('/api/designer/sessions/'+id+'/record',{headers})).json();
  await expect.poll(async()=> (await record()).timing.presentationSamples.length).toBe(1);
  const sample=(await record()).timing.presentationSamples[0];
  expect(sample.source).toBe('client-reported-monotonic-page-clock');expect(sample.completion).toBe('skipped');expect(sample.durationMilliseconds).toBeGreaterThanOrEqual(0);
  const verified=await (await page.request.post('/api/designer/replay/verify',{headers,data:await record()})).json();expect(verified.matches).toBe(true);
});
