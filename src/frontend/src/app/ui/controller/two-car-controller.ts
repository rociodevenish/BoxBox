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
  imports: [CompoundBadge],
  templateUrl: "./two-car-controller.html",
  styleUrl: "./two-car-controller.css",
})
export class TwoCarController {
  readonly model = input.required<PrivateControllerViewModel>();
  readonly submitPlan = output<TeamPlan>();
  readonly choices = signal<readonly CarInstruction[]>([]);
  readonly confirmation =
    viewChild.required<ElementRef<HTMLDialogElement>>("confirmation");
  readonly paces: readonly Pace[] = ["Conserve", "Standard", "Push"];
  readonly battles: readonly Battle[] = ["Neutral", "Attack", "Defend"];
  private draftPhase: number | undefined;
  constructor() {
    effect(() => {
      const model = this.model();
      if (this.draftPhase !== model.phase) {
        this.draftPhase = model.phase;
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
  compound(index: number, event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    if (value === "Soft" || value === "Medium" || value === "Hard")
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
