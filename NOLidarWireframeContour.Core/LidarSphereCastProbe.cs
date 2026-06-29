using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    internal struct LidarProbeResult
    {
        public bool HasHit;
        public float DistanceMeters;
        public float TimeToImpactSeconds;
        public Vector3 HitPoint;
        public Vector3 ScanOrigin;
        public Vector3 ScanDirection;
    }

    /// <summary>
    /// Двухступенчатый SphereCast по вектору скорости. Без аллокаций в горячем пути.
    /// </summary>
    internal static class LidarSphereCastProbe
    {
        private static RaycastHit s_hit;
        private const float MinObstacleDistanceM = 12f;

        internal static bool TryEvaluate(
            Vector3 aircraftPosition,
            Vector3 velocity,
            float speedMps,
            int layerMask,
            float maxDistance,
            float nearRadius,
            float farRadius,
            out LidarProbeResult result)
        {
            result = default;
            if (speedMps < 0.01f)
                return false;

            Vector3 dir = velocity / speedMps;
            result.ScanOrigin = aircraftPosition;
            result.ScanDirection = dir;

            Vector3 cast1Origin = aircraftPosition + dir * nearRadius;
            float cast1MaxDist = maxDistance - nearRadius;
            if (cast1MaxDist > 0.01f
                && Physics.SphereCast(
                    cast1Origin,
                    nearRadius,
                    dir,
                    out s_hit,
                    cast1MaxDist,
                    layerMask,
                    QueryTriggerInteraction.Ignore)
                && TryAcceptHit(s_hit, nearRadius, speedMps, ref result))
            {
                return true;
            }

            Vector3 cast2Origin = aircraftPosition + dir * farRadius;
            float cast2MaxDist = maxDistance - farRadius;
            if (cast2MaxDist <= 0.01f)
                return false;

            if (Physics.SphereCast(
                    cast2Origin,
                    farRadius,
                    dir,
                    out s_hit,
                    cast2MaxDist,
                    layerMask,
                    QueryTriggerInteraction.Ignore))
            {
                float totalDistance = farRadius + s_hit.distance;
                if (totalDistance < MinObstacleDistanceM)
                    return false;

                result.HasHit = true;
                result.DistanceMeters = totalDistance;
                result.HitPoint = s_hit.point;
                result.TimeToImpactSeconds = totalDistance / speedMps;
                return true;
            }

            return false;
        }

        private static bool TryAcceptHit(
            RaycastHit hit,
            float originOffset,
            float speedMps,
            ref LidarProbeResult result)
        {
            float totalDistance = originOffset + hit.distance;
            if (totalDistance < MinObstacleDistanceM)
                return false;

            result.HasHit = true;
            result.DistanceMeters = totalDistance;
            result.HitPoint = hit.point;
            result.TimeToImpactSeconds = totalDistance / speedMps;
            return true;
        }
    }
}
