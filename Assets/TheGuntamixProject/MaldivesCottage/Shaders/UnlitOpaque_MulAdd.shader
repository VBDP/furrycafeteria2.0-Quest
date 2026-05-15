Shader "Custom/UnlitOpaque_MulAdd"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _MultiplyColor ("Multiply Color", Color) = (1, 1, 1, 1)
        _AddColor ("Add Color", Color) = (0, 0, 0, 1)
        _AddStrength ("Add Strength", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }
        LOD 100

        ZWrite On
        Blend Off
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

            float4 _MultiplyColor;
            float4 _AddColor;
            float _AddStrength;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // テクスチャ読み込み
                fixed4 tex = tex2D(_MainTex, i.uv);

                // 乗算カラー（影のような色変化）
                float3 mulColor = tex.rgb * _MultiplyColor.rgb;

                // 加算カラー（光っぽい効果）
                float3 addColor = _AddColor.rgb * _AddStrength;

                // 合成：乗算＋加算
                float3 finalColor = mulColor + addColor;

                return fixed4(finalColor, 1.0); // アルファは常に1（完全不透明）
            }
            ENDCG
        }
    }
}
