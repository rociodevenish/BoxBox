import type {Battle,Compound,Pace} from '../ui/controller/controller.model';
export interface PlayerHelp {helpLevel:'Standard'|'Assisted'|'Guided';autoAcceptRecommendation:boolean}
export interface TeamAssistance extends PlayerHelp {
 phase:number;
 recommendation?:{phase:number;windowRevision:number;observationTime:number;choiceExplanation:string;cars:readonly {
  driverId:string;suggested:{driverId:string;pace:Pace;pit:string;compound:Compound|null;battle:Battle};reasons:readonly string[];uncertainty:string;
  paceOptions:readonly Pace[];pitOptions:readonly (Compound|null)[];battleOptions:readonly Battle[];confident:boolean;
 }[]};
}
