import { Component, input } from "@angular/core";
import { PublicGap } from "./public-classification.model";
@Component({
  selector: "app-race-gap",
  template: `@switch (gap().kind) {
    @case ("leader") {
      <span>Leader</span>
    }
    @case ("time") {
      <span>+{{ seconds() }} s</span>
    }
    @case ("laps") {
      <span>+{{ laps() }} {{ laps() === 1 ? "lap" : "laps" }}</span>
    }
    @case ("unavailable") {
      <span aria-label="Gap unavailable">Unavailable</span>
    }
  }`,
  styles: `
    :host {
      display: block;
      font-variant-numeric: tabular-nums;
      white-space: nowrap;
    }
  `,
})
export class RaceGap {
  readonly gap = input.required<PublicGap>();
  seconds(): string {
    const gap = this.gap();
    return gap.kind === "time" ? gap.seconds.toFixed(3) : "";
  }
  laps(): number {
    const gap = this.gap();
    return gap.kind === "laps" ? gap.laps : 0;
  }
}
