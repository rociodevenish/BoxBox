// Executable paths are passed directly to spawn, never encoded as shell strings.
import {spawn} from 'node:child_process';
import {fileURLToPath} from 'node:url';
const repositoryRoot=fileURLToPath(new URL('../',import.meta.url));
const artifacts=process.env.F1_DOTNET_ARTIFACTS_PATH;
const fixture=process.argv[2]==='fixture';
const port=Number(process.env.F1_API_PORT??4571);
if(!Number.isInteger(port)||port<1024||port>65535)throw new Error('Invalid F1_API_PORT');
const child=spawn(process.env.DOTNET_EXE??'dotnet',['run','--project',fixture?'../../tests/Game.BrowserFixtureHost':'../backend/Game.Api',...(artifacts?['--artifacts-path',artifacts]:[]),'--urls',`http://127.0.0.1:${port}`],{stdio:'inherit',windowsHide:true,env:{...process.env,F1_REPO_ROOT:repositoryRoot}});
child.once('error',error=>{console.error(error);process.exit(1);});child.once('exit',code=>process.exit(code??1));
for(const signal of ['SIGTERM','SIGINT'])process.on(signal,()=>child.kill(signal));
