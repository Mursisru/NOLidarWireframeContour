using UnityEngine;



namespace NOLoader.LidarWireframeContour

{

    public sealed class ACT_LidarCollisionController : MonoBehaviour

    {

        internal static ACT_LidarCollisionController? Instance { get; private set; }



        private const int MissStreakToDeactivate = 4;

        private const float HoldAfterHitSec = 0.8f;

        private const float HoldAfterLowTtiSec = 1.5f;

        private const float LowTtiHoldThresholdSec = 1f;



        private bool _wantsActive;

        private float _effectBlend;

        private float _holdTimer;

        private int _missStreak;



        private float _lastTti = 99f;

        private float _lastDist;

        private Vector3 _lastScanOrigin;

        private Vector3 _lastLidarDir = Vector3.forward;



        private static RaycastHit s_aglHit;



        internal void OnAircraftSet(Aircraft? aircraft)

        {

            ResetProbeState();

            LidarPostProcess.PushBlend(0f);

            LidarPostProcess.SetShaderActive(false);

        }



        private void Awake()

        {

            Instance = this;

        }



        private void OnDestroy()

        {

            if (Instance == this)

                Instance = null;



            LidarPostProcess.OnControllerDestroyed();
            LidarPostProcess.SetCombatActive(false);

        }



        internal void FadeTick(float dt)

        {

            if (!LidarConfig.Enabled)

            {

                ResetEffect();

                return;

            }



            if (_holdTimer > 0f)

                _holdTimer = Mathf.Max(0f, _holdTimer - dt);



            bool shouldShow = _wantsActive || _holdTimer > 0f;

            if (shouldShow)

                PushCachedUniforms();



            float fadeSpeed = 1f / Mathf.Max(0.05f, LidarConfig.FadeOutSec);

            float target = shouldShow ? 1f : 0f;

            _effectBlend = Mathf.MoveTowards(_effectBlend, target, fadeSpeed * dt);



            LidarPostProcess.PushBlend(_effectBlend);

            LidarPostProcess.SetCombatActive(shouldShow);



            if (_effectBlend <= 0.001f && !shouldShow)

                LidarPostProcess.SetShaderActive(false);

        }



        internal void ProbeTick()

        {

            if (!LidarConfig.Enabled)

            {

                DeactivateProbe("disabled", 0f, 0f);

                return;

            }



            if (!CanRun(out Aircraft aircraft, out Rigidbody rb))

            {

                if (_holdTimer > 0f)

                {

                    LogProbe("can_run_hold", 0f, _lastTti, true);

                    return;

                }



                DeactivateProbe("can_run_false", 0f, 0f);

                return;

            }



            Vector3 velocity = rb.velocity;

            float speed = velocity.magnitude;

            if (speed < LidarConfig.MinSpeedMps)

            {

                if (_holdTimer > 0f)

                {

                    LogProbe("low_speed_hold", speed, _lastTti, true);

                    return;

                }



                DeactivateProbe("low_speed", speed, 0f);

                return;

            }



            Vector3 probeOrigin = rb.position;

            float agl = 0f;

            TryGetAglMeters(probeOrigin, out agl);



            if (agl >= LidarConfig.SafeAglMeters && agl > 0.01f)

            {

                DeactivateProbe("safe_agl", speed, agl);

                return;

            }



            if (!LidarSphereCastProbe.TryEvaluate(

                    probeOrigin,

                    velocity,

                    speed,

                    LidarConfig.TerrainLayerMask,

                    LidarConfig.CastMaxDistanceM,

                    LidarConfig.CastRadiusNearM,

                    LidarConfig.CastRadiusFarM,

                    out LidarProbeResult probe))

            {

                RegisterMiss("no_cast_hit", speed, agl);

                return;

            }



            if (!probe.HasHit || probe.TimeToImpactSeconds > LidarConfig.TtiActivateSec)

            {

                RegisterMiss("tti_too_high", speed, probe.TimeToImpactSeconds);

                return;

            }



            _missStreak = 0;

            _holdTimer = probe.TimeToImpactSeconds < LowTtiHoldThresholdSec

                ? HoldAfterLowTtiSec

                : HoldAfterHitSec;

            _wantsActive = true;

            _lastTti = probe.TimeToImpactSeconds;

            _lastDist = probe.DistanceMeters;

            _lastScanOrigin = probe.ScanOrigin;

            _lastLidarDir = probe.ScanDirection;



            PushCachedUniforms();

            LogProbe("activated", speed, probe.TimeToImpactSeconds, true);

        }



