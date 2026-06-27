Shader "Hidden/ACT/LidarDepthEdge"
{
    Properties
    {
        _MainTex ("Depth", 2D) = "white" {}
        _EdgeThreshold ("Edge Threshold", Float) = 0.20
        _EdgeStrength ("Edge Strength", Float) = 1.6
        _EdgeThinPow ("Edge Thin Pow", Float) = 4.2
        _EdgeTexelScale ("Edge Texel Scale", Float) = 0.50
    }
    SubShader
    {
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _EdgeThreshold;
            float _EdgeStrength;
            float _EdgeThinPow;
            float _EdgeTexelScale;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                o.positionCS = float4(o.uv * 2.0 - 1.0, 0.0, 1.0);
                #if UNITY_UV_STARTS_AT_TOP
                o.uv.y = 1.0 - o.uv.y;
                #endif
                return o;
            }

            float LinearDepthAt(float2 uv)
            {
                return LinearEyeDepth(tex2D(_MainTex, uv).r);
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

            fixed4 frag(Varyings i) : SV_Target
            {
                float2 texel = float2(_ScreenParams.z - 1.0, _ScreenParams.w - 1.0);
                float edge = ContourEdge(i.uv, texel);
                return fixed4(edge, edge, edge, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
