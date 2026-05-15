Shader "SH_Emissive_01"
{
	Properties
	{
		_MaterialColor("MaterialColor", Color) = (1,0.9514286,0.6477987,0)
		_EmissionStrength("EmissionStrength", Range( 0 , 100)) = 2
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha nofog 
		struct Input
		{
			half filler;
		};

		uniform float4 _MaterialColor;
		uniform float _EmissionStrength;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			o.Emission = ( _MaterialColor * _EmissionStrength ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback"Diffuse"
}