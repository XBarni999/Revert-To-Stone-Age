# v1.9.0 - Role-based air defense

- Guns keep native drives and clean subsonic prediction; high-speed lead uncertainty depends on flight time and transverse movement.
- Point defense retains reaction and flight-window constraints.
- Area defense retains strong native missile physics and receives shorter reaction and lighter radar/timing penalties; fast threats must have a feasible approaching trajectory within the configurable protected footprint.
- Add optional heavy-interceptor altitude floor and per-launching-unit terminal round budget. Existing airborne missiles are unaffected.
- No forced guaranteed leaks or intercepts, and no seeker lock loss.

Build and structural checks pass. 137 model assertions and 174 actual BepInEx config assertions pass; 13 Harmony target methods/private fields verified. Mission balance and live compatibility remain unverified. Envelope parameters and role thresholds are gameplay assumptions rather than hardware specifications.


## Default role settings

- Guns: native traverse/elevation, 0.3-second Realistic reaction, 1.1x lock time. Added prediction uncertainty starts above 330 m/s.
- Area-defense classification: native missile weapon range at least 25 km and nominal interceptor speed at least 1100 m/s. These are configurable heuristics.
- Area defense: 0.5-second reaction, 1.25x planning/salvo timing, 85-degree radar elevation coverage for long-range radars and lighter added clutter.
- Fast threat envelope: missile speed at least 700 m/s; approaching within 55 degrees of the radial line with predicted closest passage within 4 km of the launcher. Meeting time accounts for reaction delay, effective interceptor speed and a minimum flight-time floor.
- Heavy terminal altitude floor: 2 km. Terminal budget: two rounds per launching vehicle/ship and target below 12 km for fast descending threats. Both constraints are configurable; already airborne interceptors remain active.

## Downloads and installation

- `RevertToStoneAge-1.9.0.zip`: plugin under `BepInEx/plugins`, English documentation and preset templates.
- `RevertToStoneAge.dll`: standalone plugin.
- `SHA256SUMS.txt`: download checksums.

Close the game, replace the older DLL and restart. Requires BepInEx with Harmony; built against Nuclear Option 0.34.1. Named presets apply updated numeric values at startup. Back up custom settings before importing templates. See the README for role thresholds and limitations.
