import {test,expect} from '@playwright/test';
test('public runtime origin sends real REST and SignalR directly across origins and refresh recovers',async({page})=>{
 const api=`http://127.0.0.1:${process.env['F1_API_PORT']??4571}`;
 await page.route('**/runtime-config.js',route=>route.fulfill({contentType:'text/javascript',body:`window.F1_RUNTIME_CONFIG={apiBaseUrl:${JSON.stringify(api)}};`}));
 const requests:string[]=[];const sockets:string[]=[];
 page.on('request',request=>{if(request.url().includes('/api/')||request.url().includes('/hubs/'))requests.push(request.url());});
 page.on('websocket',socket=>sockets.push(socket.url()));
 await page.goto('/race/new');await page.getByRole('button',{name:'Create race',exact:true}).click();
 await page.getByRole('link',{name:'Open public display',exact:true}).click();
 await expect(page.getByText('Connection: connected',{exact:true})).toBeVisible();
 await expect(page.locator('app-timing-tower tbody tr')).toHaveCount(22);
 await page.reload();await expect(page.getByText('Connection: connected',{exact:true})).toBeVisible();
 expect(requests.some(url=>url===api+'/api/sessions')).toBeTruthy();
 expect(requests.every(url=>url.startsWith(api+'/'))).toBeTruthy();
 expect(sockets.some(url=>url.startsWith(api.replace('http:','ws:')+'/hubs/race'))).toBeTruthy();
});
