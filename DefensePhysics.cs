using System;
namespace RevertToStoneAge
{
    public enum DefenseRole { Other, Gun, PointDefense, AreaDefense }
    public static class DefensePhysics
    {
        public static bool HasLaunchBudget(int shots, int limit) => limit == 0 || shots < limit;
        public static int CountConsumedRounds(int shots, int before, int after) => shots + Math.Max(0, before - after);
        public static DefenseRole Classify(bool gun, bool missile, float range, float speed, float areaRange, float areaSpeed)
        {
            if (gun) return DefenseRole.Gun;
            if (!missile) return DefenseRole.Other;
            return range >= areaRange && speed >= areaSpeed ? DefenseRole.AreaDefense : DefenseRole.PointDefense;
        }
        public static bool TryInterceptTime(double distanceSquared, double dotPositionVelocity, double targetSpeedSquared, double interceptorSpeed, out double time)
        {
            time = 0;
            if (interceptorSpeed <= 0 || distanceSquared <= 0) return false;
            double a = targetSpeedSquared - interceptorSpeed * interceptorSpeed;
            double b = 2 * dotPositionVelocity;
            if (Math.Abs(a) < 1e-6) { if (b >= 0) return false; time = -distanceSquared / b; return time > 0; }
            double disc = b * b - 4 * a * distanceSquared;
            if (disc < 0) return false;
            double root = Math.Sqrt(disc);
            double t1 = (-b - root) / (2 * a), t2 = (-b + root) / (2 * a);
            time = t1 > 0 && t2 > 0 ? Math.Min(t1, t2) : Math.Max(t1, t2);
            return time > 0;
        }
        public static bool CanEngage(double rangeSquared, double radialDot, double targetSpeedSquared, double interceptorSpeed,
            double reaction, double minimumFlight, double approachCosine, double protectedRadius, bool area)
        {
            if (targetSpeedSquared <= 0 || radialDot >= 0) return false;
            double closestTime = -radialDot / targetSpeedSquared;
            if (area)
            {
                double approach = -radialDot / Math.Sqrt(rangeSquared * targetSpeedSquared);
                double missSquared = Math.Max(0, rangeSquared - radialDot * radialDot / targetSpeedSquared);
                if (approach < approachCosine || missSquared > protectedRadius * protectedRadius) return false;
            }
            // Advance the target over the reaction delay before solving the meeting point.
            double advancedSquared = rangeSquared + 2 * radialDot * reaction + targetSpeedSquared * reaction * reaction;
            double advancedDot = radialDot + targetSpeedSquared * reaction;
            double flight;
            return closestTime > reaction && TryInterceptTime(advancedSquared, advancedDot, targetSpeedSquared, interceptorSpeed, out flight)
                && reaction + Math.Max(flight, minimumFlight) < closestTime;
        }
        public static float GunLeadError(float speed, float flightTime, float transverseFraction, float onsetSpeed, float maximumDegrees)
        {
            float stress = Math.Max(0f, Math.Min(1f, (speed - onsetSpeed) / Math.Max(1f, 1700f - onsetSpeed)));
            return maximumDegrees * stress * flightTime / (flightTime + 1f) * (0.2f + 0.8f * transverseFraction);
        }
    }
}
