using System.Text.Json;
namespace Game.Application;
/// <summary>Account lookup metadata projected from committed recovery contracts, never browser hints.</summary>
public static class SessionMembershipIndex
{
 public static string[] Members(string payload)
 {
  try
  {
  using var json=JsonDocument.Parse(payload);var root=json.RootElement;if(root.ValueKind!=JsonValueKind.Object)return [];
  if(!root.TryGetProperty("schema",out var schema)||schema.GetString()!="session-recovery-v1"||!root.TryGetProperty("lobby",out var lobby)||lobby.ValueKind!=JsonValueKind.Object)return [];
  return lobby.GetProperty("players").EnumerateArray().Where(p=>p.TryGetProperty("uid",out var uid)&&uid.ValueKind==JsonValueKind.String).Select(p=>p.GetProperty("uid").GetString()!).Distinct(StringComparer.Ordinal).ToArray();
  }
  catch(JsonException){return [];}
 }
 // Versioned authored calendar identity; candidates must match authoritative saved calendars.
 public static string? CampaignCandidate(string sessionId)
 {
  const string suffix="-event-01-weekend";
  return sessionId.Length==32+suffix.Length&&sessionId.EndsWith(suffix,StringComparison.Ordinal)&&sessionId[..32].All(char.IsAsciiHexDigit)?sessionId[..32]:null;
 }
}
