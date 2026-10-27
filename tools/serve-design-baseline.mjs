import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import {resolve,extname} from 'node:path';
export function startBaseline(port=4300){
 const root=fileURLToPath(new URL('../src/frontend/',import.meta.url));
 const server=createServer(async(req,res)=>{try{
  let relative=decodeURIComponent(new URL(req.url,'http://localhost').pathname).replace(/^\//,'')||'design-baseline/index.html';
  if(relative.endsWith('/'))relative+='index.html';
  const path=resolve(root,relative);if(!path.startsWith(root))throw Error('Outside frontend');
  const body=await readFile(path);res.setHeader('Content-Type',({'.html':'text/html','.css':'text/css','.js':'text/javascript','.png':'image/png','.json':'application/json'})[extname(path)]||'application/octet-stream');res.end(body);
 }catch{res.writeHead(404);res.end('Not found');}});
 return new Promise(resolve=>server.listen(port,'127.0.0.1',()=>resolve(server)));
}
if(process.argv[1]===fileURLToPath(import.meta.url)){await startBaseline();console.log('Design baseline: http://127.0.0.1:4300/design-baseline/');}

