using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NOLoader.LidarWireframeContour
{
    internal static class LidarPostProcess
    {
        private static bool _hooked;
        private static Material? _material;
        private static readonly LidarDepthCapturePass s_depthPass = new LidarDepthCapturePass();
        private static readonly LidarWireframeRenderPass s_pass = new LidarWireframeRenderPass();
        private static Camera? _boundCamera;
        private static bool _depthCurrentlyRequired;
        private static bool _loggedEnqueue;
        private static float _lastRejectLogTime = -999f;
        private static int _dedupeFrame = -1;
        private static readonly HashSet<int> s_enqueuedCamerasThisFrame = new HashSet<int>();

        private static float _effectBlend;
        private static float _appearBootElapsed = -1f;
        private static float _impactDistance;
        private static float _timeToImpact = 99f;
        private static Vector3 _lidarDirection = Vector3.forward;
        private static string _lastEnqueueReject = "ok";
        private static string? _modRoot;
        private static float _lastLoggedDebugForce = -1f;
        private static int _lastLoggedShaderMode = -1;
        private static bool _combatActive;
        private static float _lastCameraDiscoveryLog = -999f;

        internal static float EffectBlend => _effectBlend;
        internal static bool CombatActive => _combatActive;
        internal static string? ModRoot => _modRoot;
        internal static string LastEnqueueReject => _lastEnqueueReject;
        internal static LidarDepthCapturePass DepthCapturePass => s_depthPass;

        internal static void SetCombatActive(bool active) => _combatActive = active;

        internal static void SetModRoot(string modRoot)
        {
            _modRoot = modRoot;
        }

        internal static void TryReloadConfig()
        {
            if (string.IsNullOrEmpty(_modRoot))
                return;

            float prevForce = LidarConfig.DebugForceBlend;
            int prevMode = LidarConfig.DebugShaderMode;
            LidarConfig.Reload(_modRoot!);

            bool firstLog = _lastLoggedDebugForce < 0f;
            bool changed = firstLog
                || Mathf.Abs(prevForce - LidarConfig.DebugForceBlend) > 0.0001f
                || prevMode != LidarConfig.DebugShaderMode;

            if (!changed)
                return;

            _lastLoggedDebugForce = LidarConfig.DebugForceBlend;
            _lastLoggedShaderMode = LidarConfig.DebugShaderMode;

            // #region agent log
            LidarDebugLog.Write("H1", "LidarPostProcess.TryReloadConfig", "config_reloaded", d =>
            {
                d.Append("\"modRoot\":\"").Append(EscapeDbg(_modRoot!)).Append('\"');
                d.Append(',');
                d.Append("\"iniPath\":\"").Append(EscapeDbg(System.IO.Path.Combine(_modRoot!, "mod_config.ini"))).Append('\"');
                d.Append(',');
                d.Append("\"debugForce\":").Append(LidarConfig.DebugForceBlend.ToString("F3"));
                d.Append(',');
                d.Append("\"shaderMode\":").Append(LidarConfig.DebugShaderMode);
                d.Append(',');
                d.Append("\"outputCamera\":\"").Append(EscapeDbg(LidarConfig.OutputCameraName)).Append('\"');
            });
            // #endregion
        }

        internal static void EnsurePipelineHook()
        {
            if (_hooked)
                return;

            _hooked = true;
            s_depthPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            s_pass.ConfigureInput(ScriptableRenderPassInput.Depth);
            s_pass.SetDepthCapturePass(s_depthPass);
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;

            if (LidarConfig.ForceKeepDepthTextureActive)
                TryBindMainCameraDepth();
        }

        private static void TryBindMainCameraDepth()
        {
            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
            if (csm == null || csm.mainCamera == null)
                return;

            _boundCamera = csm.mainCamera;
            ApplyDepthPolicy(true);
        }

        internal static void Shutdown()
        {
            if (!_hooked)
                return;

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            _hooked = false;

            ReleaseGpuResources();
            _boundCamera = null;
            _loggedEnqueue = false;
            _lastEnqueueReject = "ok";
            s_enqueuedCamerasThisFrame.Clear();
        }

        internal static void OnControllerDestroyed()
        {
            ReleaseIdleResources();
            _boundCamera = null;
        }

        internal static void PushProbeUniforms(
            float impactDistanceM,
            float timeToImpact,
            Vector3 origin,
            Vector3 direction)
        {
            _impactDistance = impactDistanceM;
            _timeToImpact = timeToImpact;
            _lidarDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        internal static void PushAppearBoot(float elapsedSec)
        {
            if (Mathf.Abs(_appearBootElapsed - elapsedSec) < 0.0001f)
                return;

            _appearBootElapsed = elapsedSec;
        }

        internal static void PushBlend(float blend)
        {
            blend = Mathf.Clamp01(blend);
            if (Mathf.Abs(_effectBlend - blend) < 0.0001f)
                return;

            float prev = _effectBlend;
            _effectBlend = blend;
            ApplyDepthPolicy(ShouldKeepDepthActive());

            if (prev > 0.001f && _effectBlend <= 0.001f)
                ReleaseIdleResources();

            if (Mathf.Abs(prev - _effectBlend) > 0.05f)
            {
                // #region agent log
                LidarDebugLog.Write("B", "LidarPostProcess.PushBlend", "blend_changed", d =>
                {
                    d.Append("\"prev\":").Append(prev.ToString("F3"));
                    d.Append(',');
                    d.Append("\"blend\":").Append(_effectBlend.ToString("F3"));
                });
                // #endregion
            }
        }

        internal static void SetShaderActive(bool active)
        {
            if (!active)
            {
                _effectBlend = 0f;
                ApplyDepthPolicy(ShouldKeepDepthActive());
                ReleaseIdleResources();
            }
        }

        internal static LidarUniformSnapshot GetUniformSnapshot()
        {
            float blend = _effectBlend;
            if (LidarConfig.DebugForceBlend > 0.001f)
                blend = Mathf.Max(blend, LidarConfig.DebugForceBlend);

            return new LidarUniformSnapshot
            {
                EffectBlend = blend,
                MaxLidarDistance = LidarConfig.CastMaxDistanceM,
                ImpactDistance = _impactDistance,
                TimeToImpact = _timeToImpact,
                LidarDirection = _lidarDirection,
                AppearBootElapsed = _appearBootElapsed,
            };
        }

        private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!LidarConfig.Enabled)
                return;

            int frame = Time.frameCount;
            if (_dedupeFrame != frame)
            {
                _dedupeFrame = frame;
                s_enqueuedCamerasThisFrame.Clear();
            }

            EnsureMaterial();
            if (_material == null)
            {
                if (Time.unscaledTime - _lastRejectLogTime > 2f)
                {
                    _lastRejectLogTime = Time.unscaledTime;
                    LidarDebugLog.Write("A", "LidarPostProcess.OnBeginCameraRendering", "material_null", d =>
                    {
                        d.Append("\"shaderReady\":").Append(LidarShaderAssets.IsReady ? "true" : "false");
                    });
                }
                return;
            }

            LidarUniformSnapshot snap = GetUniformSnapshot();
            if (snap.EffectBlend <= 0.001f)
            {
                if (camera.cameraType == CameraType.Game && Time.unscaledTime - _lastRejectLogTime > 2f)
                {
                    _lastRejectLogTime = Time.unscaledTime;
                    LidarDebugLog.Write("C", "LidarPostProcess.OnBeginCameraRendering", "enqueue_rejected", d =>
                    {
                        d.Append("\"reason\":\"blend_zero\"");
                        d.Append(',');
                        d.Append("\"cam\":\"").Append(EscapeDbg(camera.name)).Append('\"');
                    });
                }
                return;
            }

            if (LidarConfig.DebugForceBlend > 0.001f && Time.unscaledTime - _lastCameraDiscoveryLog > 2f)
            {
                _lastCameraDiscoveryLog = Time.unscaledTime;
                LogCameraDiscovery();
            }

            if (!LidarShaderAssets.IsReady)
                return;

            if (camera.cameraType != CameraType.Game)
                return;

            if (!s_enqueuedCamerasThisFrame.Add(camera.GetInstanceID()))
                return;

            CameraStateManager? csm = SceneSingleton<CameraStateManager>.i;
            Camera? mainCam = csm != null ? csm.mainCamera : null;

            bool isMainCamera = mainCam != null && camera == mainCam;
            bool isOutputCamera = IsOutputCamera(camera, mainCam, csm);

            if (!isMainCamera && !isOutputCamera)
            {
                _lastEnqueueReject = "not_target_camera";
                return;
            }

            if (!PassesCombatGates(camera, csm, isOutputCamera, out string reject))
            {
                _lastEnqueueReject = reject;
                if (Time.unscaledTime - _lastRejectLogTime > 2f)
                {
                    _lastRejectLogTime = Time.unscaledTime;
                    LidarDebugLog.Write("C", "LidarPostProcess.OnBeginCameraRendering", "enqueue_rejected", d =>
                    {
                        d.Append("\"reason\":\"").Append(EscapeDbg(reject)).Append('\"');
                        d.Append(',');
                        d.Append("\"blend\":").Append(_effectBlend.ToString("F3"));
                        d.Append(',');
                        d.Append("\"cam\":\"").Append(EscapeDbg(camera.name)).Append('\"');
                    });
                }
                return;
            }

            _lastEnqueueReject = "ok";
            BindCamera(camera);
            ApplyDepthPolicy(true);

            UniversalAdditionalCameraData? urp = camera.GetUniversalAdditionalCameraData();
            if (urp == null || urp.scriptableRenderer == null)
                return;

            if (isMainCamera)
            {
                urp.scriptableRenderer.EnqueuePass(s_depthPass);
            }

            if (isOutputCamera)
            {
                s_pass.SetMaterial(_material);
                urp.scriptableRenderer.EnqueuePass(s_pass);

                // #region agent log
                LidarDebugLog.Write("C", "LidarPostProcess.OnBeginCameraRendering", "pass_enqueued", d =>
                {
                    d.Append("\"blend\":").Append(_effectBlend.ToString("F3"));
                    d.Append(',');
                    d.Append("\"debugForce\":").Append(LidarConfig.DebugForceBlend.ToString("F3"));
                    d.Append(',');
                    d.Append("\"shaderMode\":").Append(LidarConfig.DebugShaderMode);
                    d.Append(',');
                    d.Append("\"cam\":\"").Append(EscapeDbg(camera.name)).Append('\"');
                    d.Append(',');
                    d.Append("\"role\":\"").Append(isMainCamera && isOutputCamera ? "main+output" : isMainCamera ? "depth" : "output").Append('\"');
                });
                // #endregion

                if (!_loggedEnqueue)
                {
                    _loggedEnqueue = true;
                    Debug.Log("[LidarWireframe] GPU pass enqueued on " + camera.name);
                }
            }
        }

        private static bool IsOutputCamera(Camera camera, Camera? mainCam, CameraStateManager? csm)
        {
            if (!string.IsNullOrEmpty(LidarConfig.OutputCameraName))
                return camera.name == LidarConfig.OutputCameraName;

            return camera == mainCam;
        }

        private static bool PassesCombatGates(Camera camera, CameraStateManager? csm, bool isOutputCamera, out string reject)
        {
            reject = "ok";

            if (LidarConfig.DebugForceBlend > 0.001f)
                return true;

            if (_combatActive && _effectBlend > 0.5f)
                return true;

            if (!isOutputCamera)
                return true;

            if (csm == null)
            {
                reject = "csm_null";
                return false;
            }

            if (csm.mainCamera == null)
            {
                reject = "main_camera_null";
                return false;
            }

            if (csm.currentState != csm.cockpitState)
            {
                reject = "not_cockpit_state";
                return false;
            }

            GameState state = GameManager.gameState;
            if (state != GameState.SinglePlayer && state != GameState.Multiplayer)
            {
                reject = "bad_game_state";
                return false;
            }

            if (!GameManager.flightControlsEnabled)
            {
                reject = "flight_controls_disabled";
                return false;
            }

            return true;
        }

        private static void LogCameraDiscovery()
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera c = cameras[i];
                if (c == null || c.cameraType != CameraType.Game)
                    continue;

                string target = c.targetTexture != null ? c.targetTexture.name : "screen";
                LidarDebugLog.Write("H3", "LidarPostProcess.LogCameraDiscovery", "camera_row", d =>
                {
                    d.Append("\"name\":\"").Append(EscapeDbg(c.name)).Append('\"');
                    d.Append(',');
                    d.Append("\"depth\":").Append(c.depth);
                    d.Append(',');
                    d.Append("\"enabled\":").Append(c.enabled ? "true" : "false");
                    d.Append(',');
                    d.Append("\"target\":\"").Append(EscapeDbg(target)).Append('\"');
                });
            }
        }

        private static void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (_boundCamera != null && camera != _boundCamera)
                return;

            if (_effectBlend > 0.001f || LidarConfig.DebugForceBlend > 0.001f)
                return;

            ReleaseIdleResources();
        }

        private static string EscapeDbg(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static void BindCamera(Camera camera)
        {
            _boundCamera = camera;
            EnsureMaterial();
            if (LidarConfig.ForceKeepDepthTextureActive)
                ApplyDepthPolicy(true);
        }

        internal static void EnsureMaterialFromShader()
        {
            EnsureMaterial();
        }

        private static void EnsureMaterial()
        {
            if (_material != null)
                return;

            Shader? shader = LidarShaderAssets.CompositeShader;
            if (shader == null)
                return;

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            s_pass.SetMaterial(_material);
        }

        private static bool ShouldKeepDepthActive()
        {
            if (LidarConfig.ForceKeepDepthTextureActive)
                return true;

            if (LidarConfig.DebugForceBlend > 0.001f)
                return true;

            return _effectBlend > 0.001f;
        }

        private static void ApplyDepthPolicy(bool needsDepth)
        {
            if (_boundCamera == null)
                return;

            bool wantDepth = LidarConfig.ForceKeepDepthTextureActive || needsDepth;
            if (wantDepth == _depthCurrentlyRequired)
                return;

            UniversalAdditionalCameraData urp = _boundCamera.GetUniversalAdditionalCameraData();
            urp.requiresDepthTexture = wantDepth;
            _depthCurrentlyRequired = wantDepth;
        }

        private static void ReleaseIdleResources()
        {
            ApplyDepthPolicy(false);
            s_depthPass.Cleanup();
            s_pass.Cleanup();
        }

        private static void ReleaseGpuResources()
        {
            ApplyDepthPolicy(false);
            s_depthPass.Cleanup();
            s_pass.Cleanup();

            if (_material != null)
            {
                Object.Destroy(_material);
                _material = null;
            }

            s_pass.SetMaterial(null);
        }
    }
}
