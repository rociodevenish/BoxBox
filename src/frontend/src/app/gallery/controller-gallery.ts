import { Component, signal } from "@angular/core";
import { TwoCarController } from "../ui/controller/two-car-controller";
import {
  ControllerState,
  PrivateControllerViewModel,
  TeamPlan,
} from "../ui/controller/controller.model";
const fixture: PrivateControllerViewModel = {
  teamName: "Apex",
  phase: 12,
  state: "selected",
  message: "Draft choices are editable. Nothing has been submitted.",
  cars: [
    {
      id: "max",
      name: "Max Verstappen",
      currentCompound: "Medium",
      tyreEstimate: "62–70%",
      battleOptions: ['Neutral', 'Attack', 'Defend'],
      battleExplanation: "Nearby opponent: battle choices available.",
      pendingStop: null,
      selected: {
        pace: "Standard",
        stop: "Stay Out",
        compound: "Medium",
        battle: "Neutral",
      },
    },
    {
      id: "alex",
      name: "Alexandra Montgomery-Sutherland",
      currentCompound: "Hard",
      tyreEstimate: "44–52%",
      battleOptions: ['Neutral'],
      battleExplanation: "No nearby battle. Neutral remains selected.",
      pendingStop: null,
      selected: {
        pace: "Standard",
        stop: "Stay Out",
        compound: "Hard",
        battle: "Neutral",
      },
    },
  ],
};
@Component({
  selector: "app-controller-gallery",
  imports: [TwoCarController],
  template: ` <main class="gallery">
    <a href="/">Back to gallery</a
    ><app-two-car-controller [model]="model()" (submitPlan)="submit($event)" />
    <aside aria-label="Gallery fixture controls">
      <h2>Fixture controls</h2>
      <p>
        Local examples only. A future session adapter supplies these states and
        availability.
      </p>
      <div class="fixture-actions">
        <button
          (click)="state('locked')"
          [disabled]="model().state !== 'submitted'"
        >
          Acknowledge and lock</button
        ><button
          (click)="state('rejected')"
          [disabled]="model().state !== 'submitted'"
        >
          Reject submission</button
        ><button (click)="state('loading')">Show loading</button
        ><button (click)="state('resolving')">Show resolving</button
        ><button (click)="pending()">Show pending stop</button
        ><button (click)="reset()">Reset fixture</button>
      </div>
    </aside>
  </main>`,
  styles: `
    .gallery {
      max-width: 1164px;
      margin: auto;
      padding: 16px;
    }
    a {
      margin-bottom: 20px;
    }
    aside {
      margin-top: 32px;
      border-top: 1px solid var(--color-border);
      padding-top: 20px;
      color: var(--color-muted);
    }
    h2 {
      font-size: 20px;
    }
    p {
      margin: 8px 0;
    }
    .fixture-actions {
      display: flex;
      flex-wrap: wrap;
      gap: 12px;
    }
    button {
      background: var(--color-raised);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-control);
      padding: 8px 12px;
      min-height: 44px;
      color: var(--color-ink);
    }
    button:disabled {
      opacity: 0.55;
    }
    @media (min-width: 700px) {
      .gallery {
        padding: 32px;
      }
    }
  `,
})
export class ControllerGallery {
  readonly model = signal(fixture);
  submit(plan: TeamPlan) {
    this.model.update((model) => ({
      ...model,
      state: "submitted",
      message:
        "Both cars submitted. Waiting for acknowledgement; editing is unavailable.",
      cars: model.cars.map((car) => ({
        ...car,
        selected: plan.cars.find((item) => item.id === car.id)!.instruction,
      })),
    }));
  }
  state(state: ControllerState) {
    const messages = {
      selected: "Draft choices are editable.",
      submitted: "Waiting for acknowledgement.",
      locked: "Both cars locked. Wait for the next phase.",
      loading:
        "Loading team information. Instructions are unavailable until loading completes.",
      resolving: "Race phase resolving. Wait for the next decision window.",
      rejected:
        "Submission rejected: Car 1 pit selection could not be accepted. Review or change your retained choices and submit again.",
    };
    this.model.update((model) => ({
      ...model,
      state,
      message: messages[state],
    }));
  }
  pending() {
    this.model.update((model) => ({
      ...model,
      phase: model.phase + 1,
      state: "selected",
      message: "Draft choices are editable; Car 2 already has a pending stop.",
      cars: model.cars.map((car, i) =>
        i === 1
          ? {
              ...car,
              pendingStop:
                "Pit stop pending · Hard. Pit instruction cannot be changed.",
              selected: { ...car.selected, stop: "Pit", compound: "Hard" },
            }
          : car,
      ),
    }));
  }
  reset() {
    this.model.update((model) => ({ ...fixture, phase: model.phase + 1 }));
  }
}
