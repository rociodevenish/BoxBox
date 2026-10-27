import {
  PublicClassification,
  TyreCompound,
} from "../ui/timing/public-classification.model";
const identities = [
  ["max-verstappen", "VER", "Max Verstappen"],
  ["lewis-hamilton", "HAM", "Lewis Hamilton"],
  ["lando-norris", "NOR", "Lando Norris"],
  ["oscar-piastri", "PIA", "Oscar Piastri"],
  ["george-russell", "RUS", "George Russell"],
  ["charles-leclerc", "LEC", "Charles Leclerc"],
  ["carlos-sainz", "SAI", "Carlos Sainz"],
  ["fernando-alonso", "ALO", "Fernando Alonso"],
  ["alexander-albon", "ALB", "Alexander Albon"],
  ["pierre-gasly", "GAS", "Pierre Gasly"],
  ["esteban-ocon", "OCO", "Esteban Ocon"],
  ["isack-hadjar", "HAD", "Isack Hadjar"],
  ["liam-lawson", "LAW", "Liam Lawson"],
  ["yuki-tsunoda", "TSU", "Yuki Tsunoda"],
  ["kimi-antonelli", "ANT", "Andrea Kimi Antonelli"],
  ["gabriel-bortoleto", "BOR", "Gabriel Bortoleto"],
  ["oliver-bearman", "BEA", "Oliver Bearman"],
  ["nico-hulkenberg", "HUL", "Nico Hülkenberg"],
  ["alexandra-fixture", "MSU", "Alexandra Montgomery-Sutherland"],
  ["franco-colapinto", "COL", "Franco Colapinto"],
  ["valtteri-bottas", "BOT", "Valtteri Bottas"],
  ["sergio-perez", "PER", "Sergio Pérez"],
] as const;
const teams = [
  { name: "Apex", accent: "#73ddd2" },
  { name: "Ember", accent: "#ffb972" },
  { name: "Orion", accent: "#b5b2ff" },
  { name: "Coral", accent: "#ffaaa6" },
];
const compounds: readonly TyreCompound[] = ["Soft", "Medium", "Hard"];
/** Synthetic Classification. Stop pending denotes a publicly announced stop, not a hidden choice. */
export const publicRace: PublicClassification = {
  phase: 4,
  leaderLap: 20,
  totalLaps: 40,
  rows: identities.map(([id, code, name], index) => ({
    position: index + 1,
    driver: {
      id,
      code,
      name,
      team: teams[Math.floor(index / 2) % teams.length],
    },
    gap:
      index === 0
        ? { kind: "leader" }
        : index === 20
          ? { kind: "unavailable" }
          : index === 21
            ? { kind: "laps", laps: 2 }
            : index >= 18
              ? { kind: "laps", laps: 1 }
              : { kind: "time", seconds: index * 1.374 },
    compound: index === 20 ? null : compounds[index % 3],
    stopStatus:
      index === 6
        ? "announced-stop-pending"
        : index === 7
          ? "in-pit-lane"
          : "on-track",
  })),
};