        private void RegisterMiss(string reason, float speed, float metric)

        {

            if (_wantsActive || _holdTimer > 0f)

            {

                _missStreak++;

                if (_missStreak >= MissStreakToDeactivate && _holdTimer <= 0f)

                {

                    _wantsActive = false;

                    LogProbe(reason + "_off", speed, metric, false);

                    return;

                }



                LogProbe(reason + "_hold", speed, metric, true);

                return;

            }



            _wantsActive = false;

            LogProbe(reason, speed, metric, false);

        }



        private void DeactivateProbe(string reason, float speed, float metric)

        {

            _missStreak = 0;

            _holdTimer = 0f;

            _wantsActive = false;

            LogProbe(reason, speed, metric, false);

        }



        private void PushCachedUniforms()

        {

            LidarPostProcess.PushProbeUniforms(

                _lastDist,

                _lastTti,

                _lastScanOrigin,

                _lastLidarDir);

        }



        private void ResetProbeState()

        {

            _wantsActive = false;

            _effectBlend = 0f;

            _holdTimer = 0f;

            _missStreak = 0;

            _lastTti = 99f;

            _lastDist = 0f;

            _lastLidarDir = Vector3.forward;

        }



        private static float s_lastProbeLogTime = -999f;



        private void LogProbe(string reason, float speed, float metric, bool active)

        {

            // #region agent log

            if (Time.unscaledTime - s_lastProbeLogTime < 0.4f)

                return;



            s_lastProbeLogTime = Time.unscaledTime;

            LidarDebugLog.Write("B", "ACT_LidarCollisionController.ProbeTick", "probe_state", d =>

            {

                d.Append("\"reason\":\"").Append(reason.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('\"');

                d.Append(',');

                d.Append("\"speed\":").Append(speed.ToString("F1"));

                d.Append(',');

                d.Append("\"metric\":").Append(metric.ToString("F1"));

                d.Append(',');

                d.Append("\"wantsActive\":").Append(active ? "true" : "false");

                d.Append(',');

                d.Append("\"holdTimer\":").Append(_holdTimer.ToString("F2"));

                d.Append(',');

                d.Append("\"missStreak\":").Append(_missStreak);

                d.Append(',');

                d.Append("\"blend\":").Append(_effectBlend.ToString("F3"));

            });

            // #endregion

        }



        private static bool CanRun(out Aircraft aircraft, out Rigidbody rb)

        {

            aircraft = null!;

            rb = null!;



            GameState state = GameManager.gameState;

            if (state != GameState.SinglePlayer && state != GameState.Multiplayer)

                return false;



            if (!GameManager.flightControlsEnabled)

                return false;



            if (!GameManager.GetLocalAircraft(out aircraft) || aircraft == null || aircraft.disabled)

                return false;



            if (aircraft.cockpit == null)

                return false;



            rb = aircraft.cockpit.rb;

            if (rb == null)

                return false;



            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;

            if (csm == null || csm.mainCamera == null)

                return false;



            if (csm.currentState != csm.cockpitState)

                return false;



            FlightHud? fh = SceneSingleton<FlightHud>.i;

            if (fh == null)

                return false;



            Canvas? flightCanvas = fh.GetComponent<Canvas>();

            if (flightCanvas != null && !flightCanvas.gameObject.activeSelf)

                return false;



            return true;

        }



        private static bool TryGetAglMeters(Vector3 position, out float aglMeters)

        {

            aglMeters = 0f;

            if (!Physics.Raycast(

                    position,

                    Vector3.down,

                    out s_aglHit,

                    100000f,

                    LidarConfig.TerrainLayerMask,

                    QueryTriggerInteraction.Ignore))

            {

                return false;

            }



            aglMeters = position.y - s_aglHit.point.y;

            return true;

        }



        private void ResetEffect()

        {

            ResetProbeState();

            LidarPostProcess.PushBlend(0f);

            LidarPostProcess.SetShaderActive(false);

        }

    }

}


