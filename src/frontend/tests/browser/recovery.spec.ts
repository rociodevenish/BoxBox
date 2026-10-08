import { test, expect } from '@playwright/test';
test('lost HTTP acknowledgement recovers the confirmed lock rather than submitting again', async ({ page }) => {
    await page.routeWebSocket('**/hubs/race*', route => { const server = route.connectToServer(); server.onMessage(message => { if (typeof message === 'string' && message.includes('"target":"teamSnapshot"'))
        return; route.send(message); }); });
    await page.goto('/race/new');
    await page.getByRole('button', { name: 'Create race' }).click();
    const url = await page.getByRole('link', { name: 'Open controller' }).getAttribute('href');
    await page.goto(url!);
    const id = new URL(page.url()).pathname.split('/').at(-1);
    let posts = 0;
    await page.route('**/api/sessions/*/plans', async (route) => { posts++; await route.fetch(); await route.abort(); });
    await page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true}).click();
    await page.getByRole('button', { name: 'Review team plan' }).click();
    await page.getByRole('button', { name: 'Submit both cars' }).click();
    await expect(page.getByRole('button', { name: 'Reconnect' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeDisabled();
    const before = await (await page.request.get('/api/sessions/' + id)).json();
    await page.getByRole('button', { name: 'Reconnect' }).click();
    await expect(page.getByText('Phase complete', { exact: true })).toBeVisible();
    await expect(page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true})).toHaveAttribute('aria-pressed','true');
    await expect(page.getByRole('button', { name: 'Retry pending submission' })).toHaveCount(0);
    expect(posts).toBe(1);
    expect(await (await page.request.get('/api/sessions/' + id)).json()).toEqual(before);
});
test('automatic reconnect rejoins authorized groups and older duplicated wire updates cannot reopen a result', async ({ page }) => {
    let connections = 0;
    let disconnect = () => { };
    let replayOlder = () => { };
    let refreshed = 0;
    await page.routeWebSocket('**/hubs/race*', route => {
        connections++;
        disconnect = () => route.close({ code: 1012, reason: 'Connection interruption' });
        const server = route.connectToServer();
        let older: string | undefined;
        server.onMessage(message => {
            route.send(message);
            if (typeof message !== 'string')
                return;
            if (message.includes('"type":3') && message.includes('"confirmedPlan"') && message.includes('"revision":3'))
                refreshed++;
            if (message.includes('"target":"teamSnapshot"') && message.includes('"revision":2')) {
                older = message;
                replayOlder = () => route.send(message);
            }
            if (message.includes('"target":"teamSnapshot"') && message.includes('"revision":3')) {
                if (older)
                    route.send(older);
                route.send(message);
            }
        });
    });
    await page.goto('/race/new');
    await page.getByRole('button', { name: 'Create race' }).click();
    const url = await page.getByRole('link', { name: 'Open controller' }).getAttribute('href');
    await page.goto(url!);
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeEnabled();
    disconnect();
    await expect.poll(() => connections).toBe(2);
    await expect(page.getByText('Connection: connected', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeEnabled();
    await page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true}).click();
    await page.getByRole('button', { name: 'Review team plan' }).click();
    await page.getByRole('button', { name: 'Submit both cars' }).click();
    await expect(page.getByText('Phase complete', { exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeDisabled();
    await expect.poll(() => refreshed).toBeGreaterThan(0);
    replayOlder();
    // This negative assertion needs a delivery window: the old frame must not cause any state transition.
    await page.waitForTimeout(100);
    await expect(page.getByText('Phase complete', { exact: true })).toBeVisible();
    disconnect();
    await expect.poll(() => connections).toBe(3);
    await expect(page.getByText('Connection: connected', { exact: true })).toBeVisible();
    await expect(page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true})).toHaveAttribute('aria-pressed','true');
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeDisabled();
});
test('a confirmed HTTP acknowledgement retrieves actual observations when realtime team messages are lost', async ({ page }) => {
    await page.routeWebSocket('**/hubs/race*', route => { const server = route.connectToServer(); server.onMessage(message => { if (typeof message === 'string' && message.includes('"target":"teamSnapshot"'))
        return; route.send(message); }); });
    await page.goto('/race/new');
    await page.getByRole('button', { name: 'Create race' }).click();
    const url = await page.getByRole('link', { name: 'Open controller' }).getAttribute('href');
    await page.goto(url!);
    await page.getByRole('region',{name:'Car 2 — Driver 02'}).getByRole('button',{name:'Pit',exact:true}).click();
    await page.getByRole('button', { name: 'Review team plan' }).click();
    await page.getByRole('button', { name: 'Submit both cars' }).click();
    await expect(page.getByText('Phase complete', { exact: true })).toBeVisible();
    await expect(page.getByRole('region',{name:'Car 2 — Driver 02'}).getByLabel('Tyre compound Hard',{exact:true})).toBeVisible();
});
test('unavailable connections disable actions and a retry preserves the same in-flight command identity', async ({ page }) => {
    await page.goto('/race/new');
    await page.getByRole('button', { name: 'Create race' }).click();
    const url = await page.getByRole('link', { name: 'Open controller' }).getAttribute('href');
    let disconnected = false;
    await page.route('**/hubs/race/negotiate*', async (route) => { if (!disconnected) {
        disconnected = true;
        await route.abort();
    }
    else
        await route.continue(); });
    await page.goto(url!);
    await expect(page.getByRole('button', { name: 'Reconnect' })).toBeVisible();
    await page.getByRole('button', { name: 'Reconnect' }).click();
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeEnabled();
    await page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true}).click();
    const ids: string[] = [];
    await page.route('**/api/sessions/*/plans', async (route) => { ids.push(route.request().postDataJSON().commandId); if (ids.length === 1)
        await route.abort();
    else
        await route.continue(); });
    await page.getByRole('button', { name: 'Review team plan' }).click();
    await page.getByRole('button', { name: 'Submit both cars' }).click();
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeDisabled();
    await expect(page.getByRole('button', { name: 'Reconnect' })).toBeVisible();
    await expect(page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true})).toHaveAttribute('aria-pressed','true');
    await page.getByRole('button', { name: 'Reconnect' }).click();
    await expect(page.getByRole('button', { name: 'Retry pending submission' })).toBeEnabled();
    await page.getByRole('button', { name: 'Retry pending submission' }).click();
    await expect(page.getByText('Phase complete', { exact: true })).toBeVisible();
    expect(ids).toHaveLength(2);
    expect(ids[1]).toBe(ids[0]);
    await page.reload();
    await expect(page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true})).toHaveAttribute('aria-pressed','true');
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeDisabled();
});
test('refresh recovers own confirmed choices, rejects edits atomically and expires replaced sessions', async ({ page }) => {
    await page.goto('/race/new');
    await page.getByRole('button', { name: 'Create race' }).click();
    const url = await page.getByRole('link', { name: 'Open controller' }).getAttribute('href');
    await page.goto(url!);
    await page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true}).click();
    await page.reload();
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeEnabled();
    await page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true}).click();
    await page.getByRole('region',{name:'Car 2 — Driver 02'}).getByRole('button',{name:'Pit',exact:true}).click();
    await page.getByRole('region',{name:'Car 2 — Driver 02'}).getByLabel('New compound').selectOption('Hard');
    let rejected = false;
    await page.route('**/api/sessions/*/plans', async (route) => { if (!rejected) {
        rejected = true;
        const body = route.request().postDataJSON();
        body.cars[1].pace = 'Boost';
        await route.continue({ postData: JSON.stringify(body) });
    }
    else
        await route.continue(); });
    await page.getByRole('button', { name: 'Review team plan' }).click();
    await page.getByRole('button', { name: 'Submit both cars' }).click();
    await expect(page.getByRole('alert')).toContainText('valid');
    await expect(page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true})).toHaveAttribute('aria-pressed','true');
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeEnabled();
    await page.getByRole('button', { name: 'Review team plan' }).click();
    await page.getByRole('button', { name: 'Submit both cars' }).click();
    await expect(page.getByText('Phase complete', { exact: true })).toBeVisible();
    await page.reload();
    await expect(page.getByRole('button', { name: 'Review team plan' })).toBeDisabled();
    await expect(page.getByRole('region',{name:'Car 1 — Driver 01'}).getByRole('button',{name:'Push',exact:true})).toHaveAttribute('aria-pressed','true');
    await expect(page.getByRole('region',{name:'Car 2 — Driver 02'}).getByRole('button',{name:'Pit',exact:true})).toHaveAttribute('aria-pressed','true');
    await page.request.post('/api/sessions', { data: { preset: 'open-equal', laps: 6, seed: 123 } });
    await page.reload();
    await expect(page.getByRole('alert')).toContainText('unavailable');
});
