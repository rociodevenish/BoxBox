import { Component, input } from "@angular/core";
import { PublicClassification } from "./public-classification.model";
import { TimingRow } from "./timing-row";
@Component({
  selector: "app-timing-tower",
  imports: [TimingRow],
  template: `<div class="tower-panel" [class.compact]="classification().compact" [class.finished]="classification().rows[0]?.eligibleAtFinish != null">
    <div class="tower-heading">
      <h3>CLASSIFICATION</h3>
      <p>
        LAP {{ classification().leaderLap }} / {{ classification().totalLaps }}
      </p>
    </div>
    <table>
      <caption>
        Classification — {{ classification().gapReference === 'ahead' ? 'Interval to car ahead' : 'Gap to leader' }}
      </caption>
      <colgroup>
        <col class="position-column" />
        <col />
        <col class="gap-column" />
        <col class="compound-column" [class.with-age]="hasAge()" />
        <col class="status-column" />
      </colgroup>
      <thead>
        <tr>
          <th scope="col">Pos</th>
          <th scope="col">Driver</th>
          <th scope="col">{{classification().gapReference === 'ahead' ? 'Interval' : 'Gap'}}</th>
          <th scope="col">Tyre</th>
          <th scope="col" class="status-heading">{{classification().rows[0]?.eligibleAtFinish != null ? 'Distance / eligibility' : 'Stop status'}}</th>
        </tr>
      </thead>
      <tbody>
        @for (row of classification().rows; track row.driver.id) {
          <tr app-timing-row [row]="row" [compact]="classification().compact ?? false"></tr>
        }
      </tbody>
    </table>
  </div>`,
  styles: `
    :host {
      display: block;
      min-width: 0;
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
    .status-column {
      width: 126px;
    }
    .finished .status-column { width:190px; }
    @media(min-width:1200px){.compact .tower-heading {padding:10px 16px;} .compact caption {padding:8px 16px;} }
    @media (max-width: 600px) {
      .tower-heading {
        padding: 12px;
        font-size: 14px;
      }
      .position-column {
        width: 28px;
      }
      .gap-column {
        width: 82px;
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
  readonly classification = input.required<PublicClassification>();
  hasAge(){return this.classification().rows.some(row=>row.tyreAge!==undefined);}
}
