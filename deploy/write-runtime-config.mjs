import {writeFile} from 'node:fs/promises';
const [origin,output='src/frontend/dist/gallery/browser/runtime-config.js']=process.argv.slice(2);
const url=new URL(origin);
if(url.protocol!=='https:'||url.origin!==origin||url.username||url.password)throw new Error('Supply the exact direct Cloud Run HTTPS origin');
await writeFile(output,`window.F1_RUNTIME_CONFIG = ${JSON.stringify({apiBaseUrl:origin})};\n`);
