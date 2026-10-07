using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace RevertToStoneAge
{
    public static class DefenseIntegration
    {
        public static DefenseRole Role(WeaponInfo info)
        {
            if (info == null) return DefenseRole.Other;
            return DefensePhysics.Classify(info.gun, info.missile, info.targetRequirements.maxRange, info.GetMaxSpeed(),
                RevertToStoneAgePlugin.AreaMinimumRange.Value, RevertToStoneAgePlugin.AreaMinimumSpeed.Value);
        }
        internal static bool FastThreat(Unit target) => target is Missile && target.speed >= RevertToStoneAgePlugin.FastThreatSpeed.Value;
        internal static bool Terminal(Unit target) => FastThreat(target) && target.rb != null && target.rb.velocity.y < -50f && target.radarAlt <= RevertToStoneAgePlugin.TerminalBudgetAltitude.Value;
        internal static bool Eligible(Unit owner, Unit target, WeaponInfo info)
        {
            if (!RevertToStoneAgePlugin.DefenseRolesEnabled.Value || owner == null || !(owner is GroundVehicle || owner is Ship) || !FastThreat(target)) return true;
            DefenseRole role = Role(info);
            if (role != DefenseRole.PointDefense && role != DefenseRole.AreaDefense) return true;
            if (Terminal(target) && role == DefenseRole.AreaDefense && target.radarAlt < RevertToStoneAgePlugin.HeavyMinimumTargetAltitude.Value) return false;
            Vector3 position = target.transform.position - owner.transform.position;
            Vector3 velocity = target.rb != null ? target.rb.velocity : target.transform.forward * target.speed;
            if (owner.rb != null) velocity -= owner.rb.velocity;
            bool area = role == DefenseRole.AreaDefense;
            return DefensePhysics.CanEngage(position.sqrMagnitude, Vector3.Dot(position, velocity), velocity.sqrMagnitude,
                info.GetMaxSpeed() * 0.7f, area ? RevertToStoneAgePlugin.AreaReactionSeconds.Value : RevertToStoneAgePlugin.MissileTargetSwitchDelay.Value,
                area ? RevertToStoneAgePlugin.AreaMinimumFlightSeconds.Value : RevertToStoneAgePlugin.PointMinimumFlightSeconds.Value,
                Mathf.Cos(RevertToStoneAgePlugin.AreaApproachHalfAngle.Value * Mathf.Deg2Rad), RevertToStoneAgePlugin.ProtectedRadius.Value, area);
        }
    }

    [HarmonyPatch(typeof(AimSolver), "GetAimVector")]
    public static class GunLeadUncertaintyPatch
    {
        private static void Postfix(AimSolver __instance, float targetRange, ref Vector3 __result)
        {
            if (!RevertToStoneAgePlugin.DefenseRolesEnabled.Value || !RevertToStoneAgePlugin.TrackingJitterEnabled.Value) return;
            WeaponInfo info = FastReflection.AimInfo(__instance);
            Unit target = FastReflection.AimTarget(__instance);
            Transform muzzle = FastReflection.AimMuzzle(__instance);
            if (info == null || !info.gun || target == null || muzzle == null || target.speed <= 330f || __result.sqrMagnitude < 0.001f) return;
            Vector3 velocity = target.rb != null ? target.rb.velocity : target.transform.forward * target.speed;
            Vector3 direction = __result.normalized;
            float transverse = velocity.sqrMagnitude > 0f ? Vector3.Cross(direction, velocity.normalized).magnitude : 0f;
            float error = DefensePhysics.GunLeadError(target.speed, targetRange / Mathf.Max(100f, info.muzzleVelocity), transverse, 330f, RevertToStoneAgePlugin.GunLeadErrorDegrees.Value);
            Vector3 right = Vector3.Cross(direction, Vector3.up);
            if (right.sqrMagnitude < 0.001f) right = Vector3.Cross(direction, Vector3.right);
            right.Normalize();
            Vector3 up = Vector3.Cross(right, direction).normalized;
            float phase = muzzle.GetInstanceID() * 0.013f;
            float time = Time.time * 3f;
            // Correlated error after native prediction; native ballistics and fast turret drives remain intact.
            __result += (right * Mathf.Sin(time + phase) + up * Mathf.Sin(time * 0.71f + phase)) * (__result.magnitude * Mathf.Tan(error * Mathf.Deg2Rad) * 0.7071f);
        }
    }

    [HarmonyPatch(typeof(CombatAI), "AnalyzeTarget")]
    public static class DefenseTargetAssessmentPatch
    {
        private static void Postfix(WeaponStation weaponStation, Unit analyzer, TrackingInfo trackingInfo, ref OpportunityThreat __result)
        {
            if (__result.opportunity <= 0f || !RevertToStoneAgePlugin.DefenseRolesEnabled.Value) return;
            if (trackingInfo.TryGetUnit(out var target) && !DefenseIntegration.Eligible(analyzer, target, weaponStation.WeaponInfo))
                __result = new OpportunityThreat(0f, __result.threat);
        }
    }

    [HarmonyPatch(typeof(MissileLauncher), "Fire")]
    public static class DefenseLaunchEnvelopePatch
    {
        private class Budget { public int Shots; public float Last; }
        private class Launcher { public readonly ConditionalWeakTable<Unit, Budget> Targets = new ConditionalWeakTable<Unit, Budget>(); }
        private static readonly ConditionalWeakTable<Unit, Launcher> Launchers = new ConditionalWeakTable<Unit, Launcher>();
        private static Budget Get(Unit owner, Unit target)
        {
            var budget = Launchers.GetOrCreateValue(owner).Targets.GetOrCreateValue(target);
            if (Time.time - budget.Last > 45f) budget.Shots = 0;
            return budget;
        }
        private static bool Applies(Unit owner, Unit target, WeaponInfo info) => RevertToStoneAgePlugin.DefenseRolesEnabled.Value
            && owner != null && owner.LocalSim && (owner is GroundVehicle || owner is Ship) && info != null && info.missile && target != null;
        private static bool Prefix(MissileLauncher __instance, Unit owner, Unit target, out int __state)
        {
            __state = __instance.ammo;
            if (!Applies(owner, target, __instance.info)) return true;
            if (!DefenseIntegration.Eligible(owner, target, __instance.info)) return false;
            int limit = RevertToStoneAgePlugin.TerminalLaunchLimit.Value;
            return limit == 0 || !DefenseIntegration.Terminal(target) || DefensePhysics.HasLaunchBudget(Get(owner, target).Shots, limit);
        }
        private static void Postfix(MissileLauncher __instance, Unit owner, Unit target, int __state)
        {
            if (!Applies(owner, target, __instance.info) || !DefenseIntegration.Terminal(target)) return;
            int spent = __state - __instance.ammo;
            if (spent <= 0) return; // Rejected/unready shots never consume budget.
            var budget = Get(owner, target);
            budget.Shots = DefensePhysics.CountConsumedRounds(budget.Shots, __state, __instance.ammo); budget.Last = Time.time;
        }
    }
}
