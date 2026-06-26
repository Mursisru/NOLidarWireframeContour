Shader "Hidden/ACT/LidarDepthPack"
{
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
            float4 _MainTex_TexelSize;

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

            fixed4 frag(Varyings i) : SV_Target
            {
                float d = tex2D(_MainTex, i.uv).r;
                return fixed4(d, d, d, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
