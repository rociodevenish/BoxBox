import { Component, input, signal } from "@angular/core";
import { CompoundBadge } from "../compound-badge";
import { PublicRaceEvent } from "./public-phase-result.model";
@Component({
  selector: "app-race-event",
  imports: [CompoundBadge],
  template: ` <article [class.minimalist]="event().minimalist" [attr.aria-label]="label() + ' · lap ' + event().lap">
    <div class="portrait">
      @if (event().portraitUrl && failedUrl() !== event().portraitUrl) {
        <img
          [src]="event().portraitUrl"
          [alt]="event().driverName + ' portrait'"
          (error)="failedUrl.set(event().portraitUrl)"
        />
      } @else {
        <span aria-hidden="true">{{ event().driverName.charAt(0) }}</span>
      }
    </div>
    <div class="copy">
      <p class="label">{{ label() }} · Lap {{ event().lap }}</p>
      <p>{{ event().summary }}</p>
      @if (event().compound) {
        <app-compound-badge [compound]="event().compound" />
      }
    </div>
  </article>`,
  styles: `
    :host {
      display: block;
    }
    article {
      display: flex;
      gap: 12px;
      border-top: 1px solid var(--color-border);
      padding: 12px 0;
    }
    article.minimalist { gap: 6px; padding: 6px 0; color: var(--color-muted); font-size: 12px; }
    .minimalist .portrait, .minimalist app-compound-badge { display: none; }
    .minimalist .copy { display: flex; gap: 8px; flex-wrap: wrap; align-items: baseline; }
    .minimalist .copy p { margin: 0; font-size: 12px; font-weight: 400; color: var(--color-muted); }
    .portrait {
      width: 52px;
      height: 64px;
      flex: none;
      background: var(--color-raised);
      overflow: hidden;
      display: grid;
      place-items: center;
      color: var(--color-muted);
      font-size: 24px;
    }
    .portrait img {
      width: 100%;
      height: 100%;
      object-fit: cover;
      object-position: center 20%;
    }
    .copy {
      min-width: 0;
    }
    .copy > p {
      overflow-wrap: anywhere;
    }
    .label {
      font-weight: 700;
      color: var(--color-driver);
      font-size: 13px;
    }
    p {
      margin-bottom: 4px;
    }
    app-compound-badge {
      font-size: 13px;
    }
  `,
})
export class RaceEvent {
  readonly event = input.required<PublicRaceEvent>();
  readonly failedUrl = signal<string | null>(null);
  label() {
    const kind = this.event().kind;
    return kind === "weather" ? "Observed weather / tyre decision" : kind === "incident" ? "Incident" : kind === "overtake"
      ? "Overtake"
      : kind === "pit-entry"
        ? "Pit entry"
        : kind === "pit-exit"
          ? "Pit exit"
          : "Tyre change";
  }
}
