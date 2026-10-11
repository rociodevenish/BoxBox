namespace Game.Api;

public static class HostedConfiguration
{
    public static string[] Validate(IConfiguration configuration,IHostEnvironment environment)
    {
        var origins=(configuration["Hosting:AllowedOrigins"]??"").Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
        foreach(var origin in origins)
            if(!Uri.TryCreate(origin,UriKind.Absolute,out var uri)||uri.AbsolutePath!="/"||uri.Query.Length>0||uri.Fragment.Length>0||uri.UserInfo.Length>0
                ||uri.GetLeftPart(UriPartial.Authority)!=origin||uri.Scheme is not ("https" or "http")||!environment.IsDevelopment()&&uri.Scheme!="https")
                throw new InvalidOperationException("Hosting:AllowedOrigins must contain exact HTTPS origins without paths or trailing slashes.");
        if(environment.IsDevelopment())return origins;
        var required=new[]{"Firebase:ProjectId","Firebase:ApiKey","Persistence:ProjectId","Wakeups:ProjectId","Wakeups:Location","Wakeups:Queue","Wakeups:CallbackUrl","Wakeups:Audience","Wakeups:ServiceAccountEmail","Wakeups:ServiceAccountSubject","Wakeups:SweepServiceAccountEmail","Wakeups:SweepServiceAccountSubject"};
        foreach(var key in required)if(string.IsNullOrWhiteSpace(configuration[key]))throw new InvalidOperationException($"Production requires {key}.");
        if(origins.Length==0)throw new InvalidOperationException("Production requires Hosting:AllowedOrigins.");
        if(configuration["Firebase:ProjectId"]!.StartsWith("demo-",StringComparison.Ordinal)||configuration["Firebase:ApiKey"]=="demo-local-key")throw new InvalidOperationException("Production requires real Firebase configuration.");
        foreach(var key in new[]{"Persistence:EmulatorHost","Firebase:EmulatorUrl"})if(!string.IsNullOrWhiteSpace(configuration[key]))throw new InvalidOperationException($"Production forbids {key}.");
        foreach(var key in new[]{"FIRESTORE_EMULATOR_HOST","FIREBASE_AUTH_EMULATOR_HOST"})if(!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))throw new InvalidOperationException($"Production forbids {key}.");
        if(configuration["Wakeups:Mode"] is {Length:>0} mode&&mode!="cloud")throw new InvalidOperationException("Production requires Cloud Tasks wakeups.");
        if(!Uri.TryCreate(configuration["Wakeups:CallbackUrl"],UriKind.Absolute,out var callback)||callback.Scheme!="https"||callback.AbsolutePath!="/internal/wakeups"||callback.Query.Length>0||callback.Fragment.Length>0)
            throw new InvalidOperationException("Wakeups:CallbackUrl must be the HTTPS /internal/wakeups endpoint.");
        if(configuration["Wakeups:Audience"]!=callback.GetLeftPart(UriPartial.Authority))throw new InvalidOperationException("Wakeups:Audience must equal the direct API HTTPS origin.");
        if(configuration["Wakeups:ServiceAccountEmail"]==configuration["Wakeups:SweepServiceAccountEmail"]||configuration["Wakeups:ServiceAccountSubject"]==configuration["Wakeups:SweepServiceAccountSubject"])
            throw new InvalidOperationException("Task delivery and independent sweep require distinct service identities.");
        foreach(var key in new[]{"Wakeups:ServiceAccountSubject","Wakeups:SweepServiceAccountSubject"})
            if(configuration[key]!.Any(c=>!char.IsAsciiDigit(c)))throw new InvalidOperationException($"{key} must be the numeric service-account uniqueId.");
        foreach(var key in new[]{"Wakeups:ServiceAccountEmail","Wakeups:SweepServiceAccountEmail"})
            if(!configuration[key]!.EndsWith(".iam.gserviceaccount.com",StringComparison.Ordinal)||configuration[key]!.Contains(' '))throw new InvalidOperationException($"{key} must be a Google service-account email.");
        // Constructing the adapter validates resource IDs without performing network access or obtaining ADC.
        using var validated=new CloudTasksWakeups(configuration);
        return origins;
    }
}
