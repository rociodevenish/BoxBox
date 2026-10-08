import { ControllerGallery } from "./gallery/controller-gallery";
import { Component, signal } from "@angular/core";
import { TimingTower } from "./ui/timing/timing-tower";
import { publicRace } from "./gallery/public-race-fixtures";
import { DriverCard } from "./ui/driver-card";
import { max, lewis, unknown } from "./gallery/driver-fixtures";
@Component({
  selector: "app-root",
  imports: [DriverCard, TimingTower, ControllerGallery],
  templateUrl: "./app.html",
  styleUrl: "./app.css",
})
export class App {
  readonly controllerView =
    new URLSearchParams(location.search).get("view") === "controller";
  readonly leaderPortraitFailed = signal(false);
  readonly publicView =
    new URLSearchParams(location.search).get("view") === "public";
  readonly publicRace = publicRace;
  readonly max = max;
  readonly lewis = lewis;
  readonly unknown = unknown;
}
