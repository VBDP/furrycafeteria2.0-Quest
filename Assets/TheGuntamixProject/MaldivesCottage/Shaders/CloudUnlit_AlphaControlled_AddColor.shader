Shader "CloudUnlit_AlphaControlled_AddColor"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _ColorMultiplier ("Add Color", Color) = (1, 1, 1, 1)
        _EmissionStrength ("Emission Strength", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha // ← 通常のアルファ透過
        Cull Back
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ColorMultiplier;
            float _EmissionStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float3 emission = tex.rgb * _EmissionStrength + _ColorMultiplier.rgb;

                // アルファチャンネルそのまま出力（透過度として使う）
                return fixed4(emission, tex.a);
            }
            ENDCG
        }
    }
}
