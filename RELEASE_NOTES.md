# v1.8.0 - Stronger air-defense effects and visible presets

Turret penalties were difficult to notice, and named presets changed hidden runtime values without updating visible configuration entries. This release strengthens air-defense penalties, covers radar-controlled salvo launchers, and makes preset settings match their effective values.

- Named preset selection now writes all numeric values into visible BepInEx settings, applies them immediately and saves them. Manual numeric edits select Custom; feature toggles remain independent.
- Stronger Realistic values: 0.8-second gun reaction, 1.8-second missile reaction, 1.8x lock time, 62.5% automatic turret slew, 0.8-second base settling and an 18-degree/second angular tracking limit.
- Settling now resets during established engagements when native aiming loses alignment or relative angular motion exceeds the limit.
- Radar-controlled queued SAM salvos are covered independently of turret aiming: 2.5x planning with a 1.2-second floor per shot, 2x launch intervals with a 0.8-second floor. Native targets, queues and ammunition accounting remain intact.
- Stronger clutter, laser heat and tiered failure rates. Six existing motor/actuator defects remain; no seeker lock loss is added.
- Updated English documentation and complete preset templates.

Release build has zero errors/warnings. 118 pure model assertions and 118 integration assertions against actual BepInEx config events pass. Ten target methods and typed fields verified against installed game assemblies. Live Unity installation, mission balance, frame-time impact and multiplayer synchronization remain unverified. Async waits already scheduled before a config change keep their original duration.


## Downloads

- `RevertToStoneAge-1.8.0.zip`: plugin under `BepInEx/plugins`, English documentation and preset templates.
- `RevertToStoneAge.dll`: standalone plugin.
- `SHA256SUMS.txt`: checksums for both downloads.

Close the game before updating, replace the old DLL, and restart. Named presets apply the new numeric values at startup. Back up custom configurations before importing a template. Requires BepInEx with Harmony; built against Nuclear Option 0.34.1.
