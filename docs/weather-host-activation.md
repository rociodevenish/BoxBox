# Versioned host weather activation

The API defaults to `DryRacePresets`. To opt a host into complete weekend weather, set both environment variables before starting the real API:

```powershell
$env:Weather__Policy = 'weather-weekend-presets-v1'
$env:Weather__Profile = 'rain-arrival'
dotnet run --project src/backend/Game.Api
```

The supported profile values are `all-dry`, `steady-wet`, `rain-arrival`, `drying`, and `changeable`. Unknown policy versions, unsupported profiles, a missing profile, or a profile without its policy fail during startup. Remove both variables to restore dry creation defaults.

Activation keeps the existing `open-equal`, `restricted-equal`, `open-mixed`, and `restricted-mixed` public preset names. The host selects the profile; players cannot select or supply future weather. Each enabled configuration includes Weather, WetTyres, and WeatherReactions. All-dry still enables those capabilities with dry initial conditions and no rain.

Preparation applies the profile to the selected circuit's actual effective weekend. Its authored weather volatility and provenance are preserved in the frozen configuration under `weather-weekend-presets-v1/<profile>/weather-track-...`. Setup changes remap the selected circuit, and the qualifying lock freezes the resulting inputs for subsequent qualifying and race replay. Legacy direct session creation uses Monza weather inputs as its explicit default weather context while retaining the existing segmented race track and field.

Profiles are authored test hypotheses, not forecasts of real circuit weather. Initial wetness, rain mean, wetting/drying rates and interval timing differ by profile; rain-arrival and changeable use the selected track's authored volatility, steady-wet scales it, and all-dry/drying set it to zero. See [circuit weather](circuit-weather.md), [wet tyres](wet-tyres.md), and [weather reactions](weather-reactions.md) for the underlying public behavior.

Focused validation: `dotnet test tests/Game.Api.Tests --filter FullyQualifiedName~WeatherActivationTests` exercises the real host registration, all profiles and public names, dry defaults, and invalid startup configuration. It does not replace complete-weekend comparative evidence.
