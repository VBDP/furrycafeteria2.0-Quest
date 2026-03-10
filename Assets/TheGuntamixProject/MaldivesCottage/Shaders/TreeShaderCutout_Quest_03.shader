Shader "TreeShaderCutout_Quest_03"
{
    Properties
    {
        _AlbedoMap("Albedo Map", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalScale("Normal Scale", Float) = 1
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        _MultiplyColor("Multiply Color", Color) = (1,1,1,1)
        _AddColor("Add Color", Color) = (0,0,0,0)
        _EmissionMask("Emission Mask", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _SpecularIntensity("Specular Intensity", Range(0, 2)) = 0.5
        _SpecularPower("Specular Sharpness", Range(1, 64)) = 16
        _CustomSpecColor("Specular Color", Color) = (0.1,0.1,0.1,1)
        _WindAmpXYZ("Wind Amp XYZ", Vector) = (0.1, 0.01, 0.1, 0)
        _WindSpeedXYZ("Wind Speed XYZ", Vector) = (2, 2, 1, 0)
        _WindWaveXYZ("Wind Wave XYZ", Vector) = (0.3, 1, 0.3, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf StandardSpecular fullforwardshadows alpha:clip vertex:vert
        #pragma target 2.0

        sampler2D _AlbedoMap;
        sampler2D _NormalMap;
        sampler2D _EmissionMask;

        fixed4 _MultiplyColor;
        fixed4 _AddColor;
        fixed4 _EmissionColor;
        fixed4 _CustomSpecColor;

        float _NormalScale;
        float _Cutoff;
        float _SpecularIntensity;
        float _SpecularPower;

        float4 _WindAmpXYZ;
        float4 _WindSpeedXYZ;
        float4 _WindWaveXYZ;

        struct Input {
            float2 uv_AlbedoMap;
            float2 uv_NormalMap;
            float2 uv_EmissionMask;
            float3 viewDir;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 pos = v.vertex.xyz;
            float3 offset;
            offset.x = _WindAmpXYZ.x * sin(_Time.y * _WindSpeedXYZ.x + pos.x * _WindWaveXYZ.x);
            offset.y = _WindAmpXYZ.y * sin(_Time.y * _WindSpeedXYZ.y + pos.y * _WindWaveXYZ.y);
            offset.z = _WindAmpXYZ.z * sin(_Time.y * _WindSpeedXYZ.z + pos.z * _WindWaveXYZ.z);
            v.vertex.xyz += offset * v.color.a;
        }

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            fixed4 albedoTex = tex2D(_AlbedoMap, IN.uv_AlbedoMap);
            clip(albedoTex.a - _Cutoff);

            o.Albedo = (albedoTex.rgb + _AddColor.rgb) * _MultiplyColor.rgb;

            float3 normalTex = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap)) * _NormalScale;
            o.Normal = normalTex;

            o.Emission = tex2D(_EmissionMask, IN.uv_EmissionMask).rgb * _EmissionColor.rgb;
            o.Specular = _CustomSpecColor.rgb * _SpecularIntensity;
            o.Smoothness = saturate(1.0 - (1.0 / _SpecularPower));
            o.Alpha = 1.0;
        }
        ENDCG

        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _AlbedoMap;
            float _Cutoff;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target {
                float alpha = tex2D(_AlbedoMap, i.uv).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDCG
        }
    }
}
