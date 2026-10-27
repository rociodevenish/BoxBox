/** Presentation data only. Ratings are not conversions of simulation inputs. */
export type DriverAttributeGroup =
  | "Pace"
  | "Racecraft"
  | "Tyres"
  | "Consistency"
  | "Technical";
export type DisplayRating = 1 | 2 | 3 | 4 | 5 | null;
export interface CardAttribute {
  readonly label: string;
  readonly rating: DisplayRating;
  readonly displayValue?: number;
  readonly scale?: number;
}
export interface DriverCardViewModel {
  readonly id: string;
  readonly firstName: string;
  readonly surname: string;
  readonly portrait: {
    readonly url: string;
    readonly position?: string;
  } | null;
  readonly ownership: { readonly name: string; readonly accent: string } | null;
  readonly attributes: Readonly<Record<DriverAttributeGroup, DisplayRating>>;
  readonly details: readonly CardAttribute[];
  readonly preferences: readonly string[];
  readonly priceLabel?: string;
}
export const driverAttributeGroups: readonly DriverAttributeGroup[] = [
  "Pace",
  "Racecraft",
  "Tyres",
  "Consistency",
  "Technical",
];
export function driverName(driver: DriverCardViewModel): string {
  return `${driver.firstName} ${driver.surname}`;
}
