import source from '../../../../../.docs/data/f1_strategy_driver_cards_2026.json';
import portraits from '../../../public/portraits/manifest.json';
import {DriverCardViewModel,DisplayRating} from '../ui/driver-card.model';
function headline(value:number):DisplayRating {switch(value){case 1:case 2:case 3:case 4:case 5:return value;default:return null;}}
const sourceCardIds = [
  "driver-max-verstappen",
  "driver-george-russell",
  "driver-andrea-kimi-antonelli",
  "driver-lewis-hamilton",
  "driver-charles-leclerc",
  "driver-lando-norris",
  "driver-oscar-piastri",
  "driver-fernando-alonso",
  "driver-carlos-sainz",
  "driver-nico-hulkenberg",
  "driver-pierre-gasly",
  "driver-isack-hadjar",
  "driver-esteban-ocon",
  "driver-alexander-albon",
  "driver-sergio-perez",
  "driver-valtteri-bottas",
  "driver-oliver-bearman",
  "driver-gabriel-bortoleto",
  "driver-liam-lawson",
  "driver-arvid-lindblad",
  "driver-franco-colapinto",
  "driver-lance-stroll"
];
/** Fixture IDs map to source race cards in published sort order; display only, never physics. */
export function raceDriverCard(id:string,identity?:{name:string;slug:string;teamId:string}):DriverCardViewModel {
 const index=Number(id.slice(-2))-1;
 const card=source.drivers.find(d=>identity?d.slug===identity.slug:d.id===sourceCardIds[index]);
 const asset=portraits.portraits.find(p=>p.driverId===card?.slug);
 return {id,firstName:identity?identity.name.split(' ').slice(0,-1).join(' '):'Driver',surname:identity?identity.name.split(' ').slice(-1)[0]:String(index+1),portrait:asset?{url:asset.runtimeUrl,position:asset.objectPosition}:null,ownership:{name:identity?.teamId??'Team '+String(Math.floor(index/2)+1).padStart(2,'0'),accent:'#ff685b'},attributes:{Pace:headline(card?.ratings.headline.pace??0),Racecraft:headline(card?.ratings.headline.racecraft??0),Tyres:headline(card?.ratings.headline.tyres??0),Consistency:headline(card?.ratings.headline.consistency??0),Technical:headline(card?.ratings.headline.technical??0)},details:card?Object.entries(card.ratings.detailed).map(([key,value])=>({label:key.replace(/([A-Z])/g,' $1').replace(/^./,s=>s.toUpperCase()),rating:null,displayValue:value,scale:100})):[],preferences:card?.preferences.map(p=>p.label)??[],priceLabel:card?card.economy.cost+' credits':undefined};
}
