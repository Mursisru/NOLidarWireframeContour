using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal static class Patches
    {
        public static void AwakePostfix(FlightHud __instance)
        {
            if (__instance == null)
                return;

            if (__instance.GetComponent<ACT_LidarCollisionController>() == null)
                __instance.gameObject.AddComponent<ACT_LidarCollisionController>();
        }

        public static void SetAircraftPostfix(FlightHud __instance, Aircraft aircraft)
        {
            if (__instance == null)
                return;

            ACT_LidarCollisionController? ctrl = __instance.GetComponent<ACT_LidarCollisionController>();
            if (ctrl == null)
                ctrl = __instance.gameObject.AddComponent<ACT_LidarCollisionController>();

            ctrl.OnAircraftSet(aircraft);
        }

        internal static void EnsureController()
        {
            FlightHud? fh = SceneSingleton<FlightHud>.i;
            if (fh == null)
                return;

            if (fh.GetComponent<ACT_LidarCollisionController>() == null)
                fh.gameObject.AddComponent<ACT_LidarCollisionController>();
        }
    }
}
