import { Component, input } from "@angular/core";
import { PublicStopStatus } from "./public-classification.model";
@Component({
  selector: "app-stop-status",
  template: `<span
    class="status"
    [class.pending]="status() === 'announced-stop-pending'"
    [class.pitting]="status() === 'in-pit-lane'"
    >{{ label() }}</span
  >`,
  styles: `
    .status {
      font-size: 13px;
      color: var(--color-muted);
    }
    .pending {
      color: var(--color-warning);
    }
    .pitting {
      color: var(--color-ink);
      border: 1px solid var(--color-border);
      border-radius: 4px;
      padding: 3px 6px;
      white-space: nowrap;
    }
  `,
})
export class StopStatus {
  readonly status = input.required<PublicStopStatus>();
  label(): string {
    return this.status() === "retired" ? "Retired" : this.status() === "announced-stop-pending"
      ? "Stop pending"
      : this.status() === "in-pit-lane"
        ? "In pit lane"
        : "On track";
  }
}
