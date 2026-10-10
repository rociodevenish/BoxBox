import { Component, input, output } from "@angular/core";
import { PublicClassification } from "./public-classification.model";
import { TimingRow } from "./timing-row";
@Component({
  selector: "app-timing-tower",
  imports: [TimingRow],
  template: `<div class="tower-panel" [class.inline]="inline()" [class.compact]="classification().compact" [class.finished]="classification().rows[0]?.eligibleAtFinish != null">
    @if(!inline()){<div class="tower-heading">
      @if(!inline()){<h3>CLASSIFICATION</h3>}
      <p>
        LAP {{ classification().leaderLap }} / {{ classification().totalLaps }}
      </p>
    </div>}
    <table>
      @if(!inline()){<caption>
        Classification — {{ classification().gapReference === 'ahead' ? 'Interval to car ahead' : 'Gap to leader' }}
      </caption>}
      <colgroup>
        <col class="position-column" />
        <col />
        <col class="gap-column" />
        <col class="compound-column" [class.with-age]="hasAge()" />
        @if(hasStopCounts()){<col class="stops-column" />}<col class="status-column" />
      </colgroup>
      <thead>
        <tr>
          <th scope="col">Pos</th>
          <th scope="col">Driver</th>
          <th scope="col">{{classification().gapReference === 'ahead' ? 'Interval' : 'Gap'}}</th>
          <th scope="col">Tyre</th>
          @if(hasStopCounts()){<th scope="col">Stops</th>}<th scope="col" class="status-heading">Status</th>
        </tr>
      </thead>
      <tbody>
        @for (row of classification().rows; track row.driver.id) {
          <tr app-timing-row [row]="row" [highlighted]="highlightedDriverId() ? row.driver.id === highlightedDriverId() : null" [compact]="classification().compact ?? false" [interactive]="interactive()" (driverSelected)="driverSelected.emit($event)"></tr>
        }
      </tbody>
    </table>
  </div>`,
  styles: `
    :host {
      display: block;
      min-width: 0;
      container: timing / inline-size;
    }
    .tower-panel {
      border: 1px solid var(--color-border);
      border-radius: var(--radius-panel);
      overflow: hidden;
      background: var(--color-surface);
    }
    .tower-heading {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 16px;
      background: var(--color-raised);
    }
    h3 {
      font-weight: 800;
      letter-spacing: 0.5px;
    }
    .tower-heading p {
      white-space: nowrap;
      font-variant-numeric: tabular-nums;
    }
    .tower-panel.inline { border: 0; border-radius: 0; background: transparent; }
    .inline .tower-heading { padding: 0 8px 12px; background: transparent; }
    .inline .tower-heading p { margin: 0; font-size: 24px; font-weight: 800; }
    table {
      width: 100%;
      border-collapse: collapse;
      table-layout: fixed;
    }
    caption {
      text-align: left;
      color: var(--color-muted);
      font-size: 13px;
      padding: 12px 16px;
    }
    thead th {
      color: var(--color-muted);
      font-size: 13px;
      text-align: left;
      padding: 6px 8px;
      border-bottom: 1px solid var(--color-border);
      font-weight: 400;
    }
    .position-column {
      width: 44px;
    }
    .gap-column {
      width: 108px;
    }
    .compound-column {
      width: 108px;
    }
    .compound-column.with-age { width:166px; }
    .stops-column {width:56px;}
    .status-column {
      width: 126px;
    }
    .finished .status-column { width:126px; }
    .inline .position-column { width: 7%; }
    .inline .gap-column { width: 18%; }
    .inline .compound-column, .inline .compound-column.with-age { width: 20%; }
    .inline .stops-column { width: 9%; }
    .inline .status-column, .inline.finished .status-column { width: 24%; }
    @media(min-width:1200px){.compact .tower-heading {padding:10px 16px;} .compact caption {padding:8px 16px;} }
    @container timing (max-width: 600px) { .stops-column{width:44px;}
      .tower-heading {
        padding: 12px;
        font-size: 14px;
      }
      .position-column {
        width: 28px;
      }
      .gap-column {
        width: 78px;
      }
      .compound-column, .compound-column.with-age {
        width: 94px;
      }
      .status-column,
      .status-heading {
        display: none;
      }
      thead th {
        padding: 6px 4px;
      }
      caption {
        padding: 10px 12px;
      }
    }
  `,
})
export class TimingTower {
  readonly highlightedDriverId = input<string | null>(null);
  readonly inline = input(false);
  readonly interactive = input(false);
  readonly driverSelected = output<string>();
  readonly classification = input.required<PublicClassification>();
  hasStopCounts(){return this.classification().rows.some(row=>row.completedPitStops!==undefined);}
  hasAge(){return this.classification().rows.some(row=>row.tyreAge!==undefined);}
}
