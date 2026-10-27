import { DriverCardViewModel } from "../ui/driver-card.model";
import portraits from "../../../public/portraits/manifest.json";
function portrait(id: string): DriverCardViewModel["portrait"] {
  const asset = portraits.portraits.find((p) => p.driverId === id);
  return asset ? { url: asset.url, position: asset.objectPosition } : null;
}
export const max: DriverCardViewModel = {
  id: "max-verstappen",
  firstName: "Max",
  surname: "Verstappen",
  portrait: portrait("max-verstappen"),
  ownership: { name: "Apex", accent: "#73ddd2" },
  attributes: { Pace: 5, Racecraft: 5, Tyres: 4, Consistency: 4, Technical: 3 },
  details: [
    { label: "Qualifying Pace", rating: 5 },
    { label: "Race Pace", rating: 5 },
    { label: "Overtaking", rating: 5 },
    { label: "Defending", rating: 4 },
    { label: "Setup Feedback", rating: null },
  ],
  preferences: ["high speed"],
  priceLabel: "12 credits",
};
export const lewis: DriverCardViewModel = {
  id: "lewis-hamilton",
  firstName: "Lewis",
  surname: "Hamilton",
  portrait: portrait("lewis-hamilton"),
  ownership: { name: "Apex", accent: "#73ddd2" },
  attributes: {
    Pace: 4,
    Racecraft: 5,
    Tyres: 4,
    Consistency: 5,
    Technical: null,
  },
  details: [
    { label: "Qualifying Pace", rating: 4 },
    { label: "Race Pace", rating: 5 },
    { label: "Tyre Preservation", rating: 4 },
    { label: "Setup Feedback", rating: null },
  ],
  preferences: ["balanced setup"],
  priceLabel: "11 credits",
};
export const unknown: DriverCardViewModel = {
  id: "alexandra-fixture",
  firstName: "Alexandra",
  surname: "Montgomery-Sutherland",
  portrait: null,
  ownership: null,
  attributes: {
    Pace: null,
    Racecraft: null,
    Tyres: null,
    Consistency: null,
    Technical: null,
  },
  details: [
    { label: "Qualifying Pace", rating: null },
    { label: "Setup Feedback", rating: null },
  ],
  preferences: [],
};
