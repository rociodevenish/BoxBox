import {readFileSync,writeFileSync,existsSync} from 'node:fs';
import {gunzipSync} from 'node:zlib';
const [input,output]=process.argv.slice(2);
if(!input||!output||existsSync(output))throw Error('Pass frozen archive and NEW summary filename.');
const archive=JSON.parse(gunzipSync(readFileSync(input)));
const target=o=>o.classification.filter(c=>c.driverId.startsWith('team-01-'));
const mean=a=>a.reduce((s,v)=>s+v,0)/a.length;
const stats=a=>({n:a.length,mean:a.length?mean(a):null,min:a.length?Math.min(...a):null,max:a.length?Math.max(...a):null,standardDeviation:a.length?Math.sqrt(mean(a.map(x=>(x-mean(a))**2))):null});
const pairs=[];
for(const circuit of ['track-monza','track-monaco'])for(const diagnostic of [false,true]){
 const cases=archive.cases.filter(c=>c.circuit===circuit&&c.diagnostic===diagnostic);
 for(let i=0;i<cases.length;i++)for(let j=i+1;j<cases.length;j++){
  const a=cases[i],b=cases[j],deltas=a.outcomes.map((o,k)=>{
   const at=target(o),bt=target(b.outcomes[k]);if(at.length!==2||bt.length!==2||o.seed!==b.outcomes[k].seed)throw Error('Incomplete paired roster/seeds');
   return {seed:o.seed,seconds:[...at,...bt].every(c=>c.finishTime!=null)?mean(bt.map(c=>c.finishTime))-mean(at.map(c=>c.finishTime)):null,position:mean(bt.map(c=>c.position))-mean(at.map(c=>c.position)),retirements:bt.filter(c=>c.retirementTime!=null).length-at.filter(c=>c.retirementTime!=null).length};
  });
  const finite=deltas.filter(p=>p.seconds!=null).map(p=>p.seconds);
  pairs.push({circuit,diagnostic,reference:a.build,alternative:b.build,creditCostDifference:b.cost-a.cost,referenceCost:a.cost,alternativeCost:b.cost,samples:deltas.length,finishSeconds:stats(finite),faster:finite.filter(v=>v< -1e-7).length,tied:finite.filter(v=>Math.abs(v)<=1e-7).length,slower:finite.filter(v=>v>1e-7).length,position:stats(deltas.map(p=>p.position)),retirementDifference:deltas.reduce((s,p)=>s+p.retirements,0),deltas});
 }
}
writeFileSync(output,JSON.stringify({schema:'construction-pairwise-v1',interpretation:'Alternative minus reference. Finishing seconds conditional on all four target entries finishing. Separate normal and opt-in diagnostic strata. Descriptive paired samples, not calibrated balance or human comprehension.',pairs},null,2)+'\n');
console.log(`Wrote ${pairs.length} pairwise comparisons from frozen public outcomes.`);
