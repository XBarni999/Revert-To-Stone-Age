using System;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace RevertToStoneAge
{
    public enum ModPreset
    {
        Custom,
        Realistic,
        ArcadeEasy,
        ModernDefense,
        WornEquipment
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RevertToStoneAgePlugin : BaseUnityPlugin
    {
        public static RevertToStoneAgePlugin Instance { get; private set; }
        internal static ManualLogSource Log => Instance.Logger;

        // 0. General & Presets
        public static ConfigEntry<ModPreset> ActivePreset;

        // 1. Radar clutter
        public static ConfigEntry<bool> RadarClutterEnabled;
        public static ConfigEntry<float> LowAltitudeThreshold;
        public static ConfigEntry<float> LowAltitudeSignalPenalty;
        public static ConfigEntry<float> LookDownAngleThreshold;
        public static ConfigEntry<float> LookDownSignalPenalty;
        public static ConfigEntry<float> ExtraClutterMultiplier;

        // 2. Turret acquisition
        public static ConfigEntry<bool> TurretDelayEnabled;
        public static ConfigEntry<float> GunTargetSwitchDelay;
        public static ConfigEntry<float> MissileTargetSwitchDelay;
        public static ConfigEntry<float> TurretLockTimeMultiplier;

        // 3. Laser thermal budget
        public static ConfigEntry<bool> LaserThermalEnabled;
        public static ConfigEntry<float> LaserMaxFiringTime;
        public static ConfigEntry<float> LaserCooldownTime;

        // 4. Tracking settling (legacy config section retained)
        public static ConfigEntry<bool> TrackingJitterEnabled;
        public static ConfigEntry<float> TrackingConvergenceTime;

        // 5. Weapon Malfunctions
        public static ConfigEntry<bool> MunitionMalfunctionsEnabled;
        public static ConfigEntry<float> CheapMunitionFailRate;
        public static ConfigEntry<float> StandardMunitionFailRate;
        public static ConfigEntry<float> HighEndMunitionFailRate;

        // 6. Diagnostics
        public static ConfigEntry<bool> VerboseDebugLog;

        // Cached macro values
        public static float CachedLowAltPenalty = 0.5f;
        public static float CachedLookDownPenalty = 0.55f;
        public static float CachedClutterMult = 2.0f;
        public static float CachedLockTimeMult = 1.4f;
        public static float CachedLaserMaxFire = 1.8f;
        public static float CachedLaserCooldown = 3.0f;

        public static ConfigEntry<float> AngularTrackingLimit;
        public static ConfigEntry<bool> RadarHorizonEnabled;
        public static ConfigEntry<float> LaserMinimumOutput;
        public static ConfigEntry<bool> RadarUpperBlindZoneEnabled;
        public static ConfigEntry<float> RadarMaximumElevation;
        public static ConfigEntry<float> FailureOnsetMin;
        public static ConfigEntry<float> FailureOnsetMax;
        public static ConfigEntry<float> IgnitionDelayDuration;
        public static ConfigEntry<float> DegradedThrustMultiplier;
        internal static float Severity => ActivePreset.Value == ModPreset.ModernDefense ? 0.5f : ActivePreset.Value == ModPreset.WornEquipment ? 1.5f : ActivePreset.Value == ModPreset.ArcadeEasy ? 2f : 1f;
        internal static float Setting(float custom, float realistic) => ActivePreset.Value == ModPreset.Custom ? custom : realistic * Severity;

        private void Awake()
        {
            Instance = this;

            FastReflection.Initialize();

            // 0. General
            ActivePreset = Config.Bind("0. General", "Preset", ModPreset.Realistic,
                "Select difficulty preset:\n" +
                "Realistic - balanced modern air-defense characteristics with moderate acquisition and heat limits;\n" +
                "ArcadeEasy - forgiving gameplay with higher delays, rapid laser overheating, and weaker defensive endurance;\n" +
                "ModernDefense - well-maintained equipment; WornEquipment - degraded readiness;\n" +
                "Custom - uses custom numbers from sections below.");

            // 1. Radar Clutter
            RadarClutterEnabled = Config.Bind("1. Radar Ground Clutter", "Enabled", true,
                "Scale native clutter for low sea-relative height and downward viewing; native terrain masking is preserved.");
            LowAltitudeThreshold = Config.Bind("1. Radar Ground Clutter", "LowAltitudeThresholdMeters", 70f,
                new ConfigDescription("Target altitude (meters) below which low-altitude signal degradation begins.", new AcceptableValueRange<float>(1f, 1000f)));
            LowAltitudeSignalPenalty = Config.Bind("1. Radar Ground Clutter", "LowAltitudeSignalPenalty", 0.8f,
                new ConfigDescription("Native clutter attenuation proxy for low-altitude targets (lower = harder to detect near ground).", new AcceptableValueRange<float>(0.1f, 1f)));
            LookDownAngleThreshold = Config.Bind("1. Radar Ground Clutter", "LookDownAngleThresholdDeg", 3.0f,
                new ConfigDescription("Downward radar pitch angle (degrees) against ground backdrop triggering clutter penalty.", new AcceptableValueRange<float>(0f, 89f)));
            LookDownSignalPenalty = Config.Bind("1. Radar Ground Clutter", "LookDownSignalPenalty", 0.85f,
                new ConfigDescription("Native clutter return multiplier when radar looks down towards terrain.", new AcceptableValueRange<float>(0.1f, 1f)));
            ExtraClutterMultiplier = Config.Bind("1. Radar Ground Clutter", "ExtraClutterMultiplier", 1.25f,
                new ConfigDescription("Multiplier for the game's internal clutterFactor.", new AcceptableValueRange<float>(1f, 8f)));

            // 2. Turret Slew & Reaction Delay
            TurretDelayEnabled = Config.Bind("2. Turret Reaction Delay", "Enabled", true,
                "Enable acquisition delay and native lock-time scaling.");
            GunTargetSwitchDelay = Config.Bind("2. Turret Reaction Delay", "GunTargetSwitchDelaySeconds", 0.25f,
                new ConfigDescription("Engagement slew and fire-control delay for autocannons and CIWS (seconds).", new AcceptableValueRange<float>(0f, 15f)));
            MissileTargetSwitchDelay = Config.Bind("2. Turret Reaction Delay", "MissileTargetSwitchDelaySeconds", 0.7f,
                new ConfigDescription("Engagement preparation delay for SAM and IR missile launchers (seconds).", new AcceptableValueRange<float>(0f, 15f)));
            TurretLockTimeMultiplier = Config.Bind("2. Turret Reaction Delay", "TurretLockTimeMultiplier", 1.15f,
                new ConfigDescription("Multiplier for turret's internal lockTime property.", new AcceptableValueRange<float>(0.1f, 5f)));

            // 3. Laser thermal budget
            LaserThermalEnabled = Config.Bind("3. Laser Thermal Budget", "Enabled", true,
                "Enable laser capacitor and thermal budget mechanics.");
            LaserMaxFiringTime = Config.Bind("3. Laser Thermal Budget", "LaserMaxContinuousFiringSeconds", 6f,
                new ConfigDescription("Maximum continuous firing duration before capacitor depletion / overheat.", new AcceptableValueRange<float>(0.1f, 60f)));
            LaserCooldownTime = Config.Bind("3. Laser Thermal Budget", "LaserCooldownSeconds", 4f,
                new ConfigDescription("Cooldown / recharge duration after laser depletion (seconds).", new AcceptableValueRange<float>(0.1f, 120f)));

            // 4. CIWS Tracking Jitter
            TrackingJitterEnabled = Config.Bind("4. CIWS Tracking Jitter", "Enabled", true,
                "Enable settling delay based on relative angular motion; native aiming remains intact.");
            TrackingConvergenceTime = Config.Bind("4. CIWS Tracking Jitter", "TrackingConvergenceSeconds", 0.4f,
                new ConfigDescription("Time (seconds) for turret aim to converge and stabilize on target.", new AcceptableValueRange<float>(0.05f, 10f)));

            // 5. Munition Malfunctions
            MunitionMalfunctionsEnabled = Config.Bind("5. Munition Malfunctions", "Enabled", true,
                "Enable rare motor and control actuator failures on authoritative missiles, including bot SAMs. Seeker lock is untouched.");
            CheapMunitionFailRate = Config.Bind("5. Munition Malfunctions", "CheapMunitionFailRatePercent", 0.5f,
                new ConfigDescription("Failure chance for cheap munitions (cost < 1,000,000) in %.", new AcceptableValueRange<float>(0f, 100f)));
            StandardMunitionFailRate = Config.Bind("5. Munition Malfunctions", "StandardMunitionFailRatePercent", 0.5f,
                new ConfigDescription("Failure chance for mid-tier weapons (cost 1,000,000 to 10,000,000) in %.", new AcceptableValueRange<float>(0f, 100f)));
            HighEndMunitionFailRate = Config.Bind("5. Munition Malfunctions", "HighEndMunitionFailRatePercent", 0.5f,
                new ConfigDescription("Failure chance for high-end strategic weapons (cost > 10,000,000) in %.", new AcceptableValueRange<float>(0f, 100f)));

            // 6. Diagnostics
            VerboseDebugLog = Config.Bind("6. Diagnostics", "VerboseLogging", false,
                "Output detailed debug logs for every single malfunction and penalty event.");

            AngularTrackingLimit = Config.Bind("7. Physical Limits", "AngularTrackingLimitDegPerSecond", 30f, new ConfigDescription("Soft tracking limit based on transverse relative velocity / range; no Mach immunity.", new AcceptableValueRange<float>(1f, 180f)));
            RadarHorizonEnabled = Config.Bind("7. Physical Limits", "RadarHorizonEnabled", true, "Apply approximate Earth-curvature radar horizon using sea-relative heights. Native terrain checks remain in effect.");
            LaserMinimumOutput = Config.Bind("7. Physical Limits", "LaserMinimumOutput", 0.65f, new ConfigDescription("Thermal output at the end of a firing burst; scales native damage, not electrical consumption.", new AcceptableValueRange<float>(0.1f, 1f)));
            RadarUpperBlindZoneEnabled = Config.Bind("8. Radar Upper Coverage", "Enabled", true, "Limit surface radar elevation. Aircraft and missile radars keep native antenna coverage. Does not erase shared tracks.");
            RadarMaximumElevation = Config.Bind("8. Radar Upper Coverage", "MaximumElevationDegrees", 70f, new ConfigDescription("Maximum elevation above the horizontal at the actual antenna. 70 means a 20-degree blind cone around zenith; 90 disables it.", new AcceptableValueRange<float>(20f, 90f)));
            FailureOnsetMin = Config.Bind("5. Munition Malfunctions", "DelayedFailureEarliestSeconds", 0.8f, new ConfigDescription("Earliest onset for in-flight failures.", new AcceptableValueRange<float>(0.1f, 30f)));
            FailureOnsetMax = Config.Bind("5. Munition Malfunctions", "DelayedFailureLatestSeconds", 3f, new ConfigDescription("Latest onset; automatically sorted against earliest.", new AcceptableValueRange<float>(0.1f, 30f)));
            IgnitionDelayDuration = Config.Bind("5. Munition Malfunctions", "FaultyIgnitionDelaySeconds", 0.6f, new ConfigDescription("Additional ignition delay for a faulty motor, after ejection. Native motor logic resumes afterward.", new AcceptableValueRange<float>(0.1f, 5f)));
            DegradedThrustMultiplier = Config.Bind("5. Munition Malfunctions", "DegradedMotorThrustMultiplier", 0.55f, new ConfigDescription("Remaining thrust for a degraded motor; native fuel use remains unchanged.", new AcceptableValueRange<float>(0.1f, 0.95f)));
            Config.SettingChanged += (_, __) => UpdateCachedPresetValues();
            UpdateCachedPresetValues();


            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            Log.LogInfo($"{PluginInfo.PLUGIN_NAME} v{PluginInfo.PLUGIN_VERSION} initialized with physical acquisition limits and thermal budget.");
        }

        public static void UpdateCachedPresetValues()
        {
            bool custom = ActivePreset.Value == ModPreset.Custom;
            float severity = Severity;
            CachedLowAltPenalty = custom ? Mathf.Clamp01(LowAltitudeSignalPenalty.Value) : Mathf.Clamp01(1f - 0.2f * severity);
            CachedLookDownPenalty = custom ? Mathf.Clamp01(LookDownSignalPenalty.Value) : Mathf.Clamp01(1f - 0.15f * severity);
            CachedClutterMult = custom ? Mathf.Clamp(ExtraClutterMultiplier.Value, 1f, 8f) : 1f + 0.25f * severity;
            CachedLockTimeMult = custom ? Mathf.Clamp(TurretLockTimeMultiplier.Value, 0.1f, 5f) : 1f + 0.15f * severity;
            CachedLaserMaxFire = custom ? Mathf.Clamp(LaserMaxFiringTime.Value, 0.1f, 60f) : 6f / severity;
            CachedLaserCooldown = custom ? Mathf.Clamp(LaserCooldownTime.Value, 0.1f, 120f) : 4f * severity;
        }

        public static float GetTargetSwitchDelay(bool missile) => Mathf.Clamp(Setting(missile ? MissileTargetSwitchDelay.Value : GunTargetSwitchDelay.Value, missile ? 0.7f : 0.25f), 0f, 15f);
        public static float GetFailRateForCost(float cost)
        {
            float custom = cost < 1000000f ? CheapMunitionFailRate.Value : cost < 10000000f ? StandardMunitionFailRate.Value : HighEndMunitionFailRate.Value;
            // Price is only a coarse configurable proxy; these are gameplay assumptions, not measured reliability.
            return Mathf.Clamp(Setting(custom, 0.5f), 0f, 100f);
        }
    }

    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "com.xbarni.reverttostoneage";
        public const string PLUGIN_NAME = "Revert To Stone Age";
        public const string PLUGIN_VERSION = "1.7.0";
    }

    // Typed cached field delegates avoid FieldInfo.GetValue/SetValue boxing on simulation ticks.
    public static class FastReflection
    {
        internal static readonly AccessTools.FieldRef<Turret, Unit> Owner = AccessTools.FieldRefAccess<Turret, Unit>("attachedUnit");
        internal static readonly AccessTools.FieldRef<Turret, Unit> Target = AccessTools.FieldRefAccess<Turret, Unit>("target");
        internal static readonly AccessTools.FieldRef<Turret, WeaponStation> Station = AccessTools.FieldRefAccess<Turret, WeaponStation>("currentWeaponStation");
        internal static readonly AccessTools.FieldRef<Turret, float> LockTime = AccessTools.FieldRefAccess<Turret, float>("lockTime");
        internal static readonly AccessTools.FieldRef<Laser, bool> FireCommanded = AccessTools.FieldRefAccess<Laser, bool>("fireCommanded");
        internal static readonly AccessTools.FieldRef<Missile, float> Thrust = AccessTools.FieldRefAccess<Missile, float>("engineCurrentThrust");
        internal static readonly AccessTools.FieldRef<Missile, bool> Ignition = AccessTools.FieldRefAccess<Missile, bool>("ignition");
        internal static readonly AccessTools.FieldRef<Missile, Vector3> Inputs = AccessTools.FieldRefAccess<Missile, Vector3>("inputs");
        internal static readonly AccessTools.FieldRef<TargetDetector, Unit> RadarOwner = AccessTools.FieldRefAccess<TargetDetector, Unit>("attachedUnit");
        public static void Initialize() { if (Owner == null) throw new InvalidOperationException("Missing game fields"); }
    }

    // Keep the native radar evaluation, warnings and jamming intact; only adjust clutter input.
    [HarmonyPatch(typeof(Radar), "CanSeeRadarReturn")]
    public static class RadarClutterPatch
    {
        private static bool Prefix(Radar __instance, IRadarReturn radarReturn, float dist, ref float clutterFactor, ref bool __result)
        {
            if (!(radarReturn is Component target) || target == null) return true;
            if (RadarUpperCoveragePatch.IsBlind(__instance, target.transform.position)) { __result = false; return false; }
            if (!RevertToStoneAgePlugin.RadarClutterEnabled.Value) return true;
            Vector3 scannerPos = __instance.GetScanPoint().position;
            Vector3 offset = target.transform.position - scannerPos;
            float height = Mathf.Max(0f, target.transform.position.y - Datum.LocalSeaY);
            float radarHeight = Mathf.Max(0f, scannerPos.y - Datum.LocalSeaY);
            if (RevertToStoneAgePlugin.RadarHorizonEnabled.Value && dist > 4120f * (Mathf.Sqrt(height) + Mathf.Sqrt(radarHeight)))
            {
                __result = false;
                return false;
            }
            float altitude = Mathf.Clamp01(height / Mathf.Max(1f, RevertToStoneAgePlugin.LowAltitudeThreshold.Value));
            float low = Mathf.Lerp(RevertToStoneAgePlugin.CachedLowAltPenalty, 1f, altitude);
            float down = Mathf.Clamp01((-offset.y / Mathf.Max(1f, offset.magnitude) - Mathf.Sin(Mathf.Clamp(RevertToStoneAgePlugin.LookDownAngleThreshold.Value, 0f, 89f) * Mathf.Deg2Rad)) / 0.25f);
            float look = Mathf.Lerp(1f, RevertToStoneAgePlugin.CachedLookDownPenalty, down);
            clutterFactor *= RevertToStoneAgePlugin.CachedClutterMult / Mathf.Max(0.1f, low * look);
            return true;
        }
    }

    // Apply acquisition limits after native aiming, before FixedUpdate decides whether to fire.
    [HarmonyPatch(typeof(Turret), "AimTurret", new Type[] { typeof(WeaponStation) })]
    public static class TurretAcquisitionPatch
    {
        private class State { public Unit Target; public float Acquired; public float LockMultiplier = 1f; }
        private static readonly ConditionalWeakTable<Turret, State> States = new ConditionalWeakTable<Turret, State>();
        private static void Postfix(Turret __instance, ref bool __result)
        {
            State state = States.GetOrCreateValue(__instance);
            float mult = RevertToStoneAgePlugin.TurretDelayEnabled.Value ? RevertToStoneAgePlugin.CachedLockTimeMult : 1f;
            if (mult != state.LockMultiplier)
            {
                FastReflection.LockTime(__instance) = FastReflection.LockTime(__instance) / state.LockMultiplier * mult;
                state.LockMultiplier = mult;
            }
            Unit target = FastReflection.Target(__instance);
            if (target != state.Target) { state.Target = target; state.Acquired = Time.time; }
            if (target == null) return;
            WeaponStation station = FastReflection.Station(__instance);
            bool missile = station != null && station.WeaponInfo != null && station.WeaponInfo.missile;
            float elapsed = Time.time - state.Acquired;
            if (RevertToStoneAgePlugin.TurretDelayEnabled.Value && elapsed < RevertToStoneAgePlugin.GetTargetSwitchDelay(missile)) __result = false;
            if (!RevertToStoneAgePlugin.TrackingJitterEnabled.Value) return;
            Vector3 offset = target.transform.position - __instance.transform.position;
            Vector3 velocity = target.rb != null ? target.rb.velocity : target.transform.forward * target.speed;
            Unit owner = FastReflection.Owner(__instance);
            if (owner != null && owner.rb != null) velocity -= owner.rb.velocity;
            float angular = Vector3.Cross(offset, velocity).magnitude / Mathf.Max(1f, offset.sqrMagnitude) * Mathf.Rad2Deg;
            float limit = Mathf.Clamp(RevertToStoneAgePlugin.AngularTrackingLimit.Value, 1f, 180f);
            float settling = Mathf.Clamp(RevertToStoneAgePlugin.Setting(RevertToStoneAgePlugin.TrackingConvergenceTime.Value, 0.4f), 0.05f, 10f);
            // Smooth extra settling budget: a receding fast target does not get a speed-based exemption.
            if (elapsed < settling * (1f + Mathf.Clamp(angular / limit, 0f, 3f))) __result = false;
        }
    }

    [HarmonyPatch(typeof(Laser), "Fire")]
    public static class LaserThermalFirePatch
    {
        internal class State { public float Heat; public float Last; public float BlockedUntil; public float Output = 1f; public bool Initialized; }
        internal static readonly ConditionalWeakTable<Laser, State> States = new ConditionalWeakTable<Laser, State>();
        private static bool Prefix(Laser __instance)
        {
            if (!RevertToStoneAgePlugin.LaserThermalEnabled.Value) return true;
            State state = States.GetOrCreateValue(__instance);
            float now = Time.time;
            if (!state.Initialized) { state.Last = now; state.Initialized = true; }
            float gap = Mathf.Max(0f, now - state.Last);
            float capacity = RevertToStoneAgePlugin.CachedLaserMaxFire;
            float cooldown = RevertToStoneAgePlugin.CachedLaserCooldown;
            if (gap > Time.fixedDeltaTime * 1.5f) state.Heat = Mathf.Max(0f, state.Heat - gap * capacity / cooldown);
            state.Last = now;
            if (now < state.BlockedUntil) return false;
            state.Heat += Mathf.Min(gap, Time.fixedDeltaTime);
            state.Output = Mathf.Lerp(1f, RevertToStoneAgePlugin.LaserMinimumOutput.Value, Mathf.Clamp01(state.Heat / capacity));
            if (state.Heat < capacity) return true;
            state.BlockedUntil = now + cooldown;
            state.Heat = 0f;
            return false;
        }
    }

    // Native damage uses these fields; restore after each simulation tick, including exceptions.
    [HarmonyPatch(typeof(Laser), "FixedUpdate")]
    public static class LaserOutputPatch
    {
        private static readonly AccessTools.FieldRef<Laser, float> Blast = AccessTools.FieldRefAccess<Laser, float>("blastDamage");
        private static readonly AccessTools.FieldRef<Laser, float> Fire = AccessTools.FieldRefAccess<Laser, float>("fireDamage");
        private static void Prefix(Laser __instance, out Vector2 __state)
        {
            __state = new Vector2(Blast(__instance), Fire(__instance));
            if (!RevertToStoneAgePlugin.LaserThermalEnabled.Value || !LaserThermalFirePatch.States.TryGetValue(__instance, out var state)) return;
            if (Time.time < state.BlockedUntil) FastReflection.FireCommanded(__instance) = false;
            Blast(__instance) *= state.Output;
            Fire(__instance) *= state.Output;
        }
        private static Exception Finalizer(Laser __instance, Vector2 __state, Exception __exception)
        {
            Blast(__instance) = __state.x;
            Fire(__instance) = __state.y;
            return __exception;
        }
    }

    // Reject before scheduling a terrain raycast and again at signal evaluation for other callers.
    [HarmonyPatch(typeof(NuclearOption.Jobs.DetectorManager), "RequestRadarCheck")]
    public static class RadarUpperCoveragePatch
    {
        internal static bool IsBlind(Radar radar, Vector3 targetPosition)
        {
            if (!RevertToStoneAgePlugin.RadarUpperBlindZoneEnabled.Value) return false;
            Unit owner = FastReflection.RadarOwner(radar);
            if (owner is Aircraft || owner is Missile) return false;
            Vector3 offset = targetPosition - radar.GetScanPoint().position;
            return BalanceRules.IsAboveCoverage(offset.x, offset.y, offset.z, RevertToStoneAgePlugin.RadarMaximumElevation.Value);
        }
        private static bool Prefix(TargetDetector detector, Unit target)
        {
            return !(detector is Radar radar) || target == null || !IsBlind(radar, target.transform.position);
        }
    }

    [HarmonyPatch(typeof(Missile), "StartMissile")]
    public static class MissileMalfunctionInitPatch
    {
        internal class State
        {
            public MissileFailure Failure;
            public float Onset;
            public float IgnitionDelayEnd;
            public float JamInput;
        }
        internal static readonly ConditionalWeakTable<Missile, State> States = new ConditionalWeakTable<Missile, State>();
        internal static bool TryActive(Missile missile, out State state)
        {
            state = null;
            return RevertToStoneAgePlugin.MunitionMalfunctionsEnabled.Value && missile.LocalSim && States.TryGetValue(missile, out state) && missile.timeSinceSpawn >= state.Onset;
        }
        private static void Postfix(Missile __instance)
        {
            States.Remove(__instance);
            if (!__instance.LocalSim || !RevertToStoneAgePlugin.MunitionMalfunctionsEnabled.Value) return;
            WeaponInfo info = __instance.GetWeaponInfo();
            float cost = info != null ? info.costPerRound : __instance.definition != null ? __instance.definition.value : 3000000f;
            if (UnityEngine.Random.value * 100f >= RevertToStoneAgePlugin.GetFailRateForCost(cost)) return;
            MissileFailure failure = BalanceRules.SelectFailure(UnityEngine.Random.value);
            float min = Mathf.Min(RevertToStoneAgePlugin.FailureOnsetMin.Value, RevertToStoneAgePlugin.FailureOnsetMax.Value);
            float max = Mathf.Max(RevertToStoneAgePlugin.FailureOnsetMin.Value, RevertToStoneAgePlugin.FailureOnsetMax.Value);
            float onset = (failure == MissileFailure.IgnitionFailure || failure == MissileFailure.IgnitionDelay) ? 0f : UnityEngine.Random.Range(min, max);
            States.Add(__instance, new State { Failure = failure, Onset = onset, IgnitionDelayEnd = RevertToStoneAgePlugin.IgnitionDelayDuration.Value, JamInput = UnityEngine.Random.value < 0.5f ? -0.25f : 0.25f });
            if (RevertToStoneAgePlugin.VerboseDebugLog.Value) RevertToStoneAgePlugin.Log.LogInfo($"Missile failure scheduled: {failure}, onset {onset:F2}s; owner: {__instance.owner}");
        }
    }

    [HarmonyPatch(typeof(Missile), "MotorThrust")]
    public static class MissileMotorFailurePatch
    {
        private static bool Prefix(Missile __instance)
        {
            if (!MissileMalfunctionInitPatch.TryActive(__instance, out var state) || !BalanceRules.IsMotorBlocked(state.Failure, __instance.timeSinceSpawn, state.Onset, state.IgnitionDelayEnd)) return true;
            FastReflection.Thrust(__instance) = 0f;
            FastReflection.Ignition(__instance) = false;
            return false;
        }
        private static void Postfix(Missile __instance)
        {
            if (MissileMalfunctionInitPatch.TryActive(__instance, out var state) && state.Failure == MissileFailure.ReducedThrust)
                FastReflection.Thrust(__instance) *= RevertToStoneAgePlugin.DegradedThrustMultiplier.Value;
        }
    }

    [HarmonyPatch(typeof(Missile), "Steering")]
    public static class MissileActuatorFailurePatch
    {
        private static void Postfix(Missile __instance)
        {
            if (!MissileMalfunctionInitPatch.TryActive(__instance, out var state)) return;
            // One actuator is stuck; leave native yaw/roll response and aerodynamics intact.
            Vector3 input = FastReflection.Inputs(__instance);
            if (state.Failure == MissileFailure.ActuatorJam) input.x = state.JamInput;
            else if (state.Failure == MissileFailure.ReducedControlAuthority) input *= 0.4f;
            else return;
            FastReflection.Inputs(__instance) = input;
        }
    }
}
