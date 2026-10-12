namespace Game.Domain;

// Negative means Speed / Responsive / Warm-Up; positive means Downforce / Stable / Preservation.
public sealed record WeekendSetup(
    [property: System.Text.Json.Serialization.JsonRequired] int Aero = 0,
    [property: System.Text.Json.Serialization.JsonRequired] int Mechanical = 0,
    [property: System.Text.Json.Serialization.JsonRequired] int Tyres = 0)
{
    public const string Version = "weekend-setup-v1";
    public void Validate()
    {
        if (Aero is < -2 or > 2 || Mechanical is < -2 or > 2 || Tyres is < -2 or > 2)
            throw new ArgumentException("All three setup axes require a discrete choice from -2 to 2.");
    }
}
public sealed record DriverSetupFit(string DriverId, string Name, double Compatibility, string Description);
