using System;
using System.Collections;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using NOLoader.LidarWireframeContour;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LidarWireframeContour.BepInEx
{
    /// <summary>Persists across scenes; mission tick + Harmony (NOLoader loadStage Mission parity).</summary>
    internal sealed class LidarWireframeHost : MonoBehaviour
    {
        private static LidarWireframeHost? _instance;
        private Harmony? _harmony;
        private string _pluginDir = string.Empty;
        private ManualLogSource? _logger;
        private bool _missionReady;
        private bool _startupScheduled;
        private float _probeAccum;
        private float _lastNoCtrlAudit;
        private float _lastHeartbeat;
        private static bool s_controllerEnsured;

        internal static void Ensure(string pluginDir, ManualLogSource logger)
        {
            if (_instance != null)
                return;

            var go = new GameObject("LidarWireframe.Host");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _instance = go.AddComponent<LidarWireframeHost>();
            _instance._pluginDir = pluginDir;
            _instance._logger = logger;
            SceneManager.sceneLoaded += _instance.OnSceneLoaded;
            _instance.TryBootstrapCurrentScene();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_missionReady)
                return;

            if (IsMenuOrSystemScene(scene.path))
                return;

            ScheduleMissionStartup(scene.path);
        }

        private void TryBootstrapCurrentScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!IsMenuOrSystemScene(scene.path))
                ScheduleMissionStartup(scene.path);
        }

        private void ScheduleMissionStartup(string scenePath)
        {
            if (_missionReady || _startupScheduled)
                return;

            _startupScheduled = true;
            StartCoroutine(DeferredMissionStartup(scenePath));
        }

        private IEnumerator DeferredMissionStartup(string scenePath)
        {
            yield return null;
            if (_missionReady)
                yield break;

            _missionReady = true;
            StartupMission(_pluginDir, scenePath);
        }

        private void StartupMission(string pluginDir, string scenePath)
        {
            LidarDebugLog.ClearOnModLoad();
            LidarPostProcess.SetModRoot(pluginDir);
            LidarShaderAssets.Initialize(pluginDir);
            LidarPostProcess.EnsureMaterialFromShader();
            LidarPostProcess.EnsurePipelineHook();
            ApplyHarmonyPatches();

            _logger?.LogInfo($"[LidarWireframe] {AppVersion.DisplayVersion} mission ready (gpu={LidarShaderAssets.IsReady}, scene={scenePath}).");
            // #region agent log
            LidarDebugLog.Audit("H0", "LidarWireframeHost.StartupMission", "mission_ready", d =>
            {
                d.Append("\"gpu\":").Append(LidarShaderAssets.IsReady ? "true" : "false");
                d.Append(',');
                d.Append("\"scene\":\"").Append(scenePath.Replace("\\", "\\\\")).Append('\"');
                d.Append(',');
                d.Append("\"bundleBytes\":").Append(LidarShaderAssets.LoadedBundleBytes);
            });
            // #endregion
        }

        private void ApplyHarmonyPatches()
        {
            if (_harmony != null)
                return;

            _harmony = new Harmony(LidarWireframeBepInPlugin.PluginGuid);
            _harmony.PatchAll(typeof(LidarWireframeBepInPlugin).Assembly);

            int patched = 0;
            foreach (MethodBase method in _harmony.GetPatchedMethods())
                patched++;

            _logger?.LogInfo($"[LidarWireframe] Harmony patched methods: {patched}");
            // #region agent log
            LidarDebugLog.Audit("H1", "LidarWireframeHost.ApplyHarmonyPatches", "harmony_applied", d =>
            {
                d.Append("\"patchedCount\":").Append(patched);
            });
            // #endregion

            if (patched == 0)
                _logger?.LogError("[LidarWireframe] Harmony applied zero game patches.");
        }

        private static bool IsMenuOrSystemScene(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            return path.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("MultiplayerMenu", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("MissionsMenu", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("Encyclopedia", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("MissionEditor", StringComparison.OrdinalIgnoreCase) >= 0
                || path.IndexOf("empty", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Update()
        {
            if (!_missionReady)
                return;

            // #region agent log
            if (Time.unscaledTime - _lastHeartbeat > 5f)
            {
                _lastHeartbeat = Time.unscaledTime;
                LidarDebugLog.Audit("H6", "LidarWireframeHost.Update", "heartbeat", d =>
                {
                    d.Append("\"enabled\":").Append(LidarConfig.Enabled ? "true" : "false");
                    d.Append(',');
                    d.Append("\"flightHud\":").Append(SceneSingleton<FlightHud>.i != null ? "true" : "false");
                }, 0f);
            }
            // #endregion

            if (!LidarConfig.Enabled)
                return;

            EnsureController();

            ACT_LidarCollisionController? ctrl = ACT_LidarCollisionController.Instance;
            if (ctrl == null)
            {
                // #region agent log
                if (Time.unscaledTime - _lastNoCtrlAudit > 3f)
                {
                    _lastNoCtrlAudit = Time.unscaledTime;
                    LidarDebugLog.Audit("H1", "LidarWireframeHost.Update", "controller_null", d =>
                    {
                        d.Append("\"flightHud\":").Append(SceneSingleton<FlightHud>.i != null ? "true" : "false");
                    }, 0f);
                }
                // #endregion
                return;
            }

            float dt = Time.unscaledDeltaTime;
            ctrl.FadeTick(dt);

            _probeAccum += dt;
            float probeInterval = ctrl.GetProbeIntervalSec();
            if (_probeAccum < probeInterval)
                return;

            _probeAccum -= probeInterval;
            ctrl.ProbeTick();
        }

        private void OnApplicationQuit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _harmony?.UnpatchSelf();
            _harmony = null;
            LidarPostProcess.Shutdown();
            _instance = null;
        }

        private static void EnsureController()
        {
            if (s_controllerEnsured && ACT_LidarCollisionController.Instance != null)
                return;

            FlightHud? fh = SceneSingleton<FlightHud>.i;
            if (fh == null)
                return;

            if (fh.GetComponent<ACT_LidarCollisionController>() == null)
                fh.gameObject.AddComponent<ACT_LidarCollisionController>();

            if (ACT_LidarCollisionController.Instance != null)
                s_controllerEnsured = true;
        }
    }
}
