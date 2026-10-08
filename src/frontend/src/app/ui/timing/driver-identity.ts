import { Component, input } from "@angular/core";
import { PublicDriverIdentity } from "./public-classification.model";
@Component({
  selector: "app-driver-identity",
  template: `<span class="identity" [style.--team-accent]="driver().team.accent"
    ><strong>{{ driver().code }}</strong
    ><span class="full-name">{{ driver().name }}</span
    ><span class="sr-only">{{ driver().team.name }}</span></span
  >`,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }
    .identity {
      display: flex;
      align-items: center;
      gap: 10px;
      border-left: 4px solid var(--team-accent);
      padding-left: 10px;
      min-width: 0;
    }
    strong {
      font-weight: 800;
      letter-spacing: 1px;
      flex-shrink: 0;
    }
    .full-name {
      font-weight: 400;
      font-size: 14px;
      overflow-wrap: anywhere;
      min-width: 0;
    }
    @media (max-width: 600px) {
      .identity {
        display: block;
        padding-left: 6px;
      }
      .full-name {
        display: block;
        font-size: 13px;
      }
    }
  `,
})
export class DriverIdentity {
  readonly driver = input.required<PublicDriverIdentity>();
}
