/** Already-public presentation data. This contract has no condition, estimate, seed or instruction fields. */
export type PublicGap =
  | { readonly kind: "leader" }
  | { readonly kind: "time"; readonly seconds: number }
  | { readonly kind: "laps"; readonly laps: number }
  | { readonly kind: "unavailable" };
export type TyreCompound = "Soft" | "Medium" | "Hard";
export type PublicStopStatus =
  | "on-track"
  | "announced-stop-pending"
  | "in-pit-lane"
  | "retired";
export interface PublicDriverIdentity {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly team: { readonly name: string; readonly accent: string };
}
export interface PublicTimingRow {
  readonly position: number;
  readonly driver: PublicDriverIdentity;
  readonly gap: PublicGap;
  readonly compound: TyreCompound | null;
  readonly stopStatus: PublicStopStatus;
  readonly tyreAge?: number;
  readonly completedLaps?: number;
  readonly completedPitStops?: number;
  readonly isFastestLap?: boolean;
  readonly eligibleAtFinish?: boolean | null;
}
export interface PublicClassification {
  readonly phase: number;
  readonly leaderLap: number;
  readonly totalLaps: number;
  readonly rows: readonly PublicTimingRow[];
  readonly gapReference?: 'leader' | 'ahead';
  readonly compact?: boolean;
}
