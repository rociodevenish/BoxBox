// A snapshot read advances only the server clock; compare every other public field.
export function comparablePublicState(snapshot:Record<string,unknown>){
  return Object.fromEntries(Object.entries(snapshot).filter(([key])=>key!=='serverTimeUtc'));
}
