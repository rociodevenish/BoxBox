import { Component, input } from "@angular/core";
import { TyreCompound } from "./timing/public-classification.model";
@Component({
  selector: "app-compound-badge",
  template: `<span
    class="badge"
    [class.soft]="compound() === 'Soft'"
    [class.medium]="compound() === 'Medium'"
    [class.hard]="compound() === 'Hard'"
    [attr.aria-label]="
      compound() ? 'Tyre compound ' + compound() : 'Tyre compound unknown'
    "
  >
    @if (compound()) {
      <span class="compound-code" aria-hidden="true">{{ compound()?.charAt(0) }}</span><span class="compound-name" aria-hidden="true">{{ compound() }}</span>
    } @else {
      Unknown
    }
  </span>`,
  styles: `
    :host { display: inline-block; }
    .compound-code { display: none; }
    @media (max-width: 600px) { .compound-code { display: inline; } .compound-name { display: none; } }
    @container timing (max-width: 600px) { .compound-code { display: inline; } .compound-name { display: none; } }
    .badge {
      display: inline-block;
      border: 1px solid currentColor;
      border-radius: 4px;
      padding: 3px 5px;
      font-size: 13px;
      font-weight: 700;
      white-space: nowrap;
      color: var(--color-muted);
    }
    .soft {
      color: var(--color-tyre-soft);
    }
    .medium {
      color: var(--color-tyre-medium);
    }
    .hard {
      color: var(--color-tyre-hard);
    }
  `,
})
export class CompoundBadge {
  readonly compound = input.required<TyreCompound | null>();
}
