using System;

namespace RevertToStoneAge
{
    public enum MissileFailure { IgnitionFailure, IgnitionDelay, MotorCutout, ReducedThrust, ActuatorJam, ReducedControlAuthority }

    // Pure decisions shared by runtime hooks and the boundary tests.
    public static class BalanceRules
    {
        public static float ScaleTiming(float native, float multiplier, float minimum) => Math.Max(native * multiplier, minimum);
        public static float AdvanceStability(float stable, float delta, bool aligned, float angular, float limit, float settling)
        {
            if (!aligned || angular > limit) return 0f;
            return Math.Min(settling, stable + Math.Max(0f, delta) / (1f + angular / Math.Max(0.1f, limit)));
        }
        public static bool IsAboveCoverage(float x, float y, float z, float maximumElevation)
        {
            if (y <= 0f || maximumElevation >= 90f) return false;
            double horizontalSquared = (double)x * x + (double)z * z;
            double tangent = Math.Tan(maximumElevation * Math.PI / 180.0);
            return (double)y * y > horizontalSquared * tangent * tangent;
        }
        public static MissileFailure SelectFailure(float roll)
        {
            if (roll < 0.10f) return MissileFailure.IgnitionFailure;
            if (roll < 0.25f) return MissileFailure.IgnitionDelay;
            if (roll < 0.45f) return MissileFailure.MotorCutout;
            if (roll < 0.65f) return MissileFailure.ReducedThrust;
            if (roll < 0.80f) return MissileFailure.ActuatorJam;
            return MissileFailure.ReducedControlAuthority;
        }
        public static bool IsMotorBlocked(MissileFailure failure, float age, float onset, float ignitionDelayEnd)
        {
            return age >= onset && (failure == MissileFailure.IgnitionFailure || failure == MissileFailure.MotorCutout || (failure == MissileFailure.IgnitionDelay && age < ignitionDelayEnd));
        }
    }
}
