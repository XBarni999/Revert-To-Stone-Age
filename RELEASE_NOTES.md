# v1.7.0 - First public release

Revert To Stone Age adds configurable air-defense limitations and missile reliability mechanics to Nuclear Option. Built against game version 0.34.1; requires BepInEx with Harmony.

## Included changes

- **Surface radar upper blind zone:** ground and ship radars default to a 70-degree elevation limit, leaving a 20-degree half-angle blind cone around the zenith. The angle uses the actual antenna; aircraft/missile radar coverage and shared tracks remain native. The limit is configurable and can be disabled.
- **Radar clutter:** gradual low-height and look-down clutter scaling while preserving native radar-return evaluation, warnings, jamming and terrain checks. Optional additional radar-horizon filter.
- **Acquisition and settling:** target-switch delays, native lock-time scaling and settling based on relative angular motion. No price-gated Mach immunity or hard terminal interception deadzone.
- **Laser thermal budget:** firing endurance, idle cooling, overheat cooldown and gradual damage derating. Default Realistic values: 6-second firing budget, 4-second cooldown and 65% minimum output.
- **Six missile malfunctions:** ignition failure, delayed ignition, in-flight motor cutout, reduced thrust, actuator jam and reduced control authority. The authoritative launch path covers AI/SAM/ship missiles as well as player missiles. Default overall failure chance is 0.5% per launch; six types divide that chance rather than multiply it.
- **Presets:** ModernDefense, Realistic, WornEquipment, ArcadeEasy and Custom. Ready-to-use configuration templates are included.
- **Runtime efficiency:** cached typed field delegates replace boxed per-tick reflection; duplicate radar evaluation and obsolete profiling hooks are removed.

Seeker lock loss is not implemented. Native target, aimpoint, seeker and proximity-fuse updates are retained.

## Downloads and installation

- `RevertToStoneAge-1.7.0.zip`: installation package with the DLL under `BepInEx/plugins`, English documentation and preset templates.
- `RevertToStoneAge.dll`: standalone plugin for manual installation.
- `SHA256SUMS.txt`: hashes for both downloads.

Close the game, extract the ZIP's `BepInEx` folder into the game directory, and remove duplicate older copies of this plugin. The package does not overwrite your configuration automatically. See the README for presets and configuration.

## Validation

Release build: zero errors/warnings. Eight Harmony targets and private-field delegates checked against installed game assemblies. 94 automated behavior assertions passed. AI launch routing was traced in the game code.

Live Unity patch installation, AI/SAM mission behavior, mission balance, frame-time impact and multiplayer synchronization remain unverified. A standalone .NET patch-installation attempt encountered Unity's external-call restriction. Numerical settings are gameplay assumptions, not measured specifications for real equipment.
