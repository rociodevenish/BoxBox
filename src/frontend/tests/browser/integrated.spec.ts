import { test, expect } from '@playwright/test';
test('public live tower shows tyre age and selectable timing references, with all 22 rows inside the shared display', async ({ page }) => {
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto('/race/new');
    await page.getByRole('button', { name: 'Create race' }).click();
    const url = await page.getByRole('link', { name: 'Open public display' }).getAttribute('href');
    await page.goto(url!);
    await expect(page.getByRole('row')).toHaveCount(23);
    await expect(page.getByRole('table').getByText('0 laps', { exact: true })).toHaveCount(22);
    await page.getByRole('button', { name: 'Interval — to car ahead' }).click();
    await expect(page.getByRole('table', { name: 'Classification — Interval to car ahead' })).toBeVisible();
    await expect(page.getByRole('row').nth(2)).toContainText('+0.500 s');
    await expect(page.getByText(/estimate|seed|condition|opponent.*choice/i)).toHaveCount(0);
    const last = await page.getByRole('row').last().boundingBox();
    expect(last!.y + last!.height).toBeLessThanOrEqual(1080);
});
test('live controller offers only each cars legal buttons and visible new-phase defaults', async ({ page }) => {
    await page.goto('/race/new');
    await page.getByRole('button', { name: 'Create race' }).click();
    const url = await page.getByRole('link', { name: 'Open controller' }).getAttribute('href');
    await page.goto(url!);
    await expect(page.getByText('Decisions for the next 5 laps',{exact:true})).toBeVisible();
    const id = new URL(page.url()).pathname.split('/').at(-1);
    const capability = new URL(page.url()).hash.slice('#capability='.length);
    const own = await (await page.request.get('/api/sessions/' + id + '/team', { headers: { Authorization: 'Bearer ' + capability } })).json();
    for (let i = 0; i < 2; i++) {
        const car = page.getByRole('region', { name: 'Car ' + (i + 1) + ' — Driver ' + (i + 1) });
        await expect(car.getByRole('button', { name: 'Standard', exact: true })).toHaveAttribute('aria-pressed', 'true');
        await expect(car.getByRole('button', { name: 'Stay Out', exact: true })).toHaveAttribute('aria-pressed', 'true');
        await expect(car.getByRole('button',{name:'Neutral',exact:true})).toHaveAttribute('aria-pressed','true');
        for (const option of ['Attack', 'Defend'])
            await expect(car.getByRole('button', { name: option, exact: true })).toHaveCount(own.cars[i].battleOptions.includes(option) ? 1 : 0);
        await expect(car).toContainText('Tyre condition estimate');
        await expect(car).toContainText('next lap end');
    }
});
