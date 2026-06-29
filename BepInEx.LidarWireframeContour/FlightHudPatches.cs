using HarmonyLib;
using NOLoader.LidarWireframeContour;
using UnityEngine;

namespace LidarWireframeContour.BepInEx
{
    [HarmonyPatch(typeof(FlightHud), "Awake")]
    internal static class FlightHudAwakePatch
    {
        private static void Postfix(FlightHud __instance)
        {
            if (__instance == null)
                return;

            if (__instance.GetComponent<ACT_LidarCollisionController>() == null)
                __instance.gameObject.AddComponent<ACT_LidarCollisionController>();

            // #region agent log
            LidarDebugLog.Audit("H1", "FlightHudAwakePatch.Postfix", "patch_awake", d =>
            {
                d.Append("\"hasCtrl\":").Append(__instance.GetComponent<ACT_LidarCollisionController>() != null ? "true" : "false");
            });
            // #endregion
        }
    }

    [HarmonyPatch(typeof(FlightHud), "SetAircraft")]
    internal static class FlightHudSetAircraftPatch
    {
        private static void Postfix(FlightHud __instance, Aircraft aircraft)
        {
            if (__instance == null)
                return;

            ACT_LidarCollisionController? ctrl = __instance.GetComponent<ACT_LidarCollisionController>();
            if (ctrl == null)
                ctrl = __instance.gameObject.AddComponent<ACT_LidarCollisionController>();

            ctrl.OnAircraftSet(aircraft);

            // #region agent log
            LidarDebugLog.Audit("H1", "FlightHudSetAircraftPatch.Postfix", "patch_set_aircraft", d =>
            {
                d.Append("\"aircraftNull\":").Append(aircraft == null ? "true" : "false");
            });
            // #endregion
        }
    }
}
