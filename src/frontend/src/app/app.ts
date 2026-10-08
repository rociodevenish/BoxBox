import { Component } from "@angular/core";
import { DriverCard } from "./ui/driver-card";
import { max, lewis, unknown } from "./gallery/driver-fixtures";
@Component({
  selector: "app-root",
  imports: [DriverCard],
  templateUrl: "./app.html",
})
export class App {
  readonly max = max;
  readonly lewis = lewis;
  readonly unknown = unknown;
}
