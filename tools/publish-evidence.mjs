import {cpSync,existsSync,mkdirSync} from 'node:fs';
import {resolve,relative,isAbsolute} from 'node:path';
const [sourceArg,destinationArg,...files]=process.argv.slice(2);
if(!sourceArg||!destinationArg||!files.length)throw new Error('Usage: node tools/publish-evidence.mjs source-directory destination-directory file...');
const root=process.cwd(),source=resolve(sourceArg),destination=resolve(destinationArg);
const inside=(base,target)=>{const path=relative(base,target);return path!==''&&!path.startsWith('..')&&!isAbsolute(path);};
if(!inside(resolve(root,'.tools/reports'),source)&&source!==resolve(root,'.tools/reports'))throw new Error('Source must be inside ignored .tools/reports');
if(!inside(resolve(root,'.scratch'),destination))throw new Error('Destination must be inside .scratch');
for(const file of files){const from=resolve(source,file),to=resolve(destination,file);if(!inside(source,from)||!inside(destination,to)||!existsSync(from))throw new Error('Invalid evidence file '+file);}
mkdirSync(destination,{recursive:true});for(const file of files)cpSync(resolve(source,file),resolve(destination,file),{recursive:false});console.log('Published '+files.length+' explicitly selected evidence files');
