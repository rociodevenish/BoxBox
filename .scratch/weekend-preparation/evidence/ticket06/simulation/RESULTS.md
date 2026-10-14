# Full-field setup results

Negative delta means faster than the neutral target team; all20 other cars and the plan/grid stay fixed. Population SD across12 paired seeds, not a confidence interval. Clear and traffic controls each use one deterministic seed.

| Circuit / field | Variant | Clear zero delta(s) | Traffic zero delta(s) | Noisy mean(s) | SD(s) | Range(s) | Faster/tied/slower |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| track-monza / equal | speed | -5.145 | -5.145 | -5.145 | 0.000 | -5.145 to -5.145 | 12/0/0 |
| track-monza / equal | downforce | 5.367 | 5.145 | 5.195 | 0.098 | 5.145 to 5.445 | 0/0/12 |
| track-monza / equal | responsive | 0.099 | 0.099 | 0.113 | 0.042 | 0.099 to 0.251 | 0/0/12 |
| track-monza / equal | stable | 0.273 | 0.273 | 0.285 | 0.041 | 0.270 to 0.421 | 0/0/12 |
| track-monza / equal | warm-up | -0.428 | -0.428 | -0.426 | 0.002 | -0.428 to -0.421 | 12/0/0 |
| track-monza / equal | preservation | 0.430 | 0.430 | 0.440 | 0.041 | 0.425 to 0.576 | 0/0/12 |
| track-monza / mixed | speed | -5.146 | -5.145 | -5.145 | 0.000 | -5.145 to -5.145 | 12/0/0 |
| track-monza / mixed | downforce | 5.369 | 5.145 | 5.189 | 0.157 | 5.006 to 5.681 | 0/0/12 |
| track-monza / mixed | responsive | 0.286 | 0.286 | 0.250 | 0.130 | -0.049 to 0.532 | 1/0/11 |
| track-monza / mixed | stable | 0.087 | 0.087 | 0.156 | 0.068 | 0.087 to 0.271 | 0/0/12 |
| track-monza / mixed | warm-up | -0.428 | -0.428 | -0.439 | 0.041 | -0.575 to -0.423 | 12/0/0 |
| track-monza / mixed | preservation | 0.430 | 0.430 | 0.428 | 0.002 | 0.425 to 0.430 | 0/0/12 |
| track-monaco / equal | speed | 0.230 | 0.230 | 0.244 | 0.041 | 0.230 to 0.380 | 0/0/12 |
| track-monaco / equal | downforce | -0.230 | -0.230 | -0.218 | 0.041 | -0.230 to -0.080 | 12/0/0 |
| track-monaco / equal | responsive | 0.027 | 0.027 | 0.028 | 0.001 | 0.027 to 0.029 | 0/0/12 |
| track-monaco / equal | stable | 0.346 | 0.346 | 0.345 | 0.001 | 0.343 to 0.346 | 0/0/12 |
| track-monaco / equal | warm-up | -0.426 | -0.426 | -0.424 | 0.002 | -0.426 to -0.419 | 12/0/0 |
| track-monaco / equal | preservation | 0.428 | 0.428 | 0.438 | 0.041 | 0.422 to 0.574 | 0/0/12 |
| track-monaco / mixed | speed | 0.230 | 0.230 | 0.234 | 0.010 | 0.230 to 0.267 | 0/0/12 |
| track-monaco / mixed | downforce | -0.230 | -0.230 | -0.243 | 0.041 | -0.380 to -0.230 | 12/0/0 |
| track-monaco / mixed | responsive | 0.213 | 0.213 | 0.190 | 0.105 | 0.029 to 0.460 | 0/0/12 |
| track-monaco / mixed | stable | 0.159 | 0.159 | 0.229 | 0.068 | 0.159 to 0.344 | 0/0/12 |
| track-monaco / mixed | warm-up | -0.426 | -0.426 | -0.437 | 0.041 | -0.572 to -0.421 | 12/0/0 |
| track-monaco / mixed | preservation | 0.428 | 0.428 | 0.426 | 0.002 | 0.422 to 0.428 | 0/0/12 |

## Fixed-compound stint probes

No pit stops, no traffic/noise; all22 cars retained. These laboratory stints are not eligible two-compound race plans.

| Circuit | Laps | Setup | First-car finish(s) | Final condition |
| --- | ---: | --- | ---: | ---: |
| track-monza | 1 | warm-up | 89.379 | 0.9753 |
| track-monza | 1 | neutral | 89.571 | 0.9756 |
| track-monza | 1 | preservation | 89.763 | 0.9758 |
| track-monza | 20 | warm-up | 1791.805 | 0.5064 |
| track-monza | 20 | neutral | 1791.716 | 0.5110 |
| track-monza | 20 | preservation | 1791.634 | 0.5156 |
| track-monaco | 1 | warm-up | 89.379 | 0.9753 |
| track-monaco | 1 | neutral | 89.571 | 0.9756 |
| track-monaco | 1 | preservation | 89.763 | 0.9758 |
| track-monaco | 20 | warm-up | 1791.817 | 0.5062 |
| track-monaco | 20 | neutral | 1791.716 | 0.5110 |
| track-monaco | 20 | preservation | 1791.623 | 0.5157 |

## First lap after the planned Hard change

Measured public LapCompleted time minus preceding PitExit time (excludes pit loss); includes first Hard warm-up and ordinary wear.

| Circuit | Setup | First post-pit lap(s) | Finish(s) | Final condition |
| --- | --- | ---: | ---: | ---: |
| track-monza | neutral | 90.752 | 1811.189 | 0.8460 |
| track-monza | warm-up | 90.464 | 1810.764 | 0.8445 |
| track-monza | preservation | 91.040 | 1811.616 | 0.8474 |
| track-monaco | neutral | 90.752 | 1811.189 | 0.8460 |
| track-monaco | warm-up | 90.464 | 1810.767 | 0.8444 |
| track-monaco | preservation | 91.040 | 1811.613 | 0.8475 |

1 noisy comparison cases have a seed sign opposite to their clear control or mixed signs. 0 noisy paired cases have effectively zero delta variation; 4 have SD below0.001s (the table rounds to3 decimals). Raw values and all such cases are retained in analysis.json. 72 full-field saved replays reproduce identical outcomes.
