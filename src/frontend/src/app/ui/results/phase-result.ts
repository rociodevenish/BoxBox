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
  readonly board = input(false);
  readonly compact = input(false);
  readonly liveRace = input(false);
  readonly feedbackFirst = input(false);
  readonly result = input.required<PublicPhaseResult>();
  readonly failedContextUrl = signal<string | null>(null);
  title() {
    if(this.liveRace()) return this.result().state === "finished" ? "Race finished" : this.result().state === "initial" ? "Race ready" : this.result().state === "presenting" ? "Recorded race feedback" : this.result().state === "resolving" ? "Resolving play…" : "Recorded race outcome";
    if (this.result().state === 'awaiting') return 'Phase ' + this.result().classification.phase + ' ready';
    if (this.result().state === 'presenting') return 'Recorded phase feedback';
    if (this.result().state === "initial") return "Race ready";
    return this.result().state === "finished"
      ? "Race finished"
      : this.result().state === "resolving"
        ? "Resolving phase " + this.result().classification.phase
        : "Phase " + this.result().classification.phase + " results";
  }
}
