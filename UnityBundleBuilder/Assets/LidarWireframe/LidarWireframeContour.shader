Shader "Hidden/ACT/LidarWireframeContour"
{
    Properties
    {
        _BlitTexture ("Scene Color", 2D) = "white" {}
        _DepthTex ("Scene Depth", 2D) = "white" {}
        _MaxLidarDistance ("Max Lidar Distance", Float) = 1500
        _ImpactDistance ("Impact Distance", Float) = 500
        _TimeToImpact ("Time To Impact", Float) = 7
        _LidarDirView ("Lidar Direction View", Vector) = (0, 0, 1, 0)
        _EffectBlend ("Effect Blend", Range(0, 1)) = 0
        _ConeCosHalfAngle ("Cone Cos Half Angle", Float) = 0.999
        _LidarColor ("Lidar Color", Color) = (0, 1, 0.4, 1)
        _DepthClipMargin ("Depth Clip Margin", Float) = 5
        _NearClipM ("Near Clip M", Float) = 80
        _ImpactBandHalfM ("Impact Band Half M", Float) = 120
        _EdgeThreshold ("Edge Threshold", Range(0.01, 20)) = 0.5
        _EdgeStrength ("Edge Strength", Range(0, 8)) = 2.0
        _EdgeThinPow ("Edge Thin Pow", Range(1, 8)) = 2.5
        _EdgeTexelScale ("Edge Texel Scale", Range(0.25, 1.5)) = 0.65
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.15
        _DistanceFadeMeters ("Distance Fade Meters", Float) = 175
        _ConeFalloffWidth ("Cone Falloff Width", Float) = 0.05
        _HudBrightness ("HUD Brightness", Range(0, 1)) = 0.62
        _AppearBootElapsed ("Appear Boot Elapsed", Float) = -1
        _AppearBootSec ("Appear Boot Sec", Float) = 0.5
        _AppearBootFreqStart ("Appear Boot Freq Start", Float) = 6
        _AppearBootFreqEnd ("Appear Boot Freq End", Float) = 40
        _AppearBootDim ("Appear Boot Dim", Range(0, 1)) = 0
        _TtiActivateSec ("TTI Activate Sec", Float) = 7
        _DebugBypass ("Debug Bypass", Float) = 0
        _DebugShaderMode ("Debug Shader Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Name "LidarWireframeContour"
            ZTest Always
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            sampler2D _BlitTexture;
            sampler2D _DepthTex;
            sampler2D _EdgeTex;

            float _MaxLidarDistance;
            float _ImpactDistance;
            float _TimeToImpact;
            float4 _LidarDirView;
            float _EffectBlend;
            float _ConeCosHalfAngle;
            fixed4 _LidarColor;
            float _DepthClipMargin;
            float _NearClipM;
            float _ImpactBandHalfM;
            float _EdgeThreshold;
            float _EdgeStrength;
            float _EdgeThinPow;
            float _EdgeTexelScale;
            float _NoiseStrength;
            float _DistanceFadeMeters;
            float _ConeFalloffWidth;
            float _HudBrightness;
            float _AppearBootElapsed;
            float _AppearBootSec;
            float _AppearBootFreqStart;
            float _AppearBootFreqEnd;
            float _AppearBootDim;
            float _TtiActivateSec;
            float _DebugBypass;
            float _DebugShaderMode;
            float _HasEdgeTex;
            float4x4 _InvProjMatrix;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.texcoord = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.positionCS = float4(output.texcoord * 2.0 - 1.0, UNITY_NEAR_CLIP_VALUE, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                output.texcoord.y = 1.0 - output.texcoord.y;
                #endif
                return output;
            }

            float SampleDepth(float2 uv)
            {
                return tex2D(_DepthTex, uv).r;
            }

            float LinearDepthAt(float2 uv)
            {
                return LinearEyeDepth(SampleDepth(uv));
            }

            float ContourEdge(float2 uv, float2 texel)
            {
                float2 t = texel * max(_EdgeTexelScale, 0.35);
                float dC = LinearDepthAt(uv);
                float dL = LinearDepthAt(uv - float2(t.x, 0.0));
                float dR = LinearDepthAt(uv + float2(t.x, 0.0));
                float dU = LinearDepthAt(uv + float2(0.0, t.y));
                float dD = LinearDepthAt(uv - float2(0.0, t.y));

                float lap = abs(4.0 * dC - dL - dR - dU - dD);
                float depthRef = max(dC, 40.0);
                float lapThresh = _EdgeThreshold * lerp(0.15, 0.75, saturate(depthRef / 1100.0));

                float lapEdge = smoothstep(lapThresh, lapThresh * 1.12, lap);
                lapEdge *= 1.0 - smoothstep(lapThresh * 2.0, lapThresh * 3.8, lap);

                float rampX = abs(dC - 0.5 * (dL + dR));
                float rampY = abs(dC - 0.5 * (dU + dD));
                float rampSum = rampX + rampY;
                float slopeWash = smoothstep(0.0012, 0.028, rampSum);
                lapEdge *= 1.0 - slopeWash * 0.92;

                float flatKill = smoothstep(0.006, 0.0015, rampSum);
                lapEdge *= flatKill;

                float lapL = abs(4.0 * dL - LinearDepthAt(uv - float2(t.x * 2.0, 0.0)) - dC - LinearDepthAt(uv - float2(t.x, t.y)) - LinearDepthAt(uv - float2(t.x, -t.y)));
                float lapR = abs(4.0 * dR - dC - LinearDepthAt(uv + float2(t.x * 2.0, 0.0)) - LinearDepthAt(uv + float2(t.x, t.y)) - LinearDepthAt(uv + float2(t.x, -t.y)));
                float lapU = abs(4.0 * dU - LinearDepthAt(uv + float2(0.0, t.y * 2.0)) - dC - LinearDepthAt(uv + float2(t.x, t.y)) - LinearDepthAt(uv - float2(t.x, t.y)));
                float lapD = abs(4.0 * dD - dC - LinearDepthAt(uv - float2(0.0, t.y * 2.0)) - LinearDepthAt(uv - float2(t.x, -t.y)) - LinearDepthAt(uv + float2(t.x, -t.y)));
                float nms = step(lap + 1e-5, lapL) * step(lap + 1e-5, lapR) * step(lap + 1e-5, lapU) * step(lap + 1e-5, lapD);
                lapEdge *= nms;

                if (depthRef > 220.0)
                {
                    float2 t2 = t * 2.0;
                    float dL2 = LinearDepthAt(uv - float2(t2.x, 0.0));
                    float dR2 = LinearDepthAt(uv + float2(t2.x, 0.0));
                    float dU2 = LinearDepthAt(uv + float2(0.0, t2.y));
                    float dD2 = LinearDepthAt(uv - float2(0.0, t2.y));
                    float lap2 = abs(4.0 * dC - dL2 - dR2 - dU2 - dD2);
                    float farThresh = lapThresh * 0.85;
                    lapEdge = max(lapEdge, smoothstep(farThresh, farThresh * 1.15, lap2) * 0.88);
                }

                lapEdge = smoothstep(0.58, 0.72, lapEdge);
                return pow(saturate(lapEdge * _EdgeStrength), max(_EdgeThinPow, 1.0));
            }

            float DepthEdge(float2 uv, float2 texel)
            {
                if (_HasEdgeTex > 0.5)
                    return tex2D(_EdgeTex, uv).r;

                return ContourEdge(uv, texel);
            }

            float EffectiveNearClipM()
            {
                return max(_NearClipM, 20.0);
            }

            float TerrainRangeMask(float linearDepth)
            {
                float nearM = EffectiveNearClipM();
                if (linearDepth < nearM)
                    return 0.0;

                if (linearDepth > _MaxLidarDistance)
                    return 0.0;

                return 1.0;
            }

            float3 ViewRay(float2 uv)
            {
                float2 ndc = uv * 2.0 - 1.0;
                #if UNITY_UV_STARTS_AT_TOP
                ndc.y = -ndc.y;
                #endif
                float4 viewH = mul(_InvProjMatrix, float4(ndc, 1.0, 1.0));
                return normalize(viewH.xyz / max(viewH.w, 1e-6));
            }

            float ConeMask(float3 viewRay)
            {
                float3 lidarDir = normalize(_LidarDirView.xyz);
                float cosAngle = dot(viewRay, lidarDir);
                float falloff = max(_ConeFalloffWidth, 1e-4);
                return smoothstep(_ConeCosHalfAngle, _ConeCosHalfAngle + falloff, cosAngle);
            }

            float CombatConeMask(float3 viewRay)
            {
                return ConeMask(viewRay);
            }

            float ApplyHudIntensity(float2 uv, float intensity)
            {
                float scanline = sin(uv.y * _ScreenParams.y * 1.5) * 0.09 + 0.91;
                float noise = frac(sin(dot(uv + _Time.x, float2(12.9898, 78.233))) * 43758.5453);
                float noiseFactor = lerp(1.0, 0.85 + noise * 0.15, _NoiseStrength);
                return intensity * scanline * noiseFactor;
            }

            bool BootActive()
            {
                return _AppearBootElapsed >= 0.0 && _AppearBootElapsed < _AppearBootSec;
            }

            float BootContourVisibility()
            {
                if (!BootActive())
                    return 1.0;

                float tau = _AppearBootElapsed;
                float bootSec = max(_AppearBootSec, 0.05);
                float ramp = smoothstep(0.0, 1.0, tau / bootSec);
                float f0 = max(_AppearBootFreqStart, 1.0);
                float f1 = max(_AppearBootFreqEnd, f0 + 1.0);
                float cycles = tau * f0 + 0.5 * (f1 - f0) * tau * tau / bootSec;
                float on = step(0.5, frac(cycles));
                float strobe = lerp(_AppearBootDim, 1.0, on);
                return ramp * strobe;
            }

            float BootBrightnessMul()
            {
                if (!BootActive())
                    return 1.0;

                float bootSec = max(_AppearBootSec, 0.05);
                return smoothstep(0.0, 1.0, _AppearBootElapsed / bootSec);
            }

            float DistanceFade(float linearDepth)
            {
                float fadeM = max(_DistanceFadeMeters, 1.0);
                return saturate((_MaxLidarDistance - linearDepth) / fadeM);
            }

            float ApplyTerrainMask(float edge, float linearDepth)
            {
                return edge * TerrainRangeMask(linearDepth);
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                if (_DebugShaderMode > 5.5)
                {
                    float2 texelDbg = float2(_ScreenParams.z - 1.0, _ScreenParams.w - 1.0);
                    float edgeDbg = DepthEdge(uv, texelDbg);
                    return fixed4(edgeDbg, edgeDbg, edgeDbg, 1.0);
                }

                if (_DebugShaderMode > 4.5)
                    return fixed4(_LidarColor.rgb, 1.0);

                fixed4 scene = tex2D(_BlitTexture, uv);

                if (_EffectBlend <= 0.001)
                    return scene;

                float2 texel = float2(_ScreenParams.z - 1.0, _ScreenParams.w - 1.0);
                float deviceDepth = SampleDepth(uv);
                float linearDepth = LinearEyeDepth(deviceDepth);
                float edge = DepthEdge(uv, texel);
                float terrainEdge = ApplyTerrainMask(edge, linearDepth);

                if (_DebugShaderMode > 3.5)
                    return fixed4(linearDepth / max(_MaxLidarDistance, 1.0), linearDepth / max(_MaxLidarDistance, 1.0), linearDepth / max(_MaxLidarDistance, 1.0), 1.0);

                if (_DebugShaderMode > 2.5)
                    return fixed4(lerp(scene.rgb, _LidarColor.rgb, terrainEdge * _EffectBlend), 1.0);

                if (_DebugShaderMode > 1.5)
                {
                    float coneMask = ConeMask(ViewRay(uv));
                    return fixed4(lerp(fixed3(1, 0, 0), fixed3(0, 1, 0), coneMask), 1.0);
                }

                if (_DebugShaderMode > 0.5)
                {
                    float contour = terrainEdge * _EffectBlend;
                    return fixed4(lerp(scene.rgb, _LidarColor.rgb, contour * _LidarColor.a), 1.0);
                }

                if (_DebugBypass > 0.5)
                    return fixed4(lerp(scene.rgb, _LidarColor.rgb, terrainEdge * _EffectBlend), 1.0);

                float cone = CombatConeMask(ViewRay(uv));
                float contourCombat = terrainEdge * cone;
                contourCombat = ApplyHudIntensity(uv, contourCombat);
                contourCombat *= DistanceFade(linearDepth);
                float visibility = BootActive() ? BootContourVisibility() : _EffectBlend;
                contourCombat *= visibility;
                fixed3 lidarRgb = _LidarColor.rgb * _HudBrightness * BootBrightnessMul();
                fixed3 outRgb = lerp(scene.rgb, lidarRgb, contourCombat * _LidarColor.a);
                return fixed4(outRgb, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
