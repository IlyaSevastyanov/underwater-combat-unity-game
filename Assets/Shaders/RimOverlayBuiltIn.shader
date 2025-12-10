Shader "Custom/RimOverlayBuiltIn"
{
    Properties
    {
        _RimColor("Rim Color (HDR)", Color) = (1,0.5,0,1)
        _RimPower("Rim Power", Range(0.5,8)) = 2
        _Intensity("Intensity", Range(0,8)) = 2
    }
    SubShader
    {
        Tags{ "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _RimColor;
            float  _RimPower;
            float  _Intensity;

            struct v2f { float4 pos:SV_POSITION; float3 nWS:TEXCOORD0; float3 vWS:TEXCOORD1; };

            v2f vert(appdata_full v)
            {
                v2f o;
                float3 wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.nWS = UnityObjectToWorldNormal(v.normal);
                o.vWS = normalize(_WorldSpaceCameraPos - wpos);
                return o;
            }

            fixed4 frag(v2f i):SV_Target
            {
                float ndv = saturate(dot(normalize(i.nWS), normalize(i.vWS)));
                float rim = pow(1.0 - ndv, _RimPower);
                return float4(_RimColor.rgb * rim * _Intensity, 0);
            }
            ENDCG
        }
    }
}
