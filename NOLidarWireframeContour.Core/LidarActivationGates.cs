using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarActivationGates
    {
        internal static bool IsGearBlocking(Aircraft? aircraft)
        {
            if (!LidarConfig.BlockWhenGearDeployed || aircraft == null)
                return false;

            if (aircraft.gearDeployed)
                return true;

            LandingGear.GearState state = aircraft.gearState;
            return state == LandingGear.GearState.LockedExtended
                || state == LandingGear.GearState.Extending;
        }

        internal static bool IsDaytimeBlocking()
        {
            if (!LidarConfig.BlockDuringDaytime)
                return false;

            LevelInfo? level = NetworkSceneSingleton<LevelInfo>.i;
            if (level == null)
                return false;

            float hour = level.timeOfDay;
            return hour >= LidarConfig.DaytimeStartHour && hour <= LidarConfig.DaytimeEndHour;
        }

        internal static bool IsAutoActivationBlocked(Aircraft? aircraft, bool forceNightMode)
        {
            if (forceNightMode)
                return false;

            return IsGearBlocking(aircraft) || IsDaytimeBlocking();
        }
    }
}
