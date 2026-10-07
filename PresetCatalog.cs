using System.Collections.Generic;

namespace RevertToStoneAge
{
    public enum ModPreset { Custom, Realistic, ArcadeEasy, ModernDefense, WornEquipment }

    public static class PresetCatalog
    {
        public static Dictionary<string, float> Get(ModPreset preset)
        {
            if (preset == ModPreset.Custom) return null;
            float severity = preset == ModPreset.ModernDefense ? 0.65f : preset == ModPreset.WornEquipment ? 1.4f : preset == ModPreset.ArcadeEasy ? 1.8f : 1f;
            return new Dictionary<string, float>
            {
                {"LowAltitudeThreshold", 120f}, {"LowAltitudeSignalPenalty", 1f - 0.4f * severity},
                {"LookDownAngleThreshold", 3f}, {"LookDownSignalPenalty", 1f - 0.3f * severity},
                {"ExtraClutterMultiplier", 1f + 0.8f * severity},
                {"GunTargetSwitchDelay", 0.3f * severity}, {"MissileTargetSwitchDelay", 1.8f * severity},
                {"TurretLockTimeMultiplier", 1f + 0.8f * severity},
                {"TurretSlewMultiplier", 1f},
                {"TrackingConvergenceTime", 0.8f * severity}, {"AngularTrackingLimit", 18f / severity},
                {"RadarFireControlPlanningMultiplier", 1f + 1.5f * severity},
                {"RadarSalvoIntervalMultiplier", 1f + severity},
                {"RadarMinimumPlanningSeconds", 1.2f * severity}, {"RadarMinimumSalvoIntervalSeconds", 0.8f * severity},
                {"LaserMaxFiringTime", 4f / severity}, {"LaserCooldownTime", 5f * severity},
                {"LaserMinimumOutput", 0.55f}, {"RadarMaximumElevation", 70f},
                {"CheapMunitionFailRate", 1.5f * severity}, {"StandardMunitionFailRate", 1f * severity}, {"HighEndMunitionFailRate", 0.5f * severity},
                {"FailureOnsetMin", 0.8f}, {"FailureOnsetMax", 3f},
                {"AreaMinimumRange", 25000f}, {"AreaMinimumSpeed", 1100f}, {"FastThreatSpeed", 700f},
                {"GunLeadErrorDegrees", 2f * severity}, {"AreaRadarMaximumElevation", 85f}, {"AreaReactionSeconds", 0.5f * severity},
                {"AreaPlanningMultiplier", 1f + 0.25f * severity}, {"AreaSalvoMultiplier", 1f + 0.25f * severity},
                {"AreaMinimumFlightSeconds", 1.5f}, {"PointMinimumFlightSeconds", 1f},
                {"AreaApproachHalfAngle", 55f}, {"ProtectedRadius", 4000f},
                {"HeavyMinimumTargetAltitude", 2000f}, {"TerminalBudgetAltitude", 12000f},
                {"IgnitionDelayDuration", 0.6f}, {"DegradedThrustMultiplier", 0.55f}
            };
        }
    }
}
