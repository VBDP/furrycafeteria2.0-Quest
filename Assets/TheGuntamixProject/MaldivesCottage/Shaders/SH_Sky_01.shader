Shader "SH_Sky_01"
{
	Properties
	{
		_Albedo("Albedo", 2D) = "white" {}
		_SkyEmissionStrength("Sky Emission Strength", Range( 0 , 2)) = 1.1
		_ColorMultipler("Color Multipler", Color) = (0.1411765,0.4980392,0.6,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Unlit keepalpha noshadow nofog 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform sampler2D _Albedo;
		uniform float4 _Albedo_ST;
		uniform float _SkyEmissionStrength;
		uniform float4 _ColorMultipler;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
			o.Emission = ( ( tex2D( _Albedo, uv_Albedo ) * _SkyEmissionStrength ) + _ColorMultipler ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
}