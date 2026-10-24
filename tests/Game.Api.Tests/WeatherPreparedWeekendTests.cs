using Game.Application;
using Game.Simulation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace Game.Api.Tests;
public sealed class WeatherPreparedWeekendTests
{
    [Theory]
    [InlineData("track-monza","all-dry")]
    [InlineData("track-monaco","steady-wet")]
    [InlineData("track-monaco","rain-arrival")]
    [InlineData("track-monza","drying")]
    [InlineData("track-monaco","changeable")]
    public void Host_selected_profile_freezes_actual_track_for_every_team_and_common_qualifying_clock(string track,string profile)
    {
        using var host=new WebApplicationFactory<Program>().WithWebHostBuilder(b=>b.UseSetting("Weather:Policy",WeatherWeekendProfiles.Version).UseSetting("Weather:Profile",profile).UseSetting("Wakeups:Mode","manual"));
        using var client=host.CreateClient();var sessions=host.Services.GetRequiredService<SessionCoordinator>();var identity=new AuthIdentity("weather-host",false);
        var joined=sessions.CreateLobby(identity,new("open-mixed",10,101,"Competitive","Host"));var id=joined.Lobby.SessionId;var device=joined.Membership.DeviceId;
        sessions.ClaimTeam(id,identity,new("team-01",device));sessions.BeginPreparation(id,identity,new(track));
        var own=sessions.HouseholdPreparation(id,identity,device,"team-01");
        sessions.SaveSetup(id,identity,device,"team-01",new(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision,new(1,-1,0)));
        own=sessions.HouseholdPreparation(id,identity,device,"team-01");
        sessions.ConfirmPractice(id,identity,device,"team-01",new(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision));
        own=sessions.HouseholdPreparation(id,identity,device,"team-01");
        sessions.ConfirmPreparation(id,identity,device,"team-01",new(Guid.NewGuid().ToString(),own.Revision,own.AssignmentRevision));
        sessions.StartQualifying(id,identity,new(Guid.NewGuid().ToString(),sessions.PreparationPublic(id).Revision));
        var frozen=sessions.FrozenPreparation(id,identity);Assert.Equal(11,frozen.Teams.Count);
        foreach(var team in frozen.Teams)
        {
            var configuration=team.Effective.Configuration;Assert.Equal(track,team.Effective.Source.Circuit.Id);
            Assert.Equal(WeatherWeekendProfiles.Apply(team.Effective,profile),configuration.Weather);
            Assert.NotNull(configuration.WetTyres);Assert.NotNull(configuration.WeatherReactions);
            Assert.Equal(0,configuration.Weather!.TimeOriginSeconds);
            Assert.Equal(new[]{90d,270d,450d},new[]{"Q1","Q2","Q3"}.Select(q=>Qualifying.SessionConditions(configuration,101,q).Observed.Time));
        }
        Assert.Equal(90,sessions.QualifyingPublic(id).Weather!.Time);
    }
}
