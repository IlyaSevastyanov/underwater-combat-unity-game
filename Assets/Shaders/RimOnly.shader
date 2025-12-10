Shader "Hidden/Fish/RimOnly"
{
    Properties
    {
        _Color     ("Rim Color", Color) = (1,0.5,0,1)
        _Intensity ("Intensity", Range(0,8)) = 4
        _RimPower  ("Rim Power", Range(0.2,8)) = 2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }

        Cull Back
        ZWrite Off
        // Яркое аддитивное свечение
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float3 worldN : TEXCOORD0;
                float3 worldV : TEXCOORD1;
            };

            fixed4 _Color;
            float  _Intensity;
            float  _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);

                float3 wPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.worldN = UnityObjectToWorldNormal(v.normal);
                o.worldV = _WorldSpaceCameraPos - wPos;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.worldN);
                float3 v = normalize(i.worldV);

                // классический френель
                float ndv  = saturate(dot(n, v));
                float rim  = pow(1.0 - ndv, _RimPower);

                float  a = saturate(rim);                  // альфа по краю
                float3 c = _Color.rgb * (_Intensity * rim);

                return fixed4(c, a);
            }
            ENDCG
        }
    }
    Fallback Off
}
