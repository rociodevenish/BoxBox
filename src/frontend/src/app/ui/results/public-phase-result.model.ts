import {
  PublicClassification,
  TyreCompound,
} from "../timing/public-classification.model";
export interface PublicRaceEvent {
  readonly id: string;
  readonly kind: "overtake" | "pit-entry" | "tyre-change" | "pit-exit";
  readonly lap: number;
  readonly summary: string;
  readonly driverName: string;
  readonly portraitUrl: string | null;
  readonly compound: TyreCompound | null;
}
/** Already-public result data; filtering and lifecycle authority belong to the session adapter. */
export interface PublicPhaseResult {
  readonly state: "initial" | "resolving" | "phase-results" | "finished";
  readonly classification: PublicClassification;
  readonly events: readonly PublicRaceEvent[];
  readonly context: {
    readonly name: string;
    readonly description: string;
    readonly imageUrl: string | null;
  };
}
