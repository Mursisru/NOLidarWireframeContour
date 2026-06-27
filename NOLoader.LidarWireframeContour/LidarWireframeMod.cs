using System;
using NOLoader.API;
using UnityEngine;

namespace NOLoader.LidarWireframeContour
{
    public sealed class LidarWireframeMod : INOMod, INOModTickNormal
    {
        private float _probeAccum;
        private float _lastNoCtrlLog;
        private float _configReloadAccum;

        public void OnLoad(ref NOModContext ctx)
        {
            try
            {
                LidarPostProcess.SetModRoot(ctx.ModRoot);
                LidarConfig.Load(ctx.ModRoot);
                LidarDebugLog.ClearOnModLoad();
                LidarShaderAssets.Initialize(ctx.ModRoot);
                LidarPostProcess.EnsureMaterialFromShader();
                LidarPostProcess.EnsurePipelineHook();
                string stageName = ctx.Stage.ToString();
                Debug.Log($"[LidarWireframe] {AppVersion.DisplayVersion} loaded (stage={stageName}, gpu={LidarShaderAssets.IsReady}).");
                // #region agent log
                LidarDebugLog.Write("E", "LidarWireframeMod.OnLoad", "mod_loaded", d =>
                {
                    d.Append("\"gpu\":").Append(LidarShaderAssets.IsReady ? "true" : "false");
                    d.Append(',');
                    d.Append("\"stage\":\"").Append(stageName).Append('\"');
                    d.Append(',');
                    d.Append("\"bundleBytes\":").Append(LidarShaderAssets.LoadedBundleBytes);
                    d.Append(',');
                    d.Append("\"packShader\":").Append(LidarShaderAssets.HasPackShader ? "true" : "false");
                    d.Append(',');
                    d.Append("\"edgeShader\":").Append(LidarShaderAssets.HasEdgeShader ? "true" : "false");
                    d.Append(',');
                    d.Append("\"compositeSupported\":").Append(LidarShaderAssets.CompositeSupported ? "true" : "false");
                    d.Append(',');
                    d.Append("\"shaderHash\":\"").Append(LidarShaderAssets.CompositeShaderSourceHash).Append('\"');
                    d.Append(',');
                    d.Append("\"version\":\"").Append(AppVersion.DisplayVersion).Append('\"');
                });
                // #endregion
            }
            catch (Exception ex)
            {
                Debug.LogError("[LidarWireframe] OnLoad failed: " + ex.Message);
                Debug.LogException(ex);
                throw;
            }
        }

        public void OnUnload(ref NOModContext ctx)
        {
            LidarPostProcess.Shutdown();
            Debug.Log("[LidarWireframe] Unloaded.");
        }

        public void OnNormalUpdate(ref NOModContext ctx, float dt)
        {
            _configReloadAccum += dt;
            if (_configReloadAccum >= 1f)
            {
                _configReloadAccum = 0f;
                LidarPostProcess.TryReloadConfig();
            }

            if (!LidarConfig.Enabled)
                return;

            Patches.EnsureController();

            ACT_LidarCollisionController? ctrl = ACT_LidarCollisionController.Instance;
            if (ctrl == null)
            {
                // #region agent log
                if (Time.unscaledTime - _lastNoCtrlLog > 3f)
                {
                    _lastNoCtrlLog = Time.unscaledTime;
                    LidarDebugLog.Write("E", "LidarWireframeMod.OnNormalUpdate", "controller_null", d => { });
                }
                // #endregion
                return;
            }

            ctrl.FadeTick(dt);

            _probeAccum += dt;
            float probeInterval = ctrl.GetProbeIntervalSec();
            if (_probeAccum < probeInterval)
                return;

            _probeAccum = 0f;
            ctrl.ProbeTick();
        }
    }
}
