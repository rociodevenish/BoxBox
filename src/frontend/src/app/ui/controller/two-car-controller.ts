import {AssistanceSummary} from '../../assistance/assistance-summary';
import {DriverCard} from '../driver-card';
import {DriverCardViewModel} from '../driver-card.model';
import {TimingTower} from '../timing/timing-tower';
import {
  Component,
  ElementRef,
  effect,
  input,
  output,
  signal,
  viewChild,
} from "@angular/core";
import { CompoundBadge } from "../compound-badge";
import {
  CarInstruction,
  Compound,
  Pace,
  Battle,
  PrivateControllerViewModel,
  TeamPlan,
} from "./controller.model";
@Component({
  selector: "app-two-car-controller",
  imports: [CompoundBadge,DriverCard,TimingTower,AssistanceSummary],
  templateUrl: "./two-car-controller.html",
  styleUrl: "./two-car-controller.css",
})
export class TwoCarController {
  readonly analyzedDriver=signal<DriverCardViewModel|null>(null);
  private readonly analysis = viewChild.required<ElementRef<HTMLDialogElement>>('analysis');
  keepAnalysisFocus(event:KeyboardEvent){
    if(event.key!=='Tab' || event.defaultPrevented)return;
    const controls=Array.from(this.analysis().nativeElement.querySelectorAll<HTMLButtonElement>('button')).filter(control=>!control.disabled && control.getClientRects().length>0);
    if(!controls.length)return;
    event.preventDefault();
    const index=controls.findIndex(control=>control===document.activeElement);
    controls[(index+(event.shiftKey?-1:1)+controls.length)%controls.length].focus();
  }
  analyzeDriver(id:string){const driver=this.model().cars.flatMap(car=>car.nearbyDrivers??[]).find(card=>card.id===id);if(driver){this.analyzedDriver.set(driver);this.analysis().nativeElement.showModal();}}
  lapTime(seconds:number){const ms=Math.round(seconds*1000);return Math.floor(ms/60000)+':'+((ms%60000)/1000).toFixed(3).padStart(6,'0');}
  readonly model = input.required<PrivateControllerViewModel>();
  assistanceNames(){return Object.fromEntries(this.model().cars.map(car=>[car.id,car.name]));}
  readonly submitPlan = output<TeamPlan>();
  readonly choices = signal<readonly CarInstruction[]>([]);
  readonly confirmation =
    viewChild.required<ElementRef<HTMLDialogElement>>("confirmation");
  get compounds(): readonly Compound[] { return this.model().wetTyresEnabled ? ['Soft','Medium','Hard','Intermediate','Wet'] : ['Soft','Medium','Hard']; }
  readonly paces: readonly Pace[] = ["Conserve", "Standard", "Push"];
  private draftPhase: number | undefined;
  private confirmedRevision: number | undefined;
  constructor() {
    effect(() => {
      const model = this.model();
      if (this.draftPhase !== model.phase || model.confirmedRevision !== undefined && this.confirmedRevision !== model.confirmedRevision) {
        this.draftPhase = model.phase;
        this.confirmedRevision = model.confirmedRevision;
        this.choices.set(model.cars.map((car) => ({ ...car.selected })));
      }
    });
  }
  editable() {
    return (
      this.model().state === "selected" || this.model().state === "rejected"
    );
  }
  choose(index: number, change: Partial<CarInstruction>) {
    if (this.editable())
      this.choices.update((choices) =>
        choices.map((choice, i) =>
          i === index ? { ...choice, ...change } : choice,
        ),
      );
  }
  useRecommendation(){
    const advice=this.model().assistance?.recommendation;if(!this.editable()||!advice||advice.phase+1!==this.model().phase)return;
    if(this.model().safetyCar){this.choices.update(choices=>choices.map((choice,index)=>{const suggestion=advice.safetyCarCars?.find(car=>car.driverId===this.model().cars[index].id)?.suggested;return suggestion?{...choice,stop:suggestion.pit as "Pit"|"Stay Out",compound:suggestion.compound??choice.compound}:choice;}));return;}
    this.choices.update(choices=>choices.map((choice,index)=>{const suggestion=advice.cars.find(car=>car.driverId===this.model().cars[index].id)?.suggested;return suggestion?{pace:suggestion.pace,stop:suggestion.pit as "Pit"|"Stay Out",compound:suggestion.compound??choice.compound,battle:suggestion.battle}:choice;}));
  }
  compound(index: number, event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    if (value === "Soft" || value === "Medium" || value === "Hard" || this.model().wetTyresEnabled && (value === "Intermediate" || value === "Wet"))
      this.choose(index, { compound: value as Compound });
  }
  summary(choice: CarInstruction) {
    return `${choice.pace} · ${choice.stop}${choice.stop === "Pit" ? " · " + choice.compound : ""} · ${choice.battle}`;
  }
  keepFocus(event: KeyboardEvent) {
    if (event.key !== "Tab") return;
    const buttons =
      this.confirmation().nativeElement.querySelectorAll<HTMLButtonElement>(
        "button",
      );
    const first = buttons[0];
    const last = buttons[buttons.length - 1];
    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  }
  review() {
    this.confirmation().nativeElement.showModal();
  }
  close() {
    this.confirmation().nativeElement.close();
  }
  submit() {
    const plan: TeamPlan = {
      phase: this.model().phase,
      cars: this.model().cars.map((car, i) => ({
        id: car.id,
        instruction: { ...this.choices()[i] },
      })),
    };
    this.close();
    this.submitPlan.emit(plan);
  }
}
