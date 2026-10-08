import { Component, input, signal } from "@angular/core";
import { TimingTower } from "../timing/timing-tower";
import { RaceEvent } from "./race-event";
import { PublicPhaseResult } from "./public-phase-result.model";
@Component({
  selector: "app-phase-result",
  imports: [TimingTower, RaceEvent],
  templateUrl: "./phase-result.html",
  styleUrl: "./phase-result.css",
})
export class PhaseResult {
  readonly fixtureLabel = input<string | null>(null);
  readonly result = input.required<PublicPhaseResult>();
  readonly failedContextUrl = signal<string | null>(null);
  title() {
    return this.result().state === "finished"
      ? "Race finished"
      : this.result().state === "resolving"
        ? "Resolving phase " + this.result().classification.phase
        : "Phase " + this.result().classification.phase + " results";
  }
}
