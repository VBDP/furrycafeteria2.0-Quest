Shader "Unlit/SkyTexture"
{
    Properties
    {
        _Maintex("Texture",2D) = "black" {}
    	_MinDist("Minimum Distance",Float) = 200
    }
    SubShader
    {
        Tags { "Queue" = "Overlay+9999" "RenderType" = "Background" "PreviewType" = "Skybox" }
		Cull Off 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float _MinDist;
            sampler2D _Maintex;

            v2f vert (appdata v)
            {
                v2f o;

                float3 objectWorldPosition = unity_ObjectToWorld._m03_m13_m23;
                if(distance(objectWorldPosition, _WorldSpaceCameraPos)> _MinDist)
                {
                    o.vertex = 0;
                    return o;
                }

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex.xy = v.uv*4-1;
                o.vertex.w = 1;
                o.uv = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                i.uv.x += clamp(UNITY_MATRIX_P._13,-1,1) * 0.03f;

                return tex2D(_Maintex,i.uv);
            }
            ENDCG
        }
    }
}
