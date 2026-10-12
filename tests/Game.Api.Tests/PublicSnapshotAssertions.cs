using System.Text.Json.Nodes;
using Xunit;
namespace Game.Api.Tests;
internal static class PublicSnapshotAssertions
{
    internal static void EqualState(string expected,string actual)=>Assert.Equal(State(expected),State(actual));
    private static string State(string json)
    {
        var snapshot=JsonNode.Parse(json)!.AsObject();
        // Server time advances on reads; every persistent/public state field must remain equal.
        snapshot.Remove("serverTimeUtc");return snapshot.ToJsonString();
    }
}
