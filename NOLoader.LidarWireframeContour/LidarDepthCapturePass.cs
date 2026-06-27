using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NOLoader.LidarWireframeContour
{
    internal sealed class LidarDepthCapturePass : ScriptableRenderPass
    {
        private RTHandle? _capturedDepth;
        private RTHandle? _edgeMask;
        private Material? _edgeMaterial;
        private int _lastCaptureFrame = -1;
        private bool _lastCaptureOk;
        private bool _lastEdgePassOk;
        private static int _lastExecuteFrame = -1;
        private static int _lastExecuteCameraId = -1;

        private static readonly int IdMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int IdEdgeThreshold = Shader.PropertyToID("_EdgeThreshold");
        private static readonly int IdEdgeStrength = Shader.PropertyToID("_EdgeStrength");
        private static readonly int IdEdgeThinPow = Shader.PropertyToID("_EdgeThinPow");
        private static readonly int IdEdgeTexelScale = Shader.PropertyToID("_EdgeTexelScale");

        internal LidarDepthCapturePass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            profilingSampler = new ProfilingSampler("ACT.LidarDepthCapture");
        }

        internal bool TryGetCapturedDepth(out Texture depthTex)
        {
            depthTex = null!;
            Texture? raw = _capturedDepth != null ? _capturedDepth.rt : null;
            if (raw == null)
                return false;

            if (_lastCaptureFrame != Time.frameCount)
                return false;

            depthTex = raw;
            return _lastCaptureOk;
        }

        internal bool TryGetCapturedEdge(out Texture edgeTex)
        {
            edgeTex = null!;
            if (!_lastEdgePassOk || _edgeMask == null || _edgeMask.rt == null)
                return false;

            if (_lastCaptureFrame != Time.frameCount)
                return false;

            edgeTex = _edgeMask.rt;
            return _lastCaptureOk;
        }

        internal void Cleanup()
        {
            if (_capturedDepth != null)
            {
                _capturedDepth.Release();
                _capturedDepth = null;
            }

            if (_edgeMask != null)
            {
                _edgeMask.Release();
                _edgeMask = null;
            }

            if (_edgeMaterial != null)
            {
                Object.Destroy(_edgeMaterial);
                _edgeMaterial = null;
            }

            _lastCaptureOk = false;
            _lastEdgePassOk = false;
            _lastCaptureFrame = -1;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            _lastCaptureOk = false;
            _lastEdgePassOk = false;

            if (!LidarPostProcess.ShouldEnqueueGpuPasses())
                return;

            Camera cam = renderingData.cameraData.camera;
            if (cam == null)
                return;

            int frame = Time.frameCount;
            int camId = cam.GetInstanceID();
            if (_lastExecuteFrame == frame && _lastExecuteCameraId == camId)
                return;

            _lastExecuteFrame = frame;
            _lastExecuteCameraId = camId;

            Material? copyDepthMaterial = LidarCopyDepthMaterial.Instance;
            if (copyDepthMaterial == null)
            {
                LidarDebugLog.Write("D", "LidarDepthCapturePass.Execute", "copy_material_null", d => { });
                return;
            }

            ref CameraData cameraData = ref renderingData.cameraData;
            RTHandle depthSource = cameraData.renderer.cameraDepthTargetHandle;
            if (depthSource == null)
            {
                LidarDebugLog.Write("D", "LidarDepthCapturePass.Execute", "depth_source_null", d => { });
                return;
            }

            bool copyToDepth = !RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R32_SFloat, FormatUsage.Render);
            int sourceMsaaSamples = cameraData.cameraTargetDescriptor.msaaSamples;
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            desc.graphicsFormat = GraphicsFormat.R32_SFloat;

            RenderingUtils.ReAllocateIfNeeded(
                ref _capturedDepth,
                desc,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                name: "_LidarDepthCapture");

            RenderTextureDescriptor edgeDesc = desc;
            edgeDesc.graphicsFormat = GraphicsFormat.R8_UNorm;
            RenderingUtils.ReAllocateIfNeeded(
                ref _edgeMask,
                edgeDesc,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                name: "_LidarDepthEdge");

            if (_capturedDepth == null || _edgeMask == null)
                return;

            EnsureEdgeMaterial();

            CommandBuffer cmd = CommandBufferPool.Get("ACT.LidarDepthCapture");
            using (new ProfilingScope(cmd, profilingSampler))
            {
                ConfigureMsaaKeywords(cmd, sourceMsaaSamples);

                if (copyToDepth)
                    cmd.EnableShaderKeyword("_OUTPUT_DEPTH");
                else
                    cmd.DisableShaderKeyword("_OUTPUT_DEPTH");

                cmd.SetGlobalTexture("_CameraDepthAttachment", depthSource.nameID);

                Vector2 viewportScale = depthSource.useScaling
                    ? new Vector2(depthSource.rtHandleProperties.rtHandleScale.x, depthSource.rtHandleProperties.rtHandleScale.y)
                    : Vector2.one;
                bool yflip = cameraData.IsHandleYFlipped(depthSource) != cameraData.IsHandleYFlipped(_capturedDepth);
                Vector4 scaleBias = yflip
                    ? new Vector4(viewportScale.x, -viewportScale.y, 0f, viewportScale.y)
                    : new Vector4(viewportScale.x, viewportScale.y, 0f, 0f);

                cmd.SetViewport(new Rect(0f, 0f, desc.width, desc.height));
                CoreUtils.SetRenderTarget(cmd, _capturedDepth);
                Blitter.BlitTexture(cmd, depthSource, scaleBias, copyDepthMaterial, 0);

                if (_edgeMaterial != null && _capturedDepth.rt != null)
                {
                    _edgeMaterial.SetTexture(IdMainTex, _capturedDepth.rt);
                    _edgeMaterial.SetFloat(IdEdgeThreshold, LidarConfig.EdgeThreshold);
                    _edgeMaterial.SetFloat(IdEdgeStrength, LidarConfig.EdgeStrength);
                    _edgeMaterial.SetFloat(IdEdgeThinPow, LidarConfig.EdgeThinPow);
                    _edgeMaterial.SetFloat(IdEdgeTexelScale, LidarConfig.EdgeTexelScale);
                    CoreUtils.SetRenderTarget(cmd, _edgeMask);
                    CoreUtils.DrawFullScreen(cmd, _edgeMaterial, shaderPassId: 0);
                    _lastEdgePassOk = true;
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            _lastCaptureOk = true;
            _lastCaptureFrame = Time.frameCount;

            LidarDebugLog.Write("D", "LidarDepthCapturePass.Execute", "depth_captured", d =>
            {
                d.Append("\"copyToDepth\":").Append(copyToDepth ? "true" : "false");
                d.Append(',');
                d.Append("\"api\":\"").Append(SystemInfo.graphicsDeviceType.ToString()).Append('\"');
                d.Append(',');
                d.Append("\"sourceMsaa\":").Append(sourceMsaaSamples);
                d.Append(',');
                d.Append("\"w\":").Append(desc.width);
                d.Append(',');
                d.Append("\"h\":").Append(desc.height);
                d.Append(',');
                d.Append("\"edgeW\":").Append(edgeDesc.width);
                d.Append(',');
                d.Append("\"edgeH\":").Append(edgeDesc.height);
                d.Append(',');
                d.Append("\"depthFmt\":\"r32\"");
                d.Append(',');
                d.Append("\"edge\":").Append(_lastEdgePassOk ? "true" : "false");
            });
        }

        private static void ConfigureMsaaKeywords(CommandBuffer cmd, int msaa)
        {
            switch (msaa)
            {
                case 8:
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                    break;
                case 4:
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                    break;
                case 2:
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                    break;
                default:
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                    break;
            }
        }

        private void EnsureEdgeMaterial()
        {
            if (_edgeMaterial != null)
                return;

            Shader? edgeShader = LidarShaderAssets.EdgeShader;
            if (edgeShader == null)
                return;

            _edgeMaterial = new Material(edgeShader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
    }
}
