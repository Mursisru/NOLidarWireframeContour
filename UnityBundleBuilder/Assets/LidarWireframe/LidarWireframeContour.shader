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
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.15
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
            float _NoiseStrength;
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

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
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
                float dC = LinearDepthAt(uv);
                float dL = LinearDepthAt(uv - float2(texel.x, 0.0));
                float dR = LinearDepthAt(uv + float2(texel.x, 0.0));
                float dU = LinearDepthAt(uv + float2(0.0, texel.y));
                float dD = LinearDepthAt(uv - float2(0.0, texel.y));

                float lap = abs(4.0 * dC - dL - dR - dU - dD);
                float depthRef = max(dC, 40.0);
                float lapThresh = _EdgeThreshold * lerp(0.15, 0.75, saturate(depthRef / 1100.0));

                float lapEdge = smoothstep(lapThresh, lapThresh * 1.12, lap);
                lapEdge *= 1.0 - smoothstep(lapThresh * 2.2, lapThresh * 4.5, lap);

                float rampX = abs(dC - 0.5 * (dL + dR));
                float rampY = abs(dC - 0.5 * (dU + dD));
                float rampSum = rampX + rampY;
                float slopeWash = smoothstep(0.0012, 0.028, rampSum);
                lapEdge *= 1.0 - slopeWash * 0.92;

                float lapL = abs(4.0 * dL - LinearDepthAt(uv - float2(texel.x * 2.0, 0.0)) - dC - LinearDepthAt(uv - float2(texel.x, texel.y)) - LinearDepthAt(uv - float2(texel.x, -texel.y)));
                float lapR = abs(4.0 * dR - dC - LinearDepthAt(uv + float2(texel.x * 2.0, 0.0)) - LinearDepthAt(uv + float2(texel.x, texel.y)) - LinearDepthAt(uv + float2(texel.x, -texel.y)));
                float lapU = abs(4.0 * dU - LinearDepthAt(uv + float2(0.0, texel.y * 2.0)) - dC - LinearDepthAt(uv + float2(texel.x, texel.y)) - LinearDepthAt(uv - float2(texel.x, texel.y)));
                float lapD = abs(4.0 * dD - dC - LinearDepthAt(uv - float2(0.0, texel.y * 2.0)) - LinearDepthAt(uv - float2(texel.x, -texel.y)) - LinearDepthAt(uv + float2(texel.x, -texel.y)));
                float nms = step(lap, lapL) * step(lap, lapR) * step(lap, lapU) * step(lap, lapD);
                lapEdge *= lerp(0.4, 1.0, nms);

                if (depthRef > 220.0)
                {
                    float2 t2 = texel * 2.0;
                    float dL2 = LinearDepthAt(uv - float2(t2.x, 0.0));
                    float dR2 = LinearDepthAt(uv + float2(t2.x, 0.0));
                    float dU2 = LinearDepthAt(uv + float2(0.0, t2.y));
                    float dD2 = LinearDepthAt(uv - float2(0.0, t2.y));
                    float lap2 = abs(4.0 * dC - dL2 - dR2 - dU2 - dD2);
                    float farThresh = lapThresh * 0.85;
                    lapEdge = max(lapEdge, smoothstep(farThresh, farThresh * 1.15, lap2) * 0.88);
                }

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
                float impactNear = max(15.0, _ImpactDistance * 0.12);
                return min(_NearClipM, impactNear);
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
                float dotVal = dot(viewRay, lidarDir);
                return saturate((dotVal - _ConeCosHalfAngle) / max(1.0 - _ConeCosHalfAngle, 1e-4));
            }

            float ImpactDepthMask(float linearDepth)
            {
                float bandMin = max(EffectiveNearClipM(), _ImpactDistance - _ImpactBandHalfM);
                float bandMax = min(_MaxLidarDistance, _ImpactDistance + _ImpactBandHalfM * 1.5);
                float edgeFade = max(_ImpactBandHalfM * 0.15, 8.0);
                float lo = smoothstep(bandMin - edgeFade, bandMin + edgeFade, linearDepth);
                float hi = 1.0 - smoothstep(bandMax - edgeFade, bandMax + edgeFade, linearDepth);
                return lo * hi;
            }

            float CombatConeMask(float3 viewRay)
            {
                float raw = ConeMask(viewRay);
                return lerp(0.55, 1.0, raw);
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
                float contourCombat = terrainEdge * cone * ImpactDepthMask(linearDepth);
                contourCombat *= 1.0 + _NoiseStrength * (Hash21(uv * 400.0) - 0.5) * 0.3;
                contourCombat *= _EffectBlend;
                fixed3 outRgb = lerp(scene.rgb, _LidarColor.rgb, contourCombat * _LidarColor.a);
                return fixed4(outRgb, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
