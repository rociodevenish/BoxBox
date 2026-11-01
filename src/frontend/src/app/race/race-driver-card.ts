import {DriverCardViewModel} from '../ui/driver-card.model';
import {CardImage,FrozenDriver,frozenDriverCard,imageUrl} from '../ui/content-card.model';
/** Owned data comes from the authorised frozen race snapshot; nearby identities expose no private attributes. */
export function raceDriverCard(id:string,identity?:{name:string;slug:string;teamId:string;image?:CardImage|null},owned?:FrozenDriver|null):DriverCardViewModel {
 if(owned)return {...frozenDriverCard(owned,identity?{name:identity.teamId,accent:'#ff5954'}:null),id};
 const name=identity?.name??'Driver '+id,names=name.trim().split(/\s+/),url=imageUrl(identity?.image);
 return {id,firstName:names.slice(0,-1).join(' '),surname:names.at(-1)??name,portrait:url?{url,position:identity?.image?.objectPosition}:null,
  ownership:identity?{name:identity.teamId,accent:'#ff5954'}:null,attributes:{Pace:null,Racecraft:null,Tyres:null,Consistency:null,Technical:null},details:[],preferences:[]};
}