import { Component, input } from "@angular/core";
import { PublicTimingRow } from "./public-classification.model";
import { DriverIdentity } from "./driver-identity";
import { RaceGap } from "./race-gap";
import { StopStatus } from "./stop-status";
import { CompoundBadge } from "../compound-badge";
@Component({
  selector: "tr[app-timing-row]",
  imports: [DriverIdentity, RaceGap, StopStatus, CompoundBadge],
  host: { "[class.leader]": 'row().gap.kind === "leader"' },
  styleUrl: "./timing-row.css",
  template: `<td class="position">{{ row().position }}</td>
    <th scope="row">
      <app-driver-identity [driver]="row().driver" />
      @if (row().stopStatus !== "on-track") {
        <div class="mobile-status">
          <app-stop-status [status]="row().stopStatus" />
        </div>
      }
    </th>
    <td><app-race-gap [gap]="row().gap" /></td>
    <td><app-compound-badge [compound]="row().compound" /></td>
    <td class="status-cell">
      <app-stop-status [status]="row().stopStatus" />
    </td>`,
  styles: `
    :host {
      background: var(--color-surface);
    }
    :host.leader {
      background: #342322;
    }
    td,
    th {
      border-bottom: 1px solid #454b50;
      padding: 4px 8px;
      text-align: left;
      vertical-align: middle;
    }
    th {
      font-weight: 400;
    }
    .position {
      font-weight: 800;
      text-align: center;
    }
    .mobile-status {
      display: none;
    }
    @media (max-width: 600px) {
      td,
      th {
        padding: 6px 4px;
        font-size: 13px;
      }
      .status-cell {
        display: none;
      }
      .mobile-status {
        display: block;
        margin-top: 4px;
      }
    }
  `,
})
export class TimingRow {
  readonly row = input.required<PublicTimingRow>();
}
