import type {DriverCardViewModel,DisplayRating} from './driver-card.model';
import portraits from '../../../public/portraits/manifest.json';
export interface CardImage {assetId:string;entityId:string;url:string;objectPosition:string;width:number;height:number}
export interface PersonnelPresentation {personId:string;identityKind:string;referenceRole:string;price:number;simpleGroups:Record<string,number>;implementedAttributes:string[];inactiveAttributes:string[];ratingScale:string;preferenceMeaning:string;effectScope:string;image?:CardImage|null}
export interface FrozenDriver {id:string;name:string;slug:string;ratings:Record<string,number>;preferences:{key:string;label:string}[];presentation?:PersonnelPresentation|null}
export interface CardFact {label:string;value:string;state?:string;wide?:boolean}
export interface ContentCardViewModel {id:string;name:string;role:string;context?:string;priceLabel?:string;image?:CardImage|null;facts:CardFact[];details?:CardFact[];notes?:string[];preferences?:string[];landscape?:boolean}
export function readableScope(scope?:string){return scope?.startsWith('Implemented capabilities are phase/condition dependent:')?'Starts: standing starts. Wet skill: wet running. Feedback: practice information. Adaptability: preference fit.':scope;}
export const attributeLabel=(key:string)=>({racePace:'Race Pace',oneLapPace:'One-Lap Pace',wetWeatherSkill:'Wet Weather Skill',tyreWear:'Tyre Preservation',tyreTemperature:'Tyre Temperature Management',highSpeedAero:'High-Speed Aero',lowSpeedAero:'Low-Speed Aero',topSpeed:'Top Speed',accelerationDeployment:'Acceleration / Energy Deployment',mechanicalGrip:'Mechanical Grip',lowSpeedCornering:'Low-Speed Cornering',highSpeedCornering:'High-Speed Cornering',tyreDegradation:'Tyre Degradation Demand',tyreThermalDemand:'Tyre Thermal Demand'}[key]??key.replace(/([a-z])([A-Z])/g,'$1 $2').replace(/^./,s=>s.toUpperCase()));
export function imageUrl(image?:CardImage|null):string|null{return image&&/^\/(?:content\/[a-f0-9]{64}\.webp|(?:portraits|tracks)\/[a-z0-9][a-z0-9-]*\.(?:webp|png))$/.test(image.url)?image.url:null;}
export function frozenDriverCard(driver:FrozenDriver,ownership:DriverCardViewModel['ownership']=null):DriverCardViewModel {
 const names=driver.name.trim().split(/\s+/),display=driver.presentation;
 const group=(name:string):DisplayRating=>{const value=display?.simpleGroups[name];return value===1||value===2||value===3||value===4||value===5?value:null;};
 const legacy=!display?portraits.portraits.find(p=>p.driverId===driver.slug):undefined;
 const url=display?imageUrl(display.image):legacy?'/'+legacy.runtimeUrl.replace(/^\//,''):null;
 return {id:driver.id,firstName:names.slice(0,-1).join(' '),surname:names.at(-1)??driver.name,
  portrait:url?{url,position:display?.image?.objectPosition}:null,ownership,
  attributes:{Pace:group('Pace'),Racecraft:group('Racecraft'),Tyres:group('Tyres'),Consistency:group('Consistency'),Technical:group('Technical')},
  details:Object.entries(driver.ratings).map(([key,value])=>({label:attributeLabel(key),rating:null,displayValue:value,scale:100,state:display?.inactiveAttributes.includes(key)?'Inactive':display?.implementedAttributes.includes(key)?'Implemented · phase or condition dependent':undefined})),
  preferences:driver.preferences.map(p=>p.label),priceLabel:display?display.price+' credits':undefined,
  referenceRole:display?.referenceRole,effectScope:readableScope(display?.effectScope)};
}
export function personnelCard(card:{id:string;name:string;role:string;price:number;effectLabel:string;attributes?:Record<string,number>|null;driver?:FrozenDriver|null;presentation?:PersonnelPresentation|null}):ContentCardViewModel {
 const display=card.presentation;
 const facts=Object.entries(card.driver?.ratings??card.attributes??{}).map(([key,value])=>({label:attributeLabel(key),value:value+' / 100',state:display?.inactiveAttributes.includes(key)?'Inactive':display?.implementedAttributes.includes(key)?'Implemented · phase or role dependent':'Scope not supplied'}));
 const notes=[card.effectLabel];
 if(card.driver)notes.push('Preferences describe compatibility, separately from quality.',display?readableScope(display.effectScope)!:'Detailed 1–100 game inputs. Starts applies to the automatic standing start; wet skill depends on wet conditions. No headline stars are converted into pace.');
 else if(display)notes.push(display.effectScope,display.preferenceMeaning);
 return {id:card.id,name:card.name,role:card.role,context:display?.identityKind==='fictional-staff'?'Fictional staff':display?.referenceRole,priceLabel:card.price+' credits',image:display?.image,
  facts:card.driver?['Pace','Racecraft','Tyres','Consistency','Technical'].map(label=>({label,value:display?.simpleGroups[label]===undefined?'Unknown':display.simpleGroups[label]+' / 5'})):facts.filter(f=>f.state!=='Inactive'),details:facts,preferences:card.driver?.preferences.map(p=>p.label),notes};
}
