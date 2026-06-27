using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NOLoader.LidarWireframeContour
{
    internal sealed class LidarWireframeRenderPass : ScriptableRenderPass
    {
        private const float CombatSummaryIntervalSec = 0.5f;
        private static readonly int IdBlitTexture = Shader.PropertyToID("_BlitTexture");
        private static readonly int IdInvProj = Shader.PropertyToID("_InvProjMatrix");
        private static readonly int IdMaxLidarDistance = Shader.PropertyToID("_MaxLidarDistance");
        private static readonly int IdImpactDistance = Shader.PropertyToID("_ImpactDistance");
        private static readonly int IdTimeToImpact = Shader.PropertyToID("_TimeToImpact");
        private static readonly int IdLidarDirView = Shader.PropertyToID("_LidarDirView");
        private static readonly int IdEffectBlend = Shader.PropertyToID("_EffectBlend");
        private static readonly int IdConeCosHalfAngle = Shader.PropertyToID("_ConeCosHalfAngle");
        private static readonly int IdLidarColor = Shader.PropertyToID("_LidarColor");
        private static readonly int IdDepthClipMargin = Shader.PropertyToID("_DepthClipMargin");
        private static readonly int IdNearClipM = Shader.PropertyToID("_NearClipM");
        private static readonly int IdImpactBandHalfM = Shader.PropertyToID("_ImpactBandHalfM");
        private static readonly int IdEdgeThreshold = Shader.PropertyToID("_EdgeThreshold");
        private static readonly int IdEdgeStrength = Shader.PropertyToID("_EdgeStrength");
        private static readonly int IdEdgeThinPow = Shader.PropertyToID("_EdgeThinPow");
        private static readonly int IdEdgeTexelScale = Shader.PropertyToID("_EdgeTexelScale");
        private static readonly int IdNoiseStrength = Shader.PropertyToID("_NoiseStrength");
        private static readonly int IdDistanceFadeMeters = Shader.PropertyToID("_DistanceFadeMeters");
        private static readonly int IdConeFalloffWidth = Shader.PropertyToID("_ConeFalloffWidth");
        private static readonly int IdHudBrightness = Shader.PropertyToID("_HudBrightness");
        private static readonly int IdTtiActivateSec = Shader.PropertyToID("_TtiActivateSec");
        private static readonly int IdDebugBypass = Shader.PropertyToID("_DebugBypass");
        private static readonly int IdDebugShaderMode = Shader.PropertyToID("_DebugShaderMode");
        private static readonly int IdDepthTex = Shader.PropertyToID("_DepthTex");
        private static readonly int IdEdgeTex = Shader.PropertyToID("_EdgeTex");
        private static readonly int IdHasEdgeTex = Shader.PropertyToID("_HasEdgeTex");

        private Material? _material;
        private LidarDepthCapturePass? _depthCapturePass;
        private RTHandle? _copiedColor;
        private static int _executeCount;
        private static float _lastCombatSummaryTime = -999f;
        private static int _lastExecuteFrame = -1;
        private static int _lastExecuteCameraId = -1;
        private static float _lastDetailLogTime = -999f;

        internal LidarWireframeRenderPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
            profilingSampler = new ProfilingSampler("ACT.LidarWireframeContour");
        }

        internal void SetMaterial(Material? material) => _material = material;

        internal void SetDepthCapturePass(LidarDepthCapturePass depthCapturePass) => _depthCapturePass = depthCapturePass;

        internal void Cleanup()
        {
            if (_copiedColor != null)
            {
                _copiedColor.Release();
                _copiedColor = null;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null)
                return;

            ref CameraData cameraData = ref renderingData.cameraData;
            Camera cam = cameraData.camera;
            if (cam == null)
                return;

            int frame = Time.frameCount;
            int camId = cam.GetInstanceID();
            if (_lastExecuteFrame == frame && _lastExecuteCameraId == camId)
                return;

            _lastExecuteFrame = frame;
            _lastExecuteCameraId = camId;

            LidarUniformSnapshot uniforms = LidarPostProcess.GetUniformSnapshot();
            if (uniforms.EffectBlend <= 0.001f)
                return;

            string depthSource = ResolveDepthTexture(out Texture? depthTex);
            if (depthTex == null || IsPlaceholderDepth(depthTex))
            {
                LidarDebugLog.Write("D", "LidarWireframeRenderPass.Execute", "skip_bad_depth", d =>
                {
                    d.Append("\"name\":\"").Append(Escape(depthTex != null ? depthTex.name : "null")).Append('\"');
                    d.Append(',');
                    d.Append("\"source\":\"").Append(depthSource).Append('\"');
                    d.Append(',');
                    d.Append("\"cam\":\"").Append(Escape(cam.name)).Append('\"');
                });
                return;
            }

            ScriptableRenderer renderer = cameraData.renderer;
            RTHandle sourceColor = renderer.cameraColorTargetHandle;
            string sourceName = sourceColor != null && sourceColor.rt != null ? sourceColor.rt.name : "null";

            Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Vector3 camFwdView = cam.worldToCameraMatrix.MultiplyVector(cam.transform.forward).normalized;
            Vector3 velView = cam.worldToCameraMatrix.MultiplyVector(uniforms.LidarDirection).normalized;
            Vector3 coneDirView = ResolveConeDirection(cam, camFwdView, velView);
            float centerConeDot = ComputeCenterConeDot(gpuProj, coneDirView);
            bool debugBypass = LidarConfig.DebugForceBlend > 0.001f;
            bool hasEdge = LidarPostProcess.DepthCapturePass.TryGetCapturedEdge(out Texture? edgeTex);

            _material.SetMatrix(IdInvProj, gpuProj.inverse);
            _material.SetFloat(IdMaxLidarDistance, uniforms.MaxLidarDistance);
            _material.SetFloat(IdImpactDistance, uniforms.ImpactDistance);
            _material.SetFloat(IdTimeToImpact, uniforms.TimeToImpact);
            _material.SetVector(IdLidarDirView, coneDirView);
            _material.SetFloat(IdEffectBlend, uniforms.EffectBlend);
            _material.SetFloat(IdConeCosHalfAngle, LidarConfig.ConeCosHalfAngle);
            _material.SetColor(IdLidarColor, LidarConfig.LidarColor);
            _material.SetFloat(IdDepthClipMargin, LidarConfig.DepthClipMarginM);
            _material.SetFloat(IdNearClipM, LidarConfig.NearClipM);
            _material.SetFloat(IdImpactBandHalfM, LidarConfig.ImpactBandHalfM);
            _material.SetFloat(IdEdgeThreshold, LidarConfig.EdgeThreshold);
            _material.SetFloat(IdEdgeStrength, LidarConfig.EdgeStrength);
            _material.SetFloat(IdEdgeThinPow, LidarConfig.EdgeThinPow);
            _material.SetFloat(IdEdgeTexelScale, LidarConfig.EdgeTexelScale);
            _material.SetFloat(IdNoiseStrength, LidarConfig.NoiseStrength);
            _material.SetFloat(IdDistanceFadeMeters, LidarConfig.DistanceFadeMeters);
            _material.SetFloat(IdConeFalloffWidth, LidarConfig.ConeFalloffCos);
            _material.SetFloat(IdHudBrightness, LidarConfig.HudBrightness);
            _material.SetFloat(IdTtiActivateSec, LidarConfig.TtiActivateSec);
            _material.SetFloat(IdDebugBypass, debugBypass ? 1f : 0f);
            _material.SetFloat(IdDebugShaderMode, LidarConfig.DebugShaderMode);
            _material.SetTexture(IdDepthTex, depthTex);
            _material.SetFloat(IdHasEdgeTex, hasEdge ? 1f : 0f);
            if (hasEdge && edgeTex != null)
                _material.SetTexture(IdEdgeTex, edgeTex);

            LogCombatFrameSummary(cam, uniforms, depthSource, centerConeDot, debugBypass, hasEdge);

            RenderTextureDescriptor colorDesc = cameraData.cameraTargetDescriptor;
            colorDesc.depthBufferBits = 0;
            colorDesc.msaaSamples = 1;
            RenderingUtils.ReAllocateIfNeeded(
                ref _copiedColor,
                colorDesc,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_LidarCompositeCopy");

            string destName = "null";
            CommandBuffer cmd = CommandBufferPool.Get("ACT.LidarWireframeContour");
            using (new ProfilingScope(cmd, profilingSampler))
            {
                if (_copiedColor != null && sourceColor != null)
                {
                    Blitter.BlitCameraTexture(cmd, sourceColor, _copiedColor);
                    _material.SetTexture(IdBlitTexture, _copiedColor);

                    RTHandle backBuffer = LidarUrpAccess.ResolveColorBackBuffer(renderer, cmd, sourceColor);
                    destName = backBuffer != null && backBuffer.rt != null ? backBuffer.rt.name : "null";
                    CoreUtils.SetRenderTarget(cmd, backBuffer);
                    CoreUtils.DrawFullScreen(cmd, _material, shaderPassId: 0);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            bool debugActive = debugBypass || LidarConfig.DebugShaderMode > 0;
            if (debugActive || _executeCount < 5 || Time.unscaledTime - _lastDetailLogTime > 0.5f)
            {
                _lastDetailLogTime = Time.unscaledTime;
                string destNameLog = destName;
                LidarDebugLog.Write("H2", "LidarWireframeRenderPass.Execute", "composite_detail", d =>
                {
                    d.Append("\"frame\":").Append(frame);
                    d.Append(',');
                    d.Append("\"cam\":\"").Append(Escape(cam.name)).Append('\"');
                    d.Append(',');

                    d.Append("\"source\":\"").Append(Escape(sourceName)).Append('\"');
                    d.Append(',');
                    d.Append("\"dest\":\"").Append(Escape(destNameLog)).Append('\"');
                    d.Append(',');
                    d.Append("\"path\":\"backbuffer_fullscreen\"");
                    d.Append(',');
                    d.Append("\"shaderMode\":").Append(LidarConfig.DebugShaderMode);
                    d.Append(',');
                    d.Append("\"debugForce\":").Append(LidarConfig.DebugForceBlend.ToString("F3"));
                    d.Append(',');
                    d.Append("\"blend\":").Append(uniforms.EffectBlend.ToString("F3"));
                    d.Append(',');
                    d.Append("\"depthSource\":\"").Append(depthSource).Append('\"');
                    d.Append(',');
                    d.Append("\"hasEdge\":").Append(hasEdge ? "true" : "false");
                });
            }

            _executeCount++;
            if (_executeCount <= 3 || _executeCount % 120 == 0)
            {
                LidarDebugLog.Write("D", "LidarWireframeRenderPass.Execute", "draw_ok", d =>
                {
                    d.Append("\"count\":").Append(_executeCount);
                    d.Append(',');
                    d.Append("\"blend\":").Append(uniforms.EffectBlend.ToString("F3"));
                    d.Append(',');
                    d.Append("\"shaderMode\":").Append(LidarConfig.DebugShaderMode);
                    d.Append(',');
                    d.Append("\"debugBypass\":").Append(debugBypass ? "true" : "false");
                    d.Append(',');
                    d.Append("\"depthSource\":\"").Append(depthSource).Append('\"');
                    d.Append(',');
                    d.Append("\"hasEdge\":").Append(hasEdge ? "true" : "false");
                    d.Append(',');
                    d.Append("\"cam\":\"").Append(Escape(cam.name)).Append('\"');
                    d.Append(',');
                    d.Append("\"path\":\"backbuffer_fullscreen\"");
                });
            }
        }

        private static string ResolveDepthTexture(out Texture? depthTex)
        {
            depthTex = null;
            if (LidarPostProcess.DepthCapturePass.TryGetCapturedDepth(out Texture captured))
            {
                depthTex = captured;
                return "captured_r32";
            }

            depthTex = Shader.GetGlobalTexture("_CameraDepthTexture");
            return "global";
        }

        private static Vector3 ResolveConeDirection(Camera cam, Vector3 camFwdView, Vector3 velView)
        {
            if (velView.sqrMagnitude > 1e-4f)
                return velView.normalized;
            return camFwdView;
        }

        private static float ComputeCenterConeDot(Matrix4x4 gpuProj, Vector3 coneDirView)
        {
            Matrix4x4 invProj = gpuProj.inverse;
            Vector4 viewH = invProj * new Vector4(0f, 0f, 1f, 1f);
            Vector3 centerRay = new Vector3(viewH.x, viewH.y, viewH.z) / Mathf.Max(Mathf.Abs(viewH.w), 1e-6f);
            centerRay.Normalize();
            return Vector3.Dot(centerRay, coneDirView.normalized);
        }

        private static void LogCombatFrameSummary(
            Camera cam,
            LidarUniformSnapshot uniforms,
            string depthSource,
            float centerConeDot,
            bool debugBypass,
            bool hasEdge)
        {
            if (debugBypass || uniforms.EffectBlend <= 0.05f)
                return;
            if (Time.unscaledTime - _lastCombatSummaryTime < CombatSummaryIntervalSec)
                return;

            _lastCombatSummaryTime = Time.unscaledTime;
            LidarDebugLog.Write("F", "LidarWireframeRenderPass.Execute", "combat_frame_summary", d =>
            {
                d.Append("\"blend\":").Append(uniforms.EffectBlend.ToString("F3"));
                d.Append(',');
                d.Append("\"cam\":\"").Append(Escape(cam.name)).Append('\"');
                d.Append(',');
                d.Append("\"depthSource\":\"").Append(depthSource).Append('\"');
                d.Append(',');
                d.Append("\"shaderMode\":").Append(LidarConfig.DebugShaderMode);
                d.Append(',');
                d.Append("\"debugForce\":").Append(LidarConfig.DebugForceBlend.ToString("F3"));
                d.Append(',');
                d.Append("\"impactDist\":").Append(uniforms.ImpactDistance.ToString("F1"));
                d.Append(',');
                d.Append("\"centerConeDot\":").Append(centerConeDot.ToString("F3"));
                d.Append(',');
                d.Append("\"hasEdge\":").Append(hasEdge ? "true" : "false");
            });
        }

        private static bool IsPlaceholderDepth(Texture tex)
        {
            string name = tex.name;
            if (string.IsNullOrEmpty(name))
                return false;
            return name.IndexOf("Default", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("UnityBlack", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("UnityWhite", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    internal struct LidarUniformSnapshot
    {
        internal float EffectBlend;
        internal float MaxLidarDistance;
        internal float ImpactDistance;
        internal float TimeToImpact;
        internal Vector3 LidarDirection;
    }
}
