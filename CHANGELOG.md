# 1.8.0

- Write named preset values into visible BepInEx entries on selection/startup; numeric edits select Custom, toggles remain independent. Batch preset saves and avoid recursive events.
- Strengthen preset reaction, lock-time, clutter, heat and failure settings. Realistic automatic turret slew is 62.5% of native speed.
- Make acquisition settling persistent: native alignment loss or excessive angular motion resets stability during established tracking, without changing missile seeker lock.
- Cover radar FireControl PlanSalvo/LaunchSalvo paths that bypass turret aiming. Add planning/interval multipliers and minimum times; refresh registered controllers on configuration changes. Existing async waits retain their scheduled duration.
- Pass 118 model and 118 actual BepInEx configuration assertions. Ten patch targets and typed fields verified. Live mission balance remains unverified.

# 1.7.0

- Add independent surface radar upper coverage: 70-degree default elevation limit, actual antenna origin, early request rejection and final signal gate. Preserve aircraft/missile antennas, visual detection and shared tracks.
- Expand missile malfunctions to ignition failure, ignition delay, in-flight motor cutout, reduced thrust, actuator jam and reduced control authority. Preserve the total failure probability.
- Preserve seeker lock and all native target/aimpoint/fuse updates; no seeker dropout feature.
- Trace bot/SAM launch routing through the same authoritative StartMissile hook. Confirm eight method targets and typed delegates; pass 94 pure behavior assertions. Live mission and Unity patch installation remain unverified.

# 1.6.0

- Replace price-gated Mach immunity and hard terminal deadzones with angular-motion acquisition settling.
- Preserve native radar return evaluation; add gradual clutter scaling and optional sea-relative radar horizon.
- Replace random optical penalties with laser heat, idle cooling and gradual damage derating; preserve native safety and beam scale.
- Add ModernDefense and WornEquipment presets, configuration templates and live ConfigEntry updates.
- Reduce preset missile failures, evaluate only on authoritative simulation and reset launch state.
- Replace per-tick boxed reflection with cached typed field delegates; remove unused profiling and obsolete configuration options.
- Validate seven patch targets and private field delegates against installed game assemblies. Mission balance, frame-time impact and multiplayer behavior remain unverified.
