import { Component, input, output } from "@angular/core";
import { PublicTimingRow } from "./public-classification.model";
import { DriverIdentity } from "./driver-identity";
import { RaceGap } from "./race-gap";
import { StopStatus } from "./stop-status";
import { CompoundBadge } from "../compound-badge";
@Component({
  selector: "tr[app-timing-row]",
  imports: [DriverIdentity, RaceGap, StopStatus, CompoundBadge],
  host: { "[class.leader]": 'row().position === 1', '[class.compact]': 'compact()' },
  styleUrl: "./timing-row.css",
  template: `<td class="position">{{ row().position }}</td>
    <th scope="row">
      <div class="driver-heading">@if(interactive()){<button class="driver-details" [attr.aria-label]="row().driver.name" (click)="driverSelected.emit(row().driver.id)"><app-driver-identity [driver]="row().driver" /></button>}@else{<app-driver-identity [driver]="row().driver" />}@if(row().isFastestLap){<span aria-label="Fastest lap"> ◆</span>}</div>
      @if(row().eligibleAtFinish !== undefined && row().eligibleAtFinish !== null){<p class="mobile-finish">{{row().completedLaps}} laps · {{row().eligibleAtFinish ? 'Eligible' : 'Ineligible'}}</p>}
      @if (row().stopStatus !== "on-track") {
        <div class="mobile-status">
          <app-stop-status [status]="row().stopStatus" />
        </div>
      }
    </th>
    <td><app-race-gap [gap]="row().gap" /></td>
    <td><div class="tyre-content"><app-compound-badge [compound]="row().compound" />
      @if(row().tyreAge!==undefined){<span class="tyre-age" [attr.aria-label]="'Tyre age ' + row().tyreAge + ' laps'">{{row().tyreAge}} {{row().tyreAge===1?'lap':'laps'}}</span>}
    </div></td>
    @if(row().completedPitStops!==undefined){<td [attr.aria-label]="row().completedPitStops+' completed '+(row().completedPitStops===1?'stop':'stops')">{{row().completedPitStops}}</td>}<td class="status-cell">
      @if(row().eligibleAtFinish !== undefined && row().eligibleAtFinish !== null){<span class="finish-status">{{row().completedLaps}} laps · {{row().eligibleAtFinish ? 'Eligible' : 'Ineligible'}}</span>}@else{<app-stop-status [status]="row().stopStatus" />}
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
    .driver-details {min-height:0;padding:0; border:0; background:transparent; color:inherit; width:100%; text-align:left; border-radius:0; font:inherit;}
    .position {
      font-weight: 800;
      text-align: center;
    }
    .mobile-status {
      display: none;
    }
    .mobile-finish { display:none; }
    .driver-heading {display:flex;align-items:center;gap:4px;} .driver-heading app-driver-identity {flex:1;min-width:0;} .status-cell {white-space:nowrap;} .finish-status { font-size:13px; white-space:nowrap; }
    @media(min-width:1200px){:host.compact td,:host.compact th {padding:2px 8px;}}
    .tyre-content { display:flex; align-items:center; gap:8px; }
    .tyre-age { font-size:12px; color:var(--color-muted); white-space:nowrap; }
    @container timing (max-width: 600px) {
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
      .mobile-finish { display:block; font-size:12px; margin-top:4px; }
      .tyre-content { flex-direction:column; align-items:flex-start; gap:3px; }
    }
  `,
})
export class TimingRow {
  readonly interactive = input(false);
  readonly driverSelected = output<string>();
  readonly compact = input(false);
  readonly row = input.required<PublicTimingRow>();
}
