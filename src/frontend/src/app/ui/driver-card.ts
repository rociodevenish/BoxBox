import { Component, input, viewChild, ElementRef, signal } from "@angular/core";
import {
  DriverCardViewModel,
  driverAttributeGroups,
  driverName,
} from "./driver-card.model";
@Component({
  selector: "app-driver-card",
  templateUrl: "./driver-card.html",
  styleUrl: "./driver-card.css",
})
export class DriverCard {
  readonly driver = input.required<DriverCardViewModel>();
  readonly variant = input<"compact" | "expanded" | "tv" | "identity">("compact");
  readonly embedded = input(false);
  readonly failedPortrait = signal<string | null>(null);
  readonly groups = driverAttributeGroups;
  readonly fullName = driverName;
  private readonly dialog =
    viewChild.required<ElementRef<HTMLDialogElement>>("details");
  openDetails(): void {
    this.dialog().nativeElement.showModal();
  }
  closeDetails(): void {
    this.dialog().nativeElement.close();
  }
  keepDetailsFocus(event: KeyboardEvent): void {
    if (event.key === "Tab") event.preventDefault();
  }
}
