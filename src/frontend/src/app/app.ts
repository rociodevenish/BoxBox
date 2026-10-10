import { SharedLobby } from './shared/shared-lobby';
import {OpenDisplay} from './shared/open-display';
import { ResultsGallery } from "./gallery/results-gallery";
import { RacePage } from './race/race-page';
import { ControllerGallery } from "./gallery/controller-gallery";
import { Component, signal } from "@angular/core";
import { TimingTower } from "./ui/timing/timing-tower";
import { publicRace } from "./gallery/public-race-fixtures";
import { DriverCard } from "./ui/driver-card";
import { max, lewis, unknown } from "./gallery/driver-fixtures";
@Component({
  selector: "app-root",
  imports: [DriverCard, TimingTower, ControllerGallery, ResultsGallery, RacePage, SharedLobby,OpenDisplay],
  templateUrl: "./app.html",
  styleUrl: "./app.css",
})
export class App {
  readonly openView=location.pathname.startsWith('/shared/open/');
  readonly sharedView = location.pathname.startsWith('/shared/');
  readonly raceView = location.pathname.startsWith('/race/');
  readonly controllerView =
    new URLSearchParams(location.search).get("view") === "controller";
  readonly resultsView =
    new URLSearchParams(location.search).get("view") === "results";
  readonly leaderPortraitFailed = signal(false);
  readonly publicView =
    new URLSearchParams(location.search).get("view") === "public";
  readonly publicRace = publicRace;
  readonly max = max;
  readonly lewis = lewis;
  readonly unknown = unknown;
}
