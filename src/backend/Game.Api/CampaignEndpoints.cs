using Game.Application;
namespace Game.Api;

public static class CampaignEndpoints
{
    public static void MapCampaignEndpoints(this WebApplication app)
    {
        app.MapPost("/api/campaigns/{id}/next/prepare",async(string id,HttpRequest request,FirebaseIdentity auth,CampaignCoordinator campaigns)=>campaigns.PrepareNext(id,await auth.Verify(LobbyEndpoints.Token(request))));
        app.MapPost("/api/campaigns/{id}/next/start",async(string id,CampaignStartInput input,HttpRequest request,FirebaseIdentity auth,CampaignCoordinator campaigns)=>campaigns.StartNext(id,await auth.Verify(LobbyEndpoints.Token(request)),input));
        app.MapPost("/api/campaigns/{id}/return",async(string id,CampaignReturnInput input,HttpRequest request,FirebaseIdentity auth,CampaignCoordinator campaigns)=>campaigns.Return(id,await auth.Verify(LobbyEndpoints.Token(request)),input));
        app.MapPost("/api/campaigns",async(CreateCampaignInput input,HttpRequest request,FirebaseIdentity auth,CampaignCoordinator campaigns)=>campaigns.Create(await auth.Verify(LobbyEndpoints.Token(request)),input));
        app.MapGet("/api/campaigns/{id}",async(string id,string deviceId,HttpRequest request,FirebaseIdentity auth,CampaignCoordinator campaigns)=>campaigns.Snapshot(id,await auth.Verify(LobbyEndpoints.Token(request)),deviceId));
        app.MapPost("/api/campaigns/{id}/commit-result",async(string id,string deviceId,HttpRequest request,FirebaseIdentity auth,CampaignCoordinator campaigns)=>campaigns.CommitResult(id,await auth.Verify(LobbyEndpoints.Token(request)),deviceId));
        app.MapPost("/api/campaigns/{id}/prepare",async(string id,HttpRequest request,FirebaseIdentity auth,CampaignCoordinator campaigns)=>campaigns.Prepare(id,await auth.Verify(LobbyEndpoints.Token(request))));
        app.MapPost("/api/campaigns/{id}/start",async(string id,CampaignStartInput input,HttpRequest request,FirebaseIdentity auth,CampaignCoordinator campaigns)=>campaigns.Start(id,await auth.Verify(LobbyEndpoints.Token(request)),input));
    }
}
