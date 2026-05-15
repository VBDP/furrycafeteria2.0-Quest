Shader "VFF/GrabPassGrabber"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"ForceNoShadowCasting" = "True"}
		LOD 100

		ZTest Always
        ZWrite Off

		GrabPass
		{
			"_IkeiwaScreenBlocker"
		}

		Pass
		{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 vert(float2 uv : TEXCOORD0) : SV_POSITION
            {
                return float4(float2(1,-1) * (uv * 2 - 1),0,1);
            }

            struct fragOutput {
                float4 col:SV_Target;
                //float dep : SV_Depth;
            };

            fragOutput frag()
            {
                fragOutput o;
                /*#if UNITY_REVERSED_Z
                    o.dep = 1;
                #else
                    o.dep = 0;
                #endif*/
                o.col = 0;
                return o;
            }
            ENDCG
		}
    }
}
