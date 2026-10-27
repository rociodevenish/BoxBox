using Game.Application;
using Xunit;
namespace Game.Api.Tests;
public sealed class LiveDisruptionPolicyTests
{
    [Fact]
    public void Explicit_live_policy_enables_only_versioned_authored_routes()
    {
        var policy=new AuthoredDisruptionPolicy();
        Assert.NotNull(policy.ForCircuit("track-monza"));
        Assert.NotNull(policy.ForCircuit("track-monaco"));
        Assert.Null(policy.ForCircuit("track-silverstone"));
        Assert.All(new[]{"track-monza","track-monaco"},id=>{
            var enabled=policy.ForCircuit(id)!;
            Assert.True(enabled.Route.LaneTransitSeconds>0);
            Assert.Equal(AuthoredDisruptionPolicy.ContentVersion,enabled.SourceVersion);
            Assert.True(enabled.Incidents.InterruptMajorIncidents);
            Assert.Equal("local-incidents-v1",enabled.Incidents.Version);
        });
    }
}
