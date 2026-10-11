using Game.Application;
using Google.Apis.Auth;
namespace Game.Api;

// Signature validation is an external identity-provider boundary; tests supply issued claims only here.
public interface IGoogleWakeupTokens
{
    Task<GoogleJsonWebSignature.Payload> Validate(string token,string audience);
}
public sealed class GoogleWakeupTokens:IGoogleWakeupTokens
{
    public Task<GoogleJsonWebSignature.Payload> Validate(string token,string audience)=>GoogleJsonWebSignature.ValidateAsync(token,
        new GoogleJsonWebSignature.ValidationSettings{Audience=[audience]});
}
public sealed class WakeupIdentity(IConfiguration config,IGoogleWakeupTokens tokens,TimeProvider clock)
{
    public async Task Verify(HttpRequest request,bool sweep=false)
    {
        var audience=config["Wakeups:Audience"];
        var prefix=sweep?"Wakeups:Sweep":"Wakeups:";
        var email=config[prefix+"ServiceAccountEmail"];
        var subject=config[prefix+"ServiceAccountSubject"];
        var authorization=request.Headers.Authorization.FirstOrDefault();
        if(string.IsNullOrWhiteSpace(audience)||string.IsNullOrWhiteSpace(email)||string.IsNullOrWhiteSpace(subject)
            ||authorization is null||!authorization.StartsWith("Bearer ",StringComparison.Ordinal))Deny();
        try
        {
            var payload=await tokens.Validate(authorization![7..],audience!);
            if(payload.Issuer is not ("https://accounts.google.com" or "accounts.google.com")||!payload.AudienceAsList.Contains(audience!)
                ||payload.Subject!=subject||payload.Email!=email||!payload.EmailVerified
                ||payload.ExpirationTimeSeconds is not {} expiry||expiry<=clock.GetUtcNow().ToUnixTimeSeconds())Deny();
        }
        catch(Exception error) when(error is InvalidJwtException or ArgumentException){Deny();}
    }
    private static void Deny()=>throw new SessionFault(SessionFailure.Forbidden,"Scheduled delivery requires its configured Google service identity and audience.");
}
public static class WakeupEndpoints
{
    public static void MapWakeupEndpoints(this WebApplication app)
    {
        app.MapPost("/internal/wakeups",async(WakeupInput input,HttpRequest request,WakeupIdentity identity,SessionCoordinator sessions)=>{
            await identity.Verify(request);var state=await sessions.DeliverWakeup(input);
            if(state.Phase!=input.Phase||state.Paused)app.Logger.LogInformation("Scheduled progression session {Session} generation {Generation} phase {Phase} paused {Paused}",input.SessionId,input.Generation,state.Phase,state.Paused);
            return state;
        });
        app.MapPost("/internal/wakeups/sweep",async(HttpRequest request,WakeupIdentity identity,SessionCoordinator sessions)=>{
            await identity.Verify(request,true);var advanced=await sessions.SweepWakeups();
            if(advanced>0)app.Logger.LogInformation("Independent wakeup repair advanced {Count} scheduled sessions",advanced);
            return Results.Ok(new{advanced});
        });
    }
}
