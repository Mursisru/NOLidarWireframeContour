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
        private const float MinObstacleDistanceM = 12f;
        private const float ProbeNearTtiMarginSec = 3f;

        private float _appearBootElapsed = -1f;

        private bool _wantsActive;
        private bool _wasCombatActive;
        private bool _wasShowing;
        private bool _hasProbeTrack;
        private float _effectBlend;
        private float _holdTimer;
        private int _missStreak;

        private float _targetTti = 99f;
        private float _targetDist;
        private Vector3 _targetLidarDir = Vector3.forward;

        private float _smoothTti = 99f;
        private float _smoothDist;
        private Vector3 _smoothLidarDir = Vector3.forward;

        private static RaycastHit s_aglHit;

        internal float LastEstimatedTti => _hasProbeTrack ? _targetTti : 99f;

        internal float GetProbeIntervalSec()
        {
            if (!_hasProbeTrack)
                return LidarConfig.ProbeIntervalSec;

            float margin = ProbeNearTtiMarginSec;
            if (_targetTti <= LidarConfig.TtiActivateSec + margin || _wantsActive || _holdTimer > 0f)
                return LidarConfig.ProbeIntervalNearSec;

            return LidarConfig.ProbeIntervalSec;
        }

        internal void OnAircraftSet(Aircraft? aircraft)
        {
            ResetProbeState();
            LidarPostProcess.PushAppearBoot(-1f);
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

            if (TryGetFlightBody(out _, out Rigidbody rb))
                UpdateApproachEstimate(dt, rb);

            bool shouldShow = _wantsActive || _holdTimer > 0f;
            bool booting = _appearBootElapsed >= 0f && _appearBootElapsed < LidarConfig.AppearBootSec;

            if (_wantsActive && !_wasCombatActive)
                _appearBootElapsed = 0f;

            if (!shouldShow)
                _appearBootElapsed = -1f;
            else if (booting)
                _appearBootElapsed += dt;

            LidarPostProcess.PushAppearBoot(_appearBootElapsed);

            if (shouldShow)
            {
                if (!_wasShowing && _hasProbeTrack)
                    SnapSmoothUniforms();

                SmoothUniforms(dt);
                PushSmoothedUniforms();
            }

            float fadeInSec = ResolveFadeInSec();
            float fadeSpeed = shouldShow
                ? 1f / Mathf.Max(0.05f, fadeInSec)
                : 1f / Mathf.Max(0.05f, LidarConfig.FadeOutSec);

            float target = shouldShow ? 1f : 0f;
            if (booting)
                _effectBlend = 1f;
            else
                _effectBlend = Mathf.MoveTowards(_effectBlend, target, fadeSpeed * dt);

            LidarPostProcess.PushBlend(_effectBlend);
            LidarPostProcess.SetCombatActive(shouldShow);

            if (_effectBlend <= 0.001f && !shouldShow)
                LidarPostProcess.SetShaderActive(false);

            _wasShowing = shouldShow;
            _wasCombatActive = _wantsActive;
        }

        internal void ProbeTick()
        {
            if (!LidarConfig.Enabled)
            {
                DeactivateProbe("disabled", 0f, 0f);
                return;
            }

            if (!TryGetFlightBody(out Aircraft aircraft, out Rigidbody rb))
            {
                if (_holdTimer > 0f)
                {
                    LogProbe("can_run_hold", 0f, _targetTti, true);
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
                    LogProbe("low_speed_hold", speed, _targetTti, true);
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

            if (!probe.HasHit)
            {
                RegisterMiss("tti_too_high", speed, probe.TimeToImpactSeconds);
                return;
            }

            ApplyProbeResult(probe, speed);

            if (probe.TimeToImpactSeconds > LidarConfig.TtiActivateSec)
            {
                RegisterMiss("tti_too_high", speed, probe.TimeToImpactSeconds);
                return;
            }

            ActivateFromProbe(probe.TimeToImpactSeconds, speed);
        }

        private void ApplyProbeResult(LidarProbeResult probe, float speed)
        {
            _hasProbeTrack = true;
            _targetTti = probe.TimeToImpactSeconds;
            _targetDist = probe.DistanceMeters;
            _targetLidarDir = probe.ScanDirection.sqrMagnitude > 1e-6f
                ? probe.ScanDirection.normalized
                : Vector3.forward;
        }

        private void ActivateFromProbe(float tti, float speed)
        {
            _missStreak = 0;
            _holdTimer = tti < LowTtiHoldThresholdSec ? HoldAfterLowTtiSec : HoldAfterHitSec;
            _wantsActive = true;
            LogProbe("activated", speed, tti, true);
        }

        private void UpdateApproachEstimate(float dt, Rigidbody rb)
        {
            if (!_hasProbeTrack)
                return;

            Vector3 velocity = rb.velocity;
            float speed = velocity.magnitude;
            if (speed < LidarConfig.MinSpeedMps)
                return;

            Vector3 velDir = velocity / speed;
            float closingSpeed = Mathf.Max(0f, Vector3.Dot(velocity, _targetLidarDir));
            _targetDist = Mathf.Max(MinObstacleDistanceM, _targetDist - closingSpeed * dt);
            _targetTti = _targetDist / speed;
            _targetLidarDir = velDir;

            if (!_wantsActive && _holdTimer <= 0f && _targetTti <= LidarConfig.TtiActivateSec)
                ActivateFromProbe(_targetTti, speed);
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
            _hasProbeTrack = false;
            LogProbe(reason, speed, metric, false);
        }

        private void SnapSmoothUniforms()
        {
            _smoothDist = _targetDist;
            _smoothTti = _targetTti;
            _smoothLidarDir = _targetLidarDir.sqrMagnitude > 1e-6f ? _targetLidarDir : Vector3.forward;
        }

        private void SmoothUniforms(float dt)
        {
            float tau = Mathf.Max(0.03f, LidarConfig.UniformSmoothSec);
            float k = 1f - Mathf.Exp(-dt / tau);

            _smoothDist = Mathf.Lerp(_smoothDist, _targetDist, k);
            _smoothTti = Mathf.Lerp(_smoothTti, _targetTti, k);
            _smoothLidarDir = Vector3.Slerp(_smoothLidarDir, _targetLidarDir, k).normalized;
        }

        private void PushSmoothedUniforms()
        {
            LidarPostProcess.PushProbeUniforms(
                _smoothDist,
                _smoothTti,
                Vector3.zero,
                _smoothLidarDir);
        }

        private float ResolveFadeInSec()
        {
            if (_smoothTti <= LidarConfig.TtiActivateSec * 0.65f)
                return Mathf.Min(LidarConfig.FadeInSec, LidarConfig.FadeInUrgentSec);

            return LidarConfig.FadeInSec;
        }

        private void ResetProbeState()
        {
            _wantsActive = false;
            _wasCombatActive = false;
            _wasShowing = false;
            _hasProbeTrack = false;
            _effectBlend = 0f;
            _holdTimer = 0f;
            _missStreak = 0;
            _appearBootElapsed = -1f;
            _targetTti = 99f;
            _targetDist = 0f;
            _targetLidarDir = Vector3.forward;
            _smoothTti = 99f;
            _smoothDist = 0f;
            _smoothLidarDir = Vector3.forward;
        }

        private static float s_lastProbeLogTime = -999f;

        private void LogProbe(string reason, float speed, float metric, bool active)
        {
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
        }

        private static bool TryGetFlightBody(out Aircraft aircraft, out Rigidbody rb)
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
            LidarPostProcess.PushAppearBoot(-1f);
            LidarPostProcess.PushBlend(0f);
            LidarPostProcess.SetShaderActive(false);
        }
    }
}
