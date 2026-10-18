import {Component,input,output} from '@angular/core';
import type {TeamAssistance} from './assistance.model';
@Component({selector:'app-assistance-summary',template:`
 @if(assistance();as help){<section class="assistance" aria-label="Player assistance">
 <h2>{{help.helpLevel}} help</h2>
 <p>{{help.autoAcceptRecommendation?'On expiry: accept this phase’s saved recommendation if you have not locked a plan.':'On expiry: temporary AI supplies only a missing plan for this phase.'}}</p>
 @if(help.recommendation;as advice){
 <p class="freshness">Saved for Phase {{advice.phase+1}} · race observation {{advice.observationTime.toFixed(0)}} seconds. Refreshing does not change this advice.</p>
 <h3>Suggested two-car plan</h3>
 @for(car of advice.safetyCarCars??[];track car.driverId){<div class="suggestion"><h4>Driver {{number(car.driverId)}}</h4><p class="plan">{{car.suggested.pit}}{{car.suggested.compound?' · '+car.suggested.compound:''}}</p><ul>@for(reason of car.reasons;track reason){<li>{{reason}}</li>}</ul><p class="uncertainty">{{car.uncertainty}}</p></div>}
 @for(car of advice.cars;track car.driverId){<div class="suggestion"><h4>Driver {{number(car.driverId)}}</h4><p class="plan">{{car.suggested.pace}} · {{car.suggested.pit}}{{car.suggested.compound?' · '+car.suggested.compound:''}} · {{car.suggested.battle}}</p><ul>@for(reason of car.reasons;track reason){<li>{{reason}}</li>}</ul><p class="uncertainty">{{car.uncertainty}}</p>@if(car.battleOptions.length===1){<p>Neutral is the only legal battle choice.</p>}@if(car.pitOptions.length===1){<p>Stay Out is the only new pit choice while the accepted stop completes.</p>}</div>}
 @if(help.helpLevel==='Guided'){<p>{{advice.choiceExplanation}}</p>}
 @if(showAction()){<button type="button" [disabled]="!editable()" (click)="usePlan.emit()">Use suggested two-car plan</button>}
 }@else{<p>All legal choices are available. No strategic recommendations are shown.</p>}
 </section>}
 `,styles:[`.assistance{padding:16px;margin:16px 0;border:1px solid var(--color-border);border-radius:var(--radius-panel);background:var(--color-surface)}h2,h3,h4{margin:0 0 8px}.suggestion{padding:12px 0;border-top:1px solid var(--color-border)}p,li{line-height:1.5;overflow-wrap:anywhere}.plan{font-weight:700}.freshness,.uncertainty{color:var(--color-text-muted)}button{width:100%;min-height:44px}`]})
export class AssistanceSummary {
 readonly showAction=input(true);readonly assistance=input<TeamAssistance>();readonly editable=input(false);readonly usePlan=output<void>();
 number(id:string){return Number(id.slice(-2));}
}

