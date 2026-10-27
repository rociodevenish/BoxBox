import {pathToFileURL} from 'node:url';
import {writeFile,readFile} from 'node:fs/promises';
import {startBaseline} from './serve-design-baseline.mjs';
const {chromium}=await import(pathToFileURL(process.env.PLAYWRIGHT_MODULE).href);const server=await startBaseline();const browser=await chromium.launch();
try{const page=await browser.newPage({viewport:{width:1920,height:1080}});await page.goto('http://127.0.0.1:4300/design-baseline/index.html?view=display');await page.locator('.display-card').waitFor();await page.evaluate(()=>Promise.all([...document.images].map(i=>i.decode())));await page.screenshot({path:'.scratch/visual-design-foundation/evidence/shared-display-1920x1080.png'});console.log(await page.locator('.tower tbody tr:last-child').boundingBox());
const fs=await import('node:fs/promises');const dir='.scratch/visual-design-foundation/evidence';const sizes=[];for(const file of (await fs.readdir(dir)).filter(f=>f.endsWith('.png'))){const png=await readFile(`${dir}/${file}`);sizes.push({file,width:png.readUInt32BE(16),height:png.readUInt32BE(20)});}await writeFile(`${dir}/capture-dimensions.json`,JSON.stringify(sizes,null,2));}finally{await browser.close();server.close();}

