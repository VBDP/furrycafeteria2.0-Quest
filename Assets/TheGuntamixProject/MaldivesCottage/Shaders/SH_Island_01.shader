Shader "SH_Island_01"
{
	Properties
	{
		_Albedo("Albedo", 2D) = "white" {}
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_SkyEmissionStrength("Sky Emission Strength", Range( 0 , 2)) = 1.26
		_ColorMultipler("Color Multipler", Color) = (0.1294118,0.5333334,0.654902,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
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
		uniform float _Cutoff = 0.5;

		inline half4 LightingUnlit( SurfaceOutput s, half3 lightDir, half atten )
		{
			return half4 ( 0, 0, 0, s.Alpha );
		}

		void surf( Input i , inout SurfaceOutput o )
		{
			float2 uv_Albedo = i.uv_texcoord * _Albedo_ST.xy + _Albedo_ST.zw;
			float4 tex2DNode12 = tex2D( _Albedo, uv_Albedo );
			o.Emission = ( ( tex2DNode12 * _SkyEmissionStrength ) + _ColorMultipler ).rgb;
			o.Alpha = 1;
			clip( tex2DNode12.a - _Cutoff );
		}

		ENDCG
	}
}