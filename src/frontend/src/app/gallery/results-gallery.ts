import { Component, signal } from "@angular/core";
import { PhaseResult } from "../ui/results/phase-result";
import {
  PublicPhaseResult,
  PublicRaceEvent,
} from "../ui/results/public-phase-result.model";
import { publicRace } from "./public-race-fixtures";
import { max, lewis } from "./driver-fixtures";
const events: readonly PublicRaceEvent[] = [
  {
    id: "pass-1",
    kind: "overtake",
    lap: 18,
    summary: "Lewis Hamilton passed Max Verstappen for the lead.",
    driverName: "Lewis Hamilton",
    portraitUrl: lewis.portrait?.url ?? null,
    compound: null,
  },
  {
    id: "entry-1",
    kind: "pit-entry",
    lap: 19,
    summary: "Fernando Alonso entered the pit lane.",
    driverName: "Fernando Alonso",
    portraitUrl: null,
    compound: null,
  },
  {
    id: "tyres-1",
    kind: "tyre-change",
    lap: 19,
    summary: "Fernando Alonso changed Medium to Hard.",
    driverName: "Fernando Alonso",
    portraitUrl: null,
    compound: "Hard",
  },
  {
    id: "exit-1",
    kind: "pit-exit",
    lap: 19,
    summary: "Fernando Alonso exited the pit lane.",
    driverName: "Fernando Alonso",
    portraitUrl: null,
    compound: "Hard",
  },
  {
    id: "pass-2",
    kind: "overtake",
    lap: 20,
    summary: "Max Verstappen passed Lewis Hamilton to retake the lead.",
    driverName: "Max Verstappen",
    portraitUrl: max.portrait?.url ?? null,
    compound: null,
  },
];
const classification = {
  ...publicRace,
  rows: publicRace.rows.map((row) => ({
    ...row,
    stopStatus: "on-track" as const,
    compound:
      row.driver.id === "fernando-alonso" ? ("Hard" as const) : row.compound,
  })),
};
const phase: PublicPhaseResult = {
  state: "phase-results",
  classification,
  events,
  context: {
    name: "Lewis Hamilton",
    description:
      "Lead changed twice during the phase; Max leads at the boundary.",
    imageUrl: lewis.portrait?.url ?? null,
  },
};
@Component({
  selector: "app-results-gallery",
  imports: [PhaseResult],
  template: `<app-phase-result
      [result]="result()"
      fixtureLabel="Design fixtures"
    />
    <nav aria-label="Result fixture controls">
      <a href="/">Back to gallery</a
      ><button (click)="show('resolving')">Show resolving</button
      ><button (click)="show('quiet')">Show quiet phase</button
      ><button (click)="show('crowded')">Show crowded phase</button
      ><button (click)="show('phase')">Show phase results</button
      ><button (click)="show('finished')">Show race finish</button>
    </nav>`,
  styles: `
    nav {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
      padding: 16px 24px;
      border-top: 1px solid var(--color-border);
    }
    button {
      background: var(--color-raised);
      color: var(--color-ink);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-control);
      padding: 8px 12px;
      min-height: 44px;
    }
  `,
})
export class ResultsGallery {
  readonly result = signal(phase);
  show(state: "resolving" | "quiet" | "crowded" | "phase" | "finished") {
    if (state === "finished") {
      this.result.set({
        state: "finished",
        classification: { ...classification, phase: 8, leaderLap: 40 },
        events: [],
        context: {
          name: "Max Verstappen",
          description:
            "Winner · 40 laps completed. Final race order is shown alongside.",
          imageUrl: max.portrait?.url ?? null,
        },
      });
    } else if (state === "resolving") {
      this.result.set({
        ...phase,
        state: "resolving",
        events: [],
        context: {
          name: "Published race state",
          description: "The next result has not been published yet.",
          imageUrl: null,
        },
      });
    } else if (state === "quiet") {
      this.result.set({
        ...phase,
        events: [],
        context: {
          name: "Quiet phase",
          description:
            "No overtakes or pit events were reported. Classification remains the phase result.",
          imageUrl: null,
        },
      });
    } else if (state === "crowded") {
      this.result.set({
        ...phase,
        classification: {
          ...classification,
          rows: classification.rows.map((row, i) =>
            i >= 8 && i <= 16
              ? { ...row, stopStatus: "in-pit-lane" as const }
              : row,
          ),
        },
        events: [
          ...events,
          ...Array.from(
            { length: 9 },
            (_, i): PublicRaceEvent => ({
              id: "crowded-" + i,
              kind: "pit-entry",
              lap: 20,
              summary:
                classification.rows[i + 8].driver.name +
                " entered the pit lane.",
              driverName: classification.rows[i + 8].driver.name,
              portraitUrl: null,
              compound: null,
            }),
          ),
        ],
      });
    } else this.result.set(phase);
  }
}
