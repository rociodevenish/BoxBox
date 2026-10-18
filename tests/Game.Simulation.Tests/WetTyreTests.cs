using Game.Domain;
using Game.Simulation;
using Xunit;
namespace Game.Simulation.Tests;

public class WetTyreTests
{
    private static RaceConfiguration Config(double wetness) => new(3, new("Wet tyre lab",100),
        StopVariation:0, Traffic:new(Enabled:false),
        Weather:new(MeanRain:0,Volatility:0,InitialWetness:wetness,WettingPerSecond:0,DryingPerSecond:0),
        WetTyres:new());
    private static PhaseResult Run(double wetness, TyreCompound compound) =>
        new RaceEngine(Config(wetness),[new("a","A",StartingCompound:compound)],17).ResolvePhase();

    [Fact]
    public void Dry_intermediate_and_wet_each_have_a_condition_range_where_they_are_faster()
    {
        Assert.True(Run(0,TyreCompound.Medium).Time < Run(0,TyreCompound.Intermediate).Time);
        Assert.True(Run(.45,TyreCompound.Intermediate).Time < Run(.45,TyreCompound.Medium).Time);
        Assert.True(Run(.45,TyreCompound.Intermediate).Time < Run(.45,TyreCompound.Wet).Time);
        Assert.True(Run(1,TyreCompound.Wet).Time < Run(1,TyreCompound.Intermediate).Time);
    }

    [Fact]
    public void Wrong_conditions_accelerate_wear_and_wet_use_is_an_actual_race_exemption()
    {
        Assert.True(Run(0,TyreCompound.Wet).Tyres[0].Condition < Run(1,TyreCompound.Wet).Tyres[0].Condition);
        var race = new RaceEngine(Config(1),[new("a","A",StartingCompound:TyreCompound.Wet)],17);
        Assert.False(race.GetState().Eligibility["a"]);
        Assert.True(race.ResolvePhase().Eligibility["a"]);
        Assert.False(Run(1,TyreCompound.Medium).Eligibility["a"]);
    }

