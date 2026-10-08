import {pathToFileURL} from 'node:url';
import {mkdir,writeFile} from 'node:fs/promises';
import {startBaseline} from './serve-design-baseline.mjs';
const {chromium}=await import(process.env.PLAYWRIGHT_MODULE?pathToFileURL(process.env.PLAYWRIGHT_MODULE).href:'playwright');
const server=await startBaseline();
const browser=await chromium.launch();
const out='.scratch/visual-design-foundation/evidence';await mkdir(out,{recursive:true});
const checks=[];
try{for(const [width,height] of [[360,800],[768,1024],[1920,1080]]){
 const page=await browser.newPage({viewport:{width,height},reducedMotion:'reduce'});
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto('http://127.0.0.1:4300/design-baseline/index.html');await page.locator('.car').last().waitFor();await page.evaluate(()=>Promise.all([...document.images].map(i=>i.decode())));
 await page.screenshot({path:`${out}/baseline-${width}.png`,fullPage:true});
 for(const [label,selector] of [['cards','.cards'],['tower','.race-grid > .panel:first-child'],['controller','.race-grid > .panel:last-child']])await page.locator(selector).screenshot({path:`${out}/${label}-${width}.png`});
 const layout=await page.evaluate(()=>({viewport:innerWidth,scrollWidth:document.documentElement.scrollWidth,minTarget:Math.min(...[...document.querySelectorAll('button')].map(b=>Math.min(b.getBoundingClientRect().width,b.getBoundingClientRect().height))),portraits:[...document.images].map(i=>({loaded:i.naturalWidth>0,width:i.naturalWidth,height:i.naturalHeight}))}));
 await page.keyboard.press('Tab');const focus=await page.locator(':focus').evaluate(e=>({label:e.textContent,outline:getComputedStyle(e).outlineStyle}));
 checks.push({width,height,layout,focus,errors});await page.close();
}await writeFile(`${out}/render-checks.json`,JSON.stringify(checks,null,2));console.log(JSON.stringify(checks,null,2));}finally{await browser.close();server.close();}
