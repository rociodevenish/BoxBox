import { test, expect } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
for(const {preset,repeatedStop} of [...['open-equal','restricted-equal','open-mixed','restricted-mixed'].map(preset=>({preset,repeatedStop:false})),{preset:'open-equal',repeatedStop:true}]) {
  const name=preset+(repeatedStop?'-lapped':'');
  test('50-lap authoritative browser race and exact designer replay: '+name,async({page,context})=>{
    test.setTimeout(180000);
    await page.goto('/race/new');await page.getByLabel('Preset').selectOption(preset);await page.getByLabel('Race distance').selectOption('50');
    await page.getByRole('button',{name:'Create race'}).click();
    const controller=await page.getByRole('link',{name:'Open controller'}).getAttribute('href');
    const publicUrl=await page.getByRole('link',{name:'Open public display'}).getAttribute('href');
    const id=publicUrl!.split('/').at(-1);const capability=new URL(controller!,page.url()).hash.slice('#capability='.length);
    const headers={Authorization:'Bearer '+capability};const designer={'X-Designer-Key':'ephemeral-browser-test-key'};
    const display=await context.newPage();await display.emulateMedia({reducedMotion:'reduce'});await display.goto(publicUrl!);
    await page.emulateMedia({reducedMotion:'reduce'});await page.goto(controller!);
    const observations:unknown[]=[];
    for(let phase=0;phase<10;phase++) {
      await expect(page.getByText('Decisions for the next 5 laps',{exact:true})).toBeVisible();
      // Five-lap headings repeat: wait for the rendered lap boundary and its matching team window.
      await expect(page.getByText('Lap '+(phase*5)+' / 50',{exact:true})).toBeVisible();
      await expect.poll(async()=>(await(await page.request.get('/api/sessions/'+id+'/team',{headers})).json()).phase).toBe(phase);
      const own=await(await page.request.get('/api/sessions/'+id+'/team',{headers})).json();
      observations.push({phase,cars:own.cars});
      for(let car=0;car<2;car++) {
        const region=page.getByRole('region',{name:'Car '+(car+1)+' — Driver '+(car+1)});
        await region.getByRole('button',{name:(car===0)!==repeatedStop?'Conserve':'Push',exact:true}).click();
        if((repeatedStop?car===1:phase===2||phase===6)&&!own.cars[car].inPits&&!own.cars[car].stopQueued){
          await region.getByRole('button',{name:'Pit',exact:true}).click();
          await region.getByRole('button',{name:'New compound '+(repeatedStop||phase===2?'Hard':'Soft'),exact:true}).click();
        }
        const attack=region.getByRole('button',{name:'Attack',exact:true});
        await expect(attack).toHaveCount(own.cars[car].battleOptions.includes('Attack')?1:0);
        if(own.cars[car].battleOptions.includes('Attack'))await attack.click();
      }
      await page.getByRole('button',{name:'Approve Plan'}).click();await page.getByRole('button',{name:'Confirm plan'}).click();
      if(phase===9) await expect(page.getByRole('heading',{name:'Your team final results',exact:true})).toBeVisible(); else await expect(page.getByText('Decisions for the next 5 laps',{exact:true})).toBeVisible();
    }
    await expect(display.getByText('Finish confirmed',{exact:true})).toBeVisible();
    await expect(page.getByRole('button',{name:'Approve Plan'})).toBeDisabled();
    const readRecord=async()=> (await page.request.get('/api/designer/sessions/'+id+'/record',{headers:designer})).json();
    await expect.poll(async()=> (await readRecord()).timing.presentationSamples.length).toBe(10);
    const record=await readRecord();
    const verification=await(await page.request.post('/api/designer/replay/verify',{headers:designer,data:record})).json();
    expect(verification.matches).toBe(true);expect(verification.finished).toBe(true);expect(verification.phases).toHaveLength(10);
    const final=await(await page.request.get('/api/sessions/'+id)).json();
    expect(final.leaderLap).toBe(50);expect(final.classification).toHaveLength(22);
    if(repeatedStop){
      const lapped=final.classification.find((car:{driverId:string})=>car.driverId==='driver-02');
      expect(lapped.completedLaps).toBeLessThan(50);expect(lapped.gapToLeader.kind).toBe('laps');expect(lapped.eligibleAtFinish).toBe(true);
      await expect(display.getByRole('row').filter({has:display.getByText('Driver 2',{exact:true})})).toContainText('Finished');
      await display.setViewportSize({width:1920,height:1080});
      const last=await display.getByRole('row').last().boundingBox();expect(last!.y+last!.height).toBeLessThanOrEqual(1080);
      const restart=await display.getByRole('link',{name:'Choose another preset',exact:true}).boundingBox();expect(restart!.y+restart!.height).toBeLessThanOrEqual(1080);
      await display.screenshot({path:'../../.tools/reports/browser-race/review-05/lapped-finish-1920x1080.png'});
    }
    mkdirSync('../../.tools/reports/browser-race/full-distance',{recursive:true});
    writeFileSync('../../.tools/reports/browser-race/full-distance/'+name+'-replay.json',JSON.stringify(record,null,2));
    writeFileSync('../../.tools/reports/browser-race/full-distance/'+name+'-summary.json',JSON.stringify({preset,seed:123,distance:50,verification,final,observations,timing:record.timing},null,2));
  });
}
