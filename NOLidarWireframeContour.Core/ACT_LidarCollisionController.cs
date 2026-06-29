using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    public sealed class ACT_LidarCollisionController : MonoBehaviour
    {
        internal static ACT_LidarCollisionController? Instance { get; private set; }

        private const int MissStreakToDeactivate = 3;
        private const float HoldAfterHitSec = 0.8f;
        private const float HoldAfterLowTtiSec = 1.5f;
        private const float LowTtiHoldThresholdSec = 1f;
        private const float MinObstacleDistanceM = 12f;
        private const float ProbeNearTtiMarginSec = 3f;
        private const float ProbeDirBlend = 0.28f;
        private const float ExtrapolateDirBlend = 0.12f;

        private Rigidbody? _flightRb;

        private bool _wantsActive;
        private bool _forceNightMode;
        private bool _wasShowing;
        private bool _hasProbeTrack;
        private float _holdTimer;
        private int _missStreak;
        private int _lastVisualUpdateFrame = -1;

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
            if (_targetTti <= LidarConfig.TtiActivateSec + margin || _wantsActive || _forceNightMode)
                return LidarConfig.ProbeIntervalNearSec;

            return LidarConfig.ProbeIntervalSec;
        }

        internal void OnAircraftSet(Aircraft? aircraft)
        {
            ResetProbeState();
            LidarPostProcess.ResetGpuState();
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
            LidarPostProcess.ResetGpuState();
        }

        private void Update()
        {
            if (!LidarConfig.Enabled)
                return;

            PollForceHotkey();
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

            if (shouldShow && !_wasShowing)
                LidarPostProcess.TryBeginCombatVisual(ResolveFadeInSec());
            else if (!shouldShow && _wasShowing)
                LidarPostProcess.PushCombatEnd(Time.time);

            LidarPostProcess.SetCombatShowing(shouldShow);

            if (!shouldShow && LidarPostProcess.IsFadeOutComplete(Time.time))
            {
                LidarPostProcess.SetGpuGate(false);
                LidarPostProcess.ClearCombatTimes();
            }

            _wasShowing = shouldShow;
        }

        internal void VisualUpdate(float dt)
        {
            if (!LidarConfig.Enabled)
                return;

            if (Time.frameCount == _lastVisualUpdateFrame)
                return;

            _lastVisualUpdateFrame = Time.frameCount;

            bool shouldShow = _wantsActive || _holdTimer > 0f;
            if (!shouldShow)
                return;

            bool needsExtrapolation = _hasProbeTrack
                && (_wantsActive || _forceNightMode || _targetTti <= LidarConfig.TtiActivateSec + ProbeNearTtiMarginSec);
            if (needsExtrapolation && _flightRb != null)
                UpdateApproachEstimate(dt, _flightRb);

            SmoothUniforms(dt);
            PushSmoothedUniforms();
        }

        internal void PollForceHotkey()
        {
            if (!LidarForceHotkey.TryConsumePress())
                return;

            _forceNightMode = !_forceNightMode;
            _missStreak = 0;

            if (_forceNightMode)
            {
                if (TryGetFlightBody(out _, out _))
                    ProbeTick();

                return;
            }

            if (TryGetLocalAircraft(out Aircraft aircraft)
                && LidarActivationGates.IsAutoActivationBlocked(aircraft, false))
            {
                DropVisualAfterStaticBlock();
            }
        }

        private void DropVisualAfterStaticBlock()
        {
            _wantsActive = false;
            _holdTimer = 0f;
            _missStreak = 0;

            if (!_wasShowing)
                return;

            LidarPostProcess.PushCombatEnd(Time.time);
            LidarPostProcess.SetCombatShowing(false);
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
                _flightRb = null;

                if (_holdTimer > 0f)
                {
                    LogProbe("can_run_hold", 0f, _targetTti, true);
                    return;
                }

                // #region agent log
                LidarDebugLog.Audit("H2", "ACT_LidarCollisionController.ProbeTick", "can_run_false", d =>
                {
                    d.Append("\"reason\":\"").Append(DiagnoseFlightBodyBlock().Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('\"');
                }, 2f);
                // #endregion

                DeactivateProbe("can_run_false", 0f, 0f);
                return;
            }

            _flightRb = rb;

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

            if (!_forceNightMode && LidarActivationGates.IsAutoActivationBlocked(aircraft, false))
            {
                // #region agent log
                LidarDebugLog.Audit("H3", "ACT_LidarCollisionController.ProbeTick", "gates_blocked", d =>
                {
                    d.Append("\"gear\":").Append(LidarActivationGates.IsGearBlocking(aircraft) ? "true" : "false");
                    d.Append(',');
                    d.Append("\"day\":").Append(LidarActivationGates.IsDaytimeBlocking() ? "true" : "false");
                    d.Append(',');
                    d.Append("\"forceNight\":").Append(_forceNightMode ? "true" : "false");
                }, 2f);
                // #endregion

                if (_wantsActive)
                    DeactivateProbe("gates_blocked", speed, 0f);

                return;
            }

            Vector3 probeOrigin = rb.position;
            if (!(_wantsActive && _hasProbeTrack))
            {
                float agl = 0f;
                TryGetAglMeters(probeOrigin, out agl);

                if (agl >= LidarConfig.SafeAglMeters && agl > 0.01f)
                {
                    DeactivateProbe("safe_agl", speed, agl);
                    return;
                }
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
                RegisterMiss("no_cast_hit", speed, 0f);
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

            ActivateFromProbe(aircraft, probe.TimeToImpactSeconds, speed);
        }

        private void ApplyProbeResult(LidarProbeResult probe, float speed)
        {
            _hasProbeTrack = true;
            _targetTti = probe.TimeToImpactSeconds;
            _targetDist = probe.DistanceMeters;

            Vector3 newDir = probe.ScanDirection.sqrMagnitude > 1e-6f
                ? probe.ScanDirection.normalized
                : Vector3.forward;
            _targetLidarDir = BlendDirection(_targetLidarDir, newDir, ProbeDirBlend);

            if (_targetTti <= LidarConfig.TtiActivateSec + ProbeNearTtiMarginSec)
                LidarPostProcess.ArmMainCameraDepth();
        }

        private void ActivateFromProbe(Aircraft aircraft, float tti, float speed)
        {
            if (LidarActivationGates.IsAutoActivationBlocked(aircraft, _forceNightMode))
                return;

            _missStreak = 0;
            _holdTimer = tti < LowTtiHoldThresholdSec ? HoldAfterLowTtiSec : HoldAfterHitSec;
            _wantsActive = true;
            // #region agent log
            LidarDebugLog.Audit("H3", "ACT_LidarCollisionController.ActivateFromProbe", "activated", d =>
            {
                d.Append("\"tti\":").Append(tti.ToString("F2"));
                d.Append(',');
                d.Append("\"speed\":").Append(speed.ToString("F1"));
            });
            // #endregion
            SnapSmoothScalars();
            PushSmoothedUniforms();
            LidarPostProcess.TryBeginCombatVisual(ResolveFadeInSec());
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
            _targetLidarDir = BlendDirection(_targetLidarDir, velDir, ExtrapolateDirBlend);

            if (!_wantsActive && _holdTimer <= 0f && _targetTti <= LidarConfig.TtiActivateSec
                && TryGetFlightBody(out Aircraft aircraft, out _)
                && (_forceNightMode || !LidarActivationGates.IsAutoActivationBlocked(aircraft, false)))
            {
                ActivateFromProbe(aircraft, _targetTti, speed);
            }
        }

        private void RegisterMiss(string reason, float speed, float metric)
        {
            if (_wantsActive || _holdTimer > 0f)
            {
                if (_holdTimer > 0f && !_wantsActive)
                {
                    LogProbe(reason + "_hold_only", speed, metric, true);
                    return;
                }

                _missStreak++;
                if (_missStreak >= MissStreakToDeactivate)
                {
                    _wantsActive = false;
                    _holdTimer = Mathf.Max(_holdTimer, LidarConfig.HoldAfterEscapeSec);
                    _missStreak = 0;
                    LogProbe(reason + "_escape_hold", speed, metric, true);
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
            if (_wantsActive || _holdTimer > 0f)
            {
                _wantsActive = false;
                if (!_forceNightMode)
                    _holdTimer = Mathf.Max(_holdTimer, LidarConfig.HoldAfterEscapeSec);

                _missStreak = 0;
                LogProbe(reason + "_escape_hold", speed, metric, true);
                return;
            }

            _missStreak = 0;
            _holdTimer = 0f;
            _wantsActive = false;
            _hasProbeTrack = false;
            LogProbe(reason, speed, metric, false);
        }

        private static Vector3 BlendDirection(Vector3 from, Vector3 to, float t)
        {
            if (from.sqrMagnitude < 1e-6f)
                return to.sqrMagnitude > 1e-6f ? to.normalized : Vector3.forward;

            if (to.sqrMagnitude < 1e-6f)
                return from.normalized;

            return Vector3.Slerp(from.normalized, to.normalized, Mathf.Clamp01(t)).normalized;
        }

        private void SnapSmoothScalars()
        {
            _smoothDist = _targetDist;
            _smoothTti = _targetTti;
        }

        private void SmoothUniforms(float dt)
        {
            float tau = Mathf.Max(0.05f, LidarConfig.UniformSmoothSec);
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
            _forceNightMode = false;
            _wasShowing = false;
            _hasProbeTrack = false;
            _holdTimer = 0f;
            _missStreak = 0;
            _lastVisualUpdateFrame = -1;
            _flightRb = null;
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
            });
        }

        private static bool TryGetLocalAircraft(out Aircraft aircraft)
        {
            aircraft = null!;

            GameState state = GameManager.gameState;
            if (state != GameState.SinglePlayer && state != GameState.Multiplayer)
                return false;

            if (!GameManager.GetLocalAircraft(out aircraft) || aircraft == null || aircraft.disabled)
                return false;

            return true;
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

        private static string DiagnoseFlightBodyBlock()
        {
            GameState state = GameManager.gameState;
            if (state != GameState.SinglePlayer && state != GameState.Multiplayer)
                return "game_state_" + state;

            if (!GameManager.flightControlsEnabled)
                return "flight_controls_disabled";

            if (!GameManager.GetLocalAircraft(out Aircraft aircraft) || aircraft == null)
                return "no_local_aircraft";

            if (aircraft.disabled)
                return "aircraft_disabled";

            if (aircraft.cockpit == null)
                return "cockpit_null";

            if (aircraft.cockpit.rb == null)
                return "cockpit_rb_null";

            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
            if (csm == null)
                return "csm_null";

            if (csm.mainCamera == null)
                return "main_camera_null";

            if (csm.currentState != csm.cockpitState)
                return "not_cockpit_camera";

            FlightHud? fh = SceneSingleton<FlightHud>.i;
            if (fh == null)
                return "flight_hud_null";

            Canvas? flightCanvas = fh.GetComponent<Canvas>();
            if (flightCanvas != null && !flightCanvas.gameObject.activeSelf)
                return "flight_hud_canvas_inactive";

            return "unknown";
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
            LidarPostProcess.ResetGpuState();
        }
    }
}
