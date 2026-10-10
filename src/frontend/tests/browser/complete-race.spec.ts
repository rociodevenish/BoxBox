import { test, expect } from '@playwright/test';
import { mkdirSync } from 'node:fs';
test('full race feedback can skip and replay, automatic next window resets both drafts and finish preserves eligibility', async ({ page, context }) => {
    await page.setViewportSize({width:360,height:800});
    await page.goto('/race/new');
    await expect(page.getByText('Dry race rule: use at least two different tyre compounds.')).toBeVisible();
    await page.getByRole('button', { name: 'Create race' }).click();
    const controller = await page.getByRole('link', { name: 'Open controller' }).getAttribute('href');
    const publicUrl = await page.getByRole('link', { name: 'Open public display' }).getAttribute('href');
    const display = await context.newPage(); await display.setViewportSize({ width: 1920, height: 1080 }); await display.goto(publicUrl!);
    await display.getByLabel('Display settings',{exact:true}).click();await display.getByLabel('Playback seconds').fill('10');
    await page.goto(controller!);
    await page.getByRole('region', { name: 'Car 1 — Driver 1' }).getByRole('button', { name: 'Push', exact: true }).click();
    await page.getByRole('region', { name: 'Car 2 — Driver 2' }).getByRole('button', { name: 'Pit', exact: true }).click();
    await page.getByRole('button', { name: 'Approve Plan' }).click();
    await page.getByRole('button', { name: 'Confirm plan' }).click();
    await expect(display.getByText('Presenting race feedback', { exact: true })).toBeVisible();
    await expect(display.getByRole('region', {name:'Race board'})).toBeVisible();
    await expect(display.getByText('No overtakes or pit events reported in this race window.')).toHaveCount(0);
    await expect(display.getByRole('table')).not.toContainText('Hard');
    await display.getByRole('button', { name: 'Skip playback' }).click();
    await expect(display.getByRole('list', { name: 'Chronological public event list' })).toContainText('Driver 2 entered the pit lane');
    await expect(display.getByRole('table')).toContainText('Hard');
    await display.getByRole('button', { name: 'Replay feedback' }).click();
    await expect(display.getByText('Presenting race feedback', { exact: true })).toBeVisible();
    // Display playback does not hold the authoritative decision gate closed.
    await expect(page.getByText('Decisions for the next 1 lap',{exact:true})).toBeVisible();
    await expect(display.getByRole('heading', {name:'Public race events'})).toBeVisible();
    await expect(display.getByRole('region', {name:'Race board'})).toBeVisible();
    await expect(display.getByText(/Grid order.*Waiting for first/)).toHaveCount(0);
    for (const name of ['Car 1 — Driver 1', 'Car 2 — Driver 2']) {
        const car = page.getByRole('region', { name });
        await expect(car.getByRole('button', { name: 'Standard', exact: true })).toHaveAttribute('aria-pressed', 'true');
        await expect(car.getByRole('button', { name: 'Stay Out', exact: true })).toHaveAttribute('aria-pressed', 'true');

    }
    await expect(page.getByRole('button',{name:'Approve Plan'})).toBeEnabled();
    mkdirSync('../../.tools/reports/browser-race/review-fixes', {recursive:true});
    await page.screenshot({path:'../../.tools/reports/browser-race/review-fixes/automatic-window-controller-360.png',fullPage:true});
    await expect(page.getByRole('button',{name:'Continue to next phase'})).toHaveCount(0);
    // Fresh public subscription retains the resolved phase's timeline while phase 2 is editable.
    const route='/api/sessions/'+new URL(page.url()).pathname.split('/').at(-1);
    const beforeRefresh=await(await page.request.get(route)).json();
    await display.reload();await expect(display.getByRole('heading',{name:'Public race events'})).toBeVisible();
    await expect(display.getByRole('list',{name:'Chronological public event list'})).toContainText('Driver 2 entered the pit lane');
    const afterRefresh=await(await page.request.get(route)).json();expect(afterRefresh.presentation.cursor).toBe(beforeRefresh.presentation.cursor);expect(afterRefresh.events).toEqual(beforeRefresh.events);
    await display.getByRole('button',{name:'Replay feedback'}).click();
    await page.getByRole('button', { name: 'Approve Plan' }).click(); await page.getByRole('button', { name: 'Confirm plan' }).click();
    await expect(display.getByText('Finish confirmed',{exact:true})).toBeVisible();
    await expect(display.getByRole('region', {name:'Race board'})).toBeVisible();
    await expect(display.getByRole('heading', {name:'Final Classification'})).toHaveCount(0);
    await expect(display.getByRole('table')).not.toContainText('Eligible');
    await display.getByRole('button', { name: 'Skip playback' }).click();
    await expect(display.getByText('Final results ready',{exact:true})).toBeVisible();
    const final = await (await page.request.get('/api/sessions/' + new URL(page.url()).pathname.split('/').at(-1))).json();
    await expect(display.getByRole('table')).toHaveCount(1);
    for (const row of final.classification) {
        const resultRow = display.getByRole('row').filter({ has: display.getByText('Driver '+Number(row.driverId.slice(-2)),{exact:true}) });
        expect(row.completedLaps).toBeGreaterThan(0);
        await expect(resultRow).toContainText(row.eligibleAtFinish ? 'Finished' : 'Disqualified');
    }
    await expect(page.getByRole('button', { name: 'Approve Plan' })).toBeDisabled();
    await expect(page.getByRole('button', { name: 'Continue to next phase' })).toHaveCount(0);
    await expect(display.getByRole('list', { name: 'Chronological public event list' })).not.toContainText('Driver 2 entered the pit lane');
    const last = await display.getByRole('row').last().boundingBox(); expect(last!.y + last!.height).toBeLessThanOrEqual(1080);
    mkdirSync('../../.tools/reports/browser-race', { recursive: true });
    await display.screenshot({ path: '../../.tools/reports/browser-race/finish-1920x1080.png' });
    await page.setViewportSize({ width: 360, height: 800 }); await page.screenshot({ path: '../../.tools/reports/browser-race/finish-controller-360.png', fullPage: true });
    await page.getByRole('button', { name: 'Restart same preset' }).click();
    await expect(page.getByRole('link', { name: 'Open controller' })).toBeVisible();
});
test('reduced motion reconciles a quiet phase immediately without replaying old events', async ({page,context})=>{
    await page.goto('/race/new');await page.getByRole('button',{name:'Create race'}).click();
    const controller=await page.getByRole('link',{name:'Open controller'}).getAttribute('href');
    const url=await page.getByRole('link',{name:'Open public display'}).getAttribute('href');
    const display=await context.newPage();await display.emulateMedia({reducedMotion:'reduce'});await display.goto(url!);
    await display.getByLabel('Display settings',{exact:true}).click();await display.getByLabel('Playback seconds').fill('5');await page.goto(controller!);
    await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
    await expect(display.getByText('Race feedback complete · next choices are open',{exact:true})).toBeVisible();
    await expect(display.getByRole('button',{name:'Skip playback'})).toHaveCount(0);
    await expect(display.getByText('No overtakes or pit events reported in this race window.')).toBeVisible();
    await display.getByRole('button',{name:'Replay feedback'}).click();
    await expect(display.getByRole('button',{name:'Skip playback'})).toHaveCount(0);
    await expect(display.getByText('No overtakes or pit events reported in this race window.')).toBeVisible();
});
test('a delayed accepted-plan acknowledgement cannot relock a newer decision window',async({page})=>{
    await page.goto('/race/new');await page.getByRole('button',{name:'Create race'}).click();
    const url=await page.getByRole('link',{name:'Open controller'}).getAttribute('href');await page.goto(url!);
    let release=()=>{};
    const delayed=new Promise<void>(resolve=>release=resolve);
    await page.route('**/api/sessions/*/plans',async route=>{const response=await route.fetch();await delayed;await route.fulfill({response});});
    await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
    await expect(page.getByText('Decisions for the next 1 lap',{exact:true})).toBeVisible();
    release();await expect(page.getByRole('button',{name:'Approve Plan'})).toBeEnabled();
});
