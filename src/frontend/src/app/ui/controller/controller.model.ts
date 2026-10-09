import {DriverCardViewModel} from '../driver-card.model';
import {PublicClassification} from '../timing/public-classification.model';
export type ControllerState =
  | "selected"
  | "submitted"
  | "locked"
  | "loading"
  | "resolving"
  | "rejected";
export type Pace = "Conserve" | "Standard" | "Push";
export type Battle = "Neutral" | "Attack" | "Defend";
export type Compound = "Soft" | "Medium" | "Hard";
export interface CarInstruction {
  readonly pace: Pace;
  readonly stop: "Stay Out" | "Pit";
  readonly compound: Compound;
  readonly battle: Battle;
}
export interface PrivateCarViewModel {
  readonly id: string;
  readonly card?: DriverCardViewModel;
  readonly nearbyClassification?: PublicClassification;
  readonly nearbyDrivers?: readonly DriverCardViewModel[];
  readonly recentLaps?: readonly {lap:number;seconds:number}[];
  readonly name: string;
  readonly tyreEstimate: string;
  readonly currentCompound: Compound;
  readonly battleOptions: readonly Battle[];
  readonly tyreAge?: number;
  readonly battleExplanation: string;
  readonly pendingStop: string | null;
  readonly selected: CarInstruction;
}
export interface PrivateControllerViewModel {
  readonly teamName: string;
  readonly phase: number;
  readonly decisionLaps?: number;
  readonly cars: readonly PrivateCarViewModel[];
  readonly state: ControllerState;
  readonly message: string;
  readonly confirmedRevision?: number;
  readonly submitLabel?: string;
}
export interface TeamPlan {
  readonly phase: number;
  readonly cars: readonly {
    readonly id: string;
    readonly instruction: CarInstruction;
  }[];
}
