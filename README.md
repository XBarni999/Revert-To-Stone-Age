# Revert To Stone Age

A BepInEx mod for **Nuclear Option** that changes air-defense acquisition, surface radar coverage, laser endurance, and missile reliability. The default `Realistic` preset uses moderate penalties; every faction is affected, including AI-controlled aircraft, ground SAMs, and ships.

**Current version:** 1.8.0. The first public release was 1.7.0. Built against Nuclear Option **0.34.1**. Download the DLL or complete installation ZIP from [Releases](https://github.com/XBarni999/Revert-To-Stone-Age/releases/latest).

## What the mod changes

### Surface radar upper blind zone

Ground and ship radars have a configurable maximum elevation of **70 degrees above the horizontal** by default. Targets above that angle are outside local radar coverage, leaving a cone with a **20-degree half-angle around the zenith**.

- The angle is calculated from the actual antenna scan point, rather than the vehicle center.
- Coverage rejection happens before scheduling a terrain raycast and again during the final radar-return evaluation.
- The limit applies to all target types, including ballistic missiles approaching steeply from above.
- Aircraft and missile radars retain their native antenna coverage.
- Set `MaximumElevationDegrees = 90` or disable section 8 to remove this added limit.
- Other radars may still see the same target from another position. Existing shared tracks, visual detection and datalink are preserved. Entering the cone does not instantly erase a previously known target.

The 70-degree limit is a configurable gameplay assumption, not a measured specification for every military radar. An overhead blind region can complicate local tracking, but it is not a universal explanation for the difficulty of ballistic-missile interception.

### Radar clutter

The mod scales the game's existing `clutterFactor` instead of replacing radar-return evaluation. Signal strength, jamming and radar warning events remain native.

In the default preset:

- Extra clutter multiplier: **1.8**.
- Low-height attenuation proxy: **0.6** at sea level, fading to **1.0** at **120 m**.
- Downward-view attenuation proxy: up to **0.7**, applied gradually beyond the configured **3-degree** downward threshold.

These values scale clutter input; they are not direct percentage reductions in radar range or guaranteed detection probabilities. Height uses `Datum.LocalSeaY` to account for the floating origin. It is height above sea level, not local terrain clearance; native terrain checks remain responsible for terrain masking.

An optional additional radar-horizon check uses `4120 * (sqrt(radar height) + sqrt(target height))` meters. The game already performs its own Earth-curvature check during normal radar requests, which remains active. This additional filter does not extend the native horizon or simulate atmospheric ducting.

### Turret acquisition and settling

Turrets receive an acquisition delay after switching targets and a multiplier on native lock time. Optional settling time increases with the target's relative angular motion: transverse relative velocity divided by range, including the observer's velocity.

Reaction and settling delays run in parallel; native lock-time accumulation follows. Settling accumulates only while native aiming is aligned and relative angular motion is within the tracking limit. It resets on native aim loss or excessive angular motion, including during an established engagement. At the limit, settling accumulates at half speed. The default angular limit is **18 degrees per second**. This affects turret firing readiness; missile seeker lock is untouched.

Automatic turret traverse and elevation rates are scaled to **62.5%** of native values in Realistic. Native aiming and firing checks remain in control; manual turret control retains native slew rates. The original rate fields are restored after each aim call, including exceptions, so repeated calls do not compound the reduction. The mod does not grant high-speed missiles a price-based or Mach-based interception exemption.

### Radar FireControl launch timing

Some radar SAMs launch queued salvos through `FireControl` without passing through `Turret.AimTurret`. Version 1.8.0 also covers `PlanSalvo` and `LaunchSalvo`, keeping their native queues, ammo accounting and target selection intact.

Realistic uses **2.5x planning time**, with a **1.2-second minimum per queued shot**, and **2x salvo intervals**, with a **0.8-second minimum between launches**. Floors cover zero native timings. These controls share the section 2 Enabled toggle. Changes refresh registered controllers immediately; a wait already scheduled by the game's async state machine finishes at its original duration.

### Laser thermal budget

Lasers have a limited firing budget, cool during pauses, and stop accepting fire commands for a cooldown interval after overheating.

- Default firing budget: **4 seconds**.
- Default overheat cooldown: **5 seconds**.
- Damage output decreases gradually from **100% to 55%** as heat accumulates.
- Native electrical consumption remains unchanged.
- Damage fields are restored after each simulation tick, including exceptions.
- The mod does not change the safety switch or beam scale.

### Six missile malfunctions

A malfunction is rolled once per launch on the authoritative simulation. The same hook is reached by player and AI launchers; it does not filter by player ownership. A failed launch still consumes ammunition.

| Malfunction | Share of malfunction events | Effect |
|---|---:|---|
| Ignition failure | 10% | Motor thrust remains zero. |
| Delayed ignition | 15% | Motor logic is held for an additional 0.6 seconds after ejection, then resumes. |
| In-flight motor cutout | 20% | Motor stops after a delayed onset. |
| Reduced thrust | 20% | Motor produces 55% of native thrust; native fuel consumption continues. |
| Actuator jam | 15% | One control input is stuck at +0.25 or -0.25; other axes retain native response. |
| Reduced control authority | 20% | Control inputs are reduced to 40% of their native values. |

The default onset window for the four in-flight malfunctions is **0.8-3 seconds** after launch. These shares divide the total failure probability; they do not add six separate failure rolls.

**Seeker lock loss is not included.** The mod does not disable the seeker, clear the missile target, or intercept native `Seek`, `SetTarget`, `SetAimpoint` or `SetProxyFuse` calls. Motor and actuator defects can cause a miss through flight behavior while retaining native targeting logic.

## Presets

| Preset | Gun / missile reaction (s) | Base settling (s) | Native lock-time multiplier | Laser firing / cooldown (s) | Missile failure chance per launch |
|---|---:|---:|---:|---:|---:|
| ModernDefense | 0.52 / 1.17 | 0.52 | 1.52 | 6.15 / 3.25 | 0.975 / 0.65 / 0.325% |
| Realistic (default) | 0.8 / 1.8 | 0.8 | 1.8 | 4 / 5 | 1.5 / 1 / 0.5% |
| WornEquipment | 1.12 / 2.52 | 1.12 | 2.12 | 2.86 / 7 | 2.1 / 1.4 / 0.7% |
| ArcadeEasy | 1.44 / 3.24 | 1.44 | 2.44 | 2.22 / 9 | 2.7 / 1.8 / 0.9% |
| Custom | Configured values | Configured value | Configured value | Configured values | Configured cost tiers |

Failure percentages are cheap / standard / high-end cost tiers.

`ArcadeEasy` makes attacking air defenses easier; it affects every side equally. `WornEquipment` represents degraded readiness through settings, not a simulated maintenance history.

Preset clutter values also vary:

| Preset | Low-height proxy | Look-down proxy | Extra clutter multiplier |
|---|---:|---:|---:|
| ModernDefense | 0.74 | 0.805 | 1.52 |
| Realistic | 0.6 | 0.7 | 1.8 |
| WornEquipment | 0.44 | 0.58 | 2.12 |
| ArcadeEasy | 0.28 | 0.46 | 2.44 |

Feature toggles and the shared settings below apply to every preset. Selecting a named preset writes all numeric preset values directly to BepInEx settings and saves them together. Displayed values and runtime values now match. Editing any numeric entry selects Custom and preserves the edit; selecting Custom keeps the currently displayed numbers. Feature toggles remain independent. Named presets are also applied on startup, so an older named configuration receives the new values.

## Installation

Requires a working BepInEx installation for Nuclear Option, including its Harmony dependency. These dependencies are not bundled.

1. Close the game.
2. Download `RevertToStoneAge-<version>.zip` and extract its `BepInEx` folder into the game directory. Alternatively, copy `RevertToStoneAge.dll` into `BepInEx/plugins`.
3. Replace older copies of this mod; keep only one installed DLL.
4. Launch the game once to generate `BepInEx/config/com.xbarni.reverttostoneage.cfg`.
5. Choose a preset in `[0. General]`, or close the game and copy a template from `Presets` over that configuration file, renaming it to `com.xbarni.reverttostoneage.cfg`.

The ZIP does not overwrite your configuration automatically. Back up custom settings before replacing them with a template.

## Configuration

Important shared controls:

| Section | Key | Default |
|---|---|---:|
| `7. Physical Limits` | `AngularTrackingLimitDegPerSecond` | 18 |
| `7. Physical Limits` | `RadarHorizonEnabled` | true |
| `7. Physical Limits` | `LaserMinimumOutput` | 0.55 |
| `8. Radar Upper Coverage` | `Enabled` | true |
| `8. Radar Upper Coverage` | `MaximumElevationDegrees` | 70 |
| `5. Munition Malfunctions` | `DelayedFailureEarliestSeconds` | 0.8 |
| `5. Munition Malfunctions` | `DelayedFailureLatestSeconds` | 3 |
| `5. Munition Malfunctions` | `FaultyIgnitionDelaySeconds` | 0.6 |
| `5. Munition Malfunctions` | `DegradedMotorThrustMultiplier` | 0.55 |
| `6. Diagnostics` | `VerboseLogging` | false |

For `Custom`, failure chance can be set by cost category: below 1,000,000; from 1,000,000 to below 10,000,000; and 10,000,000 or higher. Price is a coarse configuration proxy, not proof of real-world reliability. Realistic uses 1.5%, 1% and 0.5% respectively.

Changes made through BepInEx `ConfigEntry` objects update immediately. Editing the file externally requires a configuration reload through your BepInEx tools or a game restart.

Older development configurations may retain their custom numbers. Obsolete Mach, deadzone, optical-smudge, random-jitter and profiling settings no longer control behavior. The legacy `4. CIWS Tracking Jitter` section name is retained; its toggle now controls acquisition settling.

## Validation and limitations

Checked for this release:

- Release build against installed Nuclear Option 0.34.1: zero errors and warnings.
- Ten Harmony target methods exist; typed private-field delegates initialize against the installed assemblies.
- 118 model assertions pass for radar geometry, failure selection, motor boundaries, persistent settling, timing floors and preset ordering.
- 118 integration assertions pass against actual BepInEx ConfigFile events for all numeric preset values, Custom transitions, runtime cache synchronization and saved values.
- The native AI launch path reaches the malfunction hook: `Turret.FixedUpdate -> WeaponStation.Fire -> MissileLauncher.Fire -> Spawner.SpawnMissile -> Missile.StartMissile`. `StartMissile` sets authority from server state, not player ownership.

Not yet verified: installation of the patches inside a running Unity game, live AI/SAM malfunction behavior, mission balance, frame-time impact and multiplayer synchronization. A standalone .NET patch-installation attempt hit Unity's external-call restriction; that process cannot substitute for an in-game test. No FPS improvement or real-world equipment accuracy is claimed.

For multiplayer testing, use matching mod versions and configurations on participants. This is a test recommendation, not a multiplayer compatibility guarantee.

Suggested mission checks: compare otherwise identical raids across presets; test a target at 69 and 71 degrees with isolated radar coverage; add a second radar to check shared tracking; test short laser bursts and full cooldown. To make malfunctions easy to observe, temporarily choose `Custom`, set all three failure chances to 100%, enable verbose logging, then reselect Realistic (or another desired preset) after testing.

## Compatibility with modded weapons and air defenses

**Yes, when the added content uses the game's native components and execution paths.** Revert To Stone Age patches game component methods, not lists of vanilla weapon names, prefab names or IDs. It does not require separate registration of each added weapon.

| Added content | Coverage | Conditions |
|---|---|---|
| Missiles using native `Missile` | Launch malfunction roll, motor defects and actuator defects | Authoritative `StartMissile`, `MotorThrust` and `Steering` paths execute. |
| Automatic turrets using native `Turret` | Acquisition delay, lock-time scaling, slower slew and continuous settling | Native `AimTurret(WeaponStation)` executes; manual control retains native slew. |
| Radar-controlled launchers using native `FireControl` | Salvo planning and launch-interval penalties | Native `PlanSalvo` / `LaunchSalvo` execute and the controller has an attached radar. |
| Surface radars using native `Radar` | Clutter scaling and upper blind cone | Native radar-return evaluation runs; aircraft and missile radars retain their own upper coverage. |
| Lasers using native `Laser` | Thermal budget, cooldown and damage derating | Native fire and simulation methods execute. |
| Independently implemented projectiles, sensors or launch controllers | Not automatically guaranteed | Components that replace these native methods need a specific integration review. |

Blueprinter content that reuses the native components is covered by the same hooks. Source inspection of the local Apex-6 project confirms that its drone is based on a native missile prefab, and its TEL uses `MissileLauncher` and `Turret`. The local HSM-290 Killjoy project also uses `Missile`, but includes additional runtime Harmony guidance logic, so the combined behavior still needs an in-mission check.

These are **structural compatibility findings, not a claim that every mod combination has been tested in a live mission**. Another plugin may skip a patched method or overwrite thrust, control inputs, timings or damage after this mod applies its effect. Such cases require checking the specific mod and patch ordering. Native seeker lock remains unchanged by Revert To Stone Age.

To check a particular combination, enable verbose logging and temporarily use Custom with 100% failure rates, launch the added missile, and inspect the scheduled-failure message. This confirms that the launch hook sees it; observing its flight is still required to establish that another mod does not override the selected defect. Restore a normal preset afterward. For an added SAM, separately check its native turret path and radar FireControl path; support for its missile alone does not prove that every launcher penalty applies.

## Building and testing

The source targets .NET Framework 4.7.2. Supply your game directory to resolve the game's and BepInEx's assemblies:

```powershell
dotnet build RevertToStoneAge.csproj -c Release -p:GameDir="C:\Games\Nuclear Option"
dotnet run --project Tests/ModelTests.csproj -c Release
dotnet build Tests/ConfigIntegrationTests.csproj -c Release -p:GameDir="C:\Games\Nuclear Option"
Tests/bin/Release/net472/ConfigIntegrationTests.exe "C:\Games\Nuclear Option"
```

The DLL is written to `bin/Release/RevertToStoneAge.dll`. Building does not install it unless `-p:DeployToGame=true` is explicitly supplied. Game assemblies and decompiled game source are not included in this repository.

This project is an unofficial community modification and is not affiliated with, sponsored by, or endorsed by Shockfront Studios Pty Ltd. Original Nuclear Option assets, vehicle designs, audio, and code are Copyright (c) 2026 Shockfront Studios Pty Ltd. All rights reserved. Nuclear Option and Shockfront Studios are trademarks or registered trademarks of Shockfront Studios Pty Ltd. Original mod content and all other trademarks belong to their respective owners.

## References

The [NWS explanation of a radar cone of silence](https://www.weather.gov/mlb/Doppler_Dual_Pol_Weather_Radar) illustrates overhead coverage limits using weather radar; its numerical scan angles are not used as military radar specifications here. [MDA's history of remote-sensor interception](https://www.mda.mil/about/history.html) provides context for keeping shared sensor information intact.

All numerical penalties and failure rates in this mod are gameplay-model settings rather than measured specifications for particular weapons.
