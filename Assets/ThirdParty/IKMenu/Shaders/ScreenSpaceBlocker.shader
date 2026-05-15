Shader "VFF/ScreenSpaceBlocker"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "Queue" = "Overlay+2145287568"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent" }
        LOD 100

        ZTest Always
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 grabPos : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _IkeiwaScreenBlocker;

            v2f vert (float2 uv : TEXCOORD0)
            {
                v2f o;
                o.vertex = float4(float2(1, -1) * (uv * 2 - 1), 0, 1);
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                return tex2D(_IkeiwaScreenBlocker, i.grabPos.xy / i.grabPos.w);
            }
            ENDCG
        }
    }
}
