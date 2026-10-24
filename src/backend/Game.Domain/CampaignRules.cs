namespace Game.Domain;

/// <summary>Initial preset-campaign lifecycle rules, independent of hosting and persistence.</summary>
public static class CampaignRules
{
    public const string Version="preset-campaign-lifecycle-v1";
    public static string Lifecycle(string saved,string weekend)=>saved=="event-result-committed"?saved:weekend is "qualifying" or "started"||saved=="active-weekend"
        ?"active-weekend":weekend=="preparing"?"event-preparation":saved;
    public static bool CanStart(string lifecycle,IReadOnlyList<bool> finalReady)=>
        lifecycle=="event-preparation"&&finalReady.Count>0&&finalReady.All(ready=>ready);
    public static void ValidateShortCalendar(IReadOnlyList<string> trackIds)
    {
        if(trackIds.Count<2||trackIds.Distinct(StringComparer.Ordinal).Count()!=trackIds.Count)
            throw new ArgumentException("A short campaign requires at least two distinct validated circuit identities.");
    }
}