    [Fact]
    public void Mid_segment_drying_and_existing_error_channel_are_independent_of_equivalent_subdivision()
    {
        RaceEngine Race(bool split) => new(Config(.9) with {
            Laps=2, Weather=new(MeanRain:0,Volatility:0,InitialWetness:.9,DryingPerSecond:.003,TimeOriginSeconds:23.25),
            Incidents=new(ErrorRate:.00001,FailureRate:0,ComponentWearPerSecond:0),
            Track=new("Lab",100,Segments:split?[new("first",500,50),new("second",500,50)]:[new("whole",1000,100)])
        },[new("a","A",StartingCompound:TyreCompound.Intermediate,WetWeather:new(30,40,50))],17);
        var a=Race(false);var b=Race(true);
        var first=a.ResolvePhase();var second=b.ResolvePhase();
        Assert.Equal(first.Time,second.Time,7);
        Assert.Equal(first.Tyres[0].Condition,second.Tyres[0].Condition,9);
        Assert.Equal(a.GetDesignerIncidentDiagnostics()[0].ErrorExposure,b.GetDesignerIncidentDiagnostics()[0].ErrorExposure,9);
        Assert.DoesNotContain(a.GetDesignerDiagnostics(),d=>d.Channel.Contains("weather",StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Wet_attributes_use_supplied_detailed_ratings_and_effective_setup_with_provenance()
    {
        var weekend=WeekendPreparation.Prepare(WeekendPresetCatalog.Open("track-monza"),3,new(Mechanical:1,Tyres:1));
        var car=weekend.Cars[0];
        var wet=WeekendPreparation.EffectiveWetWeather(car.Source,weekend.Setup);
        Assert.Equal(car.Source.Driver.Ratings["wetWeatherSkill"],wet.WetWeatherSkill);
        Assert.Equal(car.Source.Car.Ratings["tyreTemperature"],wet.TyreTemperatureManagement);
        Assert.Equal(car.Source.Car.Ratings["traction"]+3,wet.Traction);
        Assert.Contains(car.Source.Driver.Version,wet.SourceVersion);
        Assert.Contains(car.Source.Car.Version,wet.SourceVersion);
    }

    [Fact]
    public void Wet_inputs_are_bounded_versioned_and_legacy_null_metadata_is_unchanged()
    {
        var dry=new RaceConfiguration(3,new("Lab"));
        var driver=new Driver("a","A");
        Assert.DoesNotContain("Wet",System.Text.Json.JsonSerializer.Serialize(dry));
        Assert.DoesNotContain("Wet",System.Text.Json.JsonSerializer.Serialize(driver));
        Assert.Equal(0,(int)TyreCompound.Soft);Assert.Equal(2,(int)TyreCompound.Hard);
        Assert.Contains("wet-tyres-v1+actual-race-wet-use-v1",SimulationReplay.Model(Config(0)));
        Assert.Throws<ArgumentException>(()=>new RaceEngine(Config(0) with {WetTyres=new(WrongConditionWear:double.NaN)},[driver],17));
        Assert.Throws<ArgumentException>(()=>new RaceEngine(Config(0),[driver with {WetWeather=new(WetWeatherSkill:101)}],17));
        Assert.Throws<ArgumentException>(()=>new RaceEngine(Config(0) with {WetTyres=new(ModelVersion:"unknown")},[driver],17));
        foreach(var wetness in new[]{0d,.001,.45,.999,1d})
            foreach(var compound in Enum.GetValues<TyreCompound>())
            {
                var result=Run(wetness,compound);
                Assert.InRange(result.Time,1,1000);
                Assert.InRange(result.Tyres[0].Condition,0,1);
            }
    }

    [Fact]
    public void A_queued_or_fitted_but_not_raced_wet_set_grants_no_exemption_and_service_stays_fixed()
    {
        var race=new RaceEngine(Config(1) with {NormalPhaseLaps=1},[new("a","A")],17);
        var fitted=race.ResolvePhase(new Dictionary<string,DriverDecision>{{"a",new(PitCompound:TyreCompound.Wet)}});
        Assert.True(fitted.Tyres[0].InPits);
        Assert.False(fitted.Eligibility["a"]);
        Assert.Equal(3,fitted.Events.Single(e=>e.Kind==RaceEventKind.PitService).Value);
        Assert.Throws<ArgumentException>(()=>race.ResolvePhase(new Dictionary<string,DriverDecision>{{"a",new(PitCompound:TyreCompound.Intermediate)}}));
        var used=race.ResolvePhase();
        Assert.True(used.Eligibility["a"]);
        Assert.Single(used.Events,e=>e.Kind==RaceEventKind.PitService);
        var queued=new RaceEngine(Config(1) with {NormalPhaseLaps=.5},[new("a","A")],17)
            .ResolvePhase(new Dictionary<string,DriverDecision>{{"a",new(PitCompound:TyreCompound.Wet)}});
        Assert.False(queued.Eligibility["a"]);
        Assert.DoesNotContain(queued.Events,e=>e.Kind==RaceEventKind.PitService);
    }

    [Fact]
    public void Wet_skill_changes_existing_integrated_error_exposure_and_incident_losses_survive_condition_cells()
    {
        RaceEngine Race(double skill,IncidentScenario? scenario=null)
        {
            var config=Config(1) with {Laps=1,Incidents=new(ErrorRate:.00001,FailureRate:0,ComponentWearPerSecond:0)};
            Driver[] drivers=[new("a","A",StartingCompound:TyreCompound.Wet,WetWeather:new(skill,82,skill))];
            return scenario is null?new(config,drivers,17):new(config,drivers,17,scenario);
        }
        var skilled=Race(100);var novice=Race(1);
        var clean=skilled.ResolvePhase();novice.ResolvePhase();
        Assert.True(novice.GetDesignerIncidentDiagnostics()[0].ErrorExposure>skilled.GetDesignerIncidentDiagnostics()[0].ErrorExposure);
        var incident=Race(100,new([new("a",IncidentKind.LockUp,20)])).ResolvePhase();
        Assert.True(incident.Time>clean.Time+1.9);
        Assert.Single(incident.Events,e=>e.Kind==RaceEventKind.LockUp);
    }

    [Fact]
    public void Higher_effective_wet_skill_and_temperature_management_improve_only_their_supported_effects()
    {
        PhaseResult With(WetWeatherAttributes attributes)=>new RaceEngine(Config(.8),
            [new("a","A",StartingCompound:TyreCompound.Intermediate,WetWeather:attributes)],17).ResolvePhase();
        Assert.True(With(new(100,82,82)).Time<With(new(1,82,82)).Time);
        var managed=With(new(82,100,82));var unmanaged=With(new(82,1,82));
        Assert.True(managed.Time<unmanaged.Time);
        Assert.True(managed.Tyres[0].Condition>unmanaged.Tyres[0].Condition);
    }
}
