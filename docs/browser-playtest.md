# Browser race playtest and designer replay

Start with [the local run procedure](browser-dry-race.md). One human controls both cars against ten scripted teams. Use 6 laps for a quick clarity/recovery check, then 50 laps for sustained decisions. On 2026-10-09 the user retired the fixed duration target: timing informs a pleasant experience without a required duration.

## Repeatable player check

1. Choose a preset. Equal fields have identical driver/car attributes; mixed fields vary car fit and small driver Pace offsets.
2. Open the private controller and public display separately. Read both cars' defaults, estimates, legal battles and the two-compound rule. Change their instructions independently, review both cars and submit.
3. Follow overtake/pit feedback and compare Gap (to leader) with Interval (to the car ahead). Inspect compound, age and stop status. Skip/replay and reduced motion must reconcile to the same recorded Classification.
4. The next decision window opens automatically after running the reviewed team plan. Change compound at least once per car when testing eligibility. A stop executes at that car's next lap-end; queued/ongoing stops cannot be replaced. Choose Attack/Defend only when offered.
5. Refresh an accepted plan and interrupt/reconnect the connection. Confirm the newer window restores its defaults, or final instructions remain confirmed, and no time advances twice. Inspect final distance, lap deficits and eligibility, then restart or return to setup.
6. Record confusion, both-car workload, useful event explanations and unpleasant waiting/interaction. Separate these findings from automated times and server benchmarks.

## Separate devices on a trusted local network

Keep the API on `http://127.0.0.1:4510`. Start Angular with `npm run start:race -- --host 0.0.0.0`, then open `http://<host-LAN-IP>:4410/race/new` on the controller device. Angular proxies `/api` and `/hubs` to the local API; no second exposed API port is needed. Permit the Angular development-server connection in the host's local network settings if needed. Devices must reach the same host and port. Correlation IDs use `crypto.getRandomValues`, supporting HTTP LAN pages without the secure-context-only `randomUUID` convenience API.

Share the public URL with the viewing device. Share the controller URL only with the intended controller: its fragment contains the private grant. A browser already given that grant retains the controller link locally and offers a return link from the public board; a fresh viewer gets only Race setup. Another device needs the original private link. Local storage does not isolate separate users sharing one browser profile. Server restart expires the session; no durable recovery is promised.

## Designer diagnostics

Set a separate key in the terminal that starts the API:

```powershell
$env:Designer__Key = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
dotnet run --project src/backend/Game.Api --urls http://127.0.0.1:4510
```

The key is independent of controller grants and never enters player responses. Without it the resource is disabled; missing/invalid keys and non-loopback requests reject. Export locally using that key and the session ID from the URL:

```powershell
$sessionId = '<session-ID>'
$headers = @{ 'X-Designer-Key' = $env:Designer__Key }
New-Item -ItemType Directory -Force -Path .tools/reports/browser-race | Out-Null
Invoke-RestMethod "http://127.0.0.1:4510/api/designer/sessions/$sessionId/record" -Headers $headers |
  ConvertTo-Json -Depth 100 | Set-Content .tools/reports/browser-race/designer-replay.json
dotnet run --project tools/SimulationRunner -- --replay .tools/reports/browser-race/designer-replay.json --output .tools/reports/browser-race/replayed
```

`browser-dry-race-replay-v1` retains explicit layout/tuning, driver inputs, seed, engine/randomness versions, frozen opponent script version and every accepted complete phase plan. Verification runs recorded decisions through the existing engine and hashes its full ordered physical phase results, distances, tyres and eligibility. Partial records replay only accepted phases. Incompatible versions or incomplete inputs reject before output. Timings and opaque player-event IDs are not physical inputs. The script version identifies how choices were obtained; replay uses accepted choices rather than regenerating scripts.

Measurements distinguish server window opening to accepted lock (includes viewing/network/automation wait), engine resolution only, and initialization to finish. Presentation/page elapsed durations are untrusted client-reported monotonic samples from a write-only resource. They never authorize commands or change physics. Ready-wait is not applicable to automatic windows and is null in new records. Historical explicit-Continue measurements retain their original meaning. Presentation overlaps next-window decision time; do not add these as disjoint components. Total-to-finish excludes final presentation. Measurements establish neither human pacing nor server capacity.

## Reproduce evidence

Run `npm run test:browser` in `src/frontend` with the documented Chromium cache. Tests use ports 4410/4510 and an ephemeral test-only designer key. Five 50-lap cases cover four presets plus repeated legal stops causing a lapped finish. Generated replay/summary files stay in ignored `.tools/reports/browser-race/full-distance`. From repository root run `node tools/summarize-browser-playtest.mjs` to regenerate review-automatic-windows.json. The earlier 05-playtest.json remains historical explicit-Continue evidence. Actual feedback and limitations appear in [ticket 05 evidence](../.scratch/browser-dry-race/evidence/05-playtest.md).
