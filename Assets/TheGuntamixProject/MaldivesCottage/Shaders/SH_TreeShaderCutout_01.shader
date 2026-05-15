Shader "SH_TreeShaderCutout_01"
{
	Properties
	{
		_NormalMap("Normal Map", 2D) = "white" {}
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_NormalScale("Normal Scale", Float) = 1
		_AlbedoMap("Albedo Map", 2D) = "white" {}
		_MultiplyColor("Multiply Color", Color) = (1,1,1,0)
		_AddColor("Add Color", Color) = (0,0,0,0)
		_EmissionMask("Emission Mask", 2D) = "white" {}
		_Metalic("Metalic", 2D) = "white" {}
		_EmissionColor("Emission Color", Color) = (0,0,0,0)
		_MetaricColor("Metaric Color", Color) = (0,0,0,0)
		_WindAmpXYZ("Wind Amp XYZ", Vector) = (0.1,0.01,0.1,0)
		_WindSpeedXYZ("Wind Speed XYZ", Vector) = (2,2,1,0)
		_WindWaveXYZ("Wind Wave XYZ", Vector) = (0.3,1,0.3,0)
		_Smoothness("Smoothness", Range( 0 , 1)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows exclude_path:deferred vertex:vertexDataFunc 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float3 _WindAmpXYZ;
		uniform float3 _WindSpeedXYZ;
		uniform float3 _WindWaveXYZ;
		uniform sampler2D _NormalMap;
		uniform float4 _NormalMap_ST;
		uniform float _NormalScale;
		uniform sampler2D _AlbedoMap;
		uniform float4 _AlbedoMap_ST;
		uniform float4 _AddColor;
		uniform float4 _MultiplyColor;
		uniform float4 _EmissionColor;
		uniform sampler2D _EmissionMask;
		uniform float4 _EmissionMask_ST;
		uniform float4 _MetaricColor;
		uniform sampler2D _Metalic;
		uniform float4 _Metalic_ST;
		uniform float _Smoothness;
		uniform float _Cutoff = 0.5;

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float mulTime50 = _Time.y * _WindSpeedXYZ.x;
			float3 ase_vertex3Pos = v.vertex.xyz;
			float mulTime51 = _Time.y * _WindSpeedXYZ.y;
			float mulTime52 = _Time.y * _WindSpeedXYZ.z;
			float4 appendResult26 = (float4(( _WindAmpXYZ.x * sin( ( mulTime50 + ( _WindWaveXYZ.x * ase_vertex3Pos.x ) ) ) ) , ( _WindAmpXYZ.y * sin( ( mulTime51 + ( _WindWaveXYZ.y * ase_vertex3Pos.y ) ) ) ) , ( _WindAmpXYZ.z * sin( ( mulTime52 + ( _WindWaveXYZ.z * ase_vertex3Pos.z ) ) ) ) , 0.0));
			v.vertex.xyz += ( appendResult26 * v.color ).xyz;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_NormalMap = i.uv_texcoord * _NormalMap_ST.xy + _NormalMap_ST.zw;
			o.Normal = UnpackScaleNormal( tex2D( _NormalMap, uv_NormalMap ), _NormalScale );
			float2 uv_AlbedoMap = i.uv_texcoord * _AlbedoMap_ST.xy + _AlbedoMap_ST.zw;
			float4 tex2DNode14 = tex2D( _AlbedoMap, uv_AlbedoMap );
			o.Albedo = ( ( tex2DNode14 + _AddColor ) * _MultiplyColor ).rgb;
			float2 uv_EmissionMask = i.uv_texcoord * _EmissionMask_ST.xy + _EmissionMask_ST.zw;
			o.Emission = ( _EmissionColor * tex2D( _EmissionMask, uv_EmissionMask ) ).rgb;
			float2 uv_Metalic = i.uv_texcoord * _Metalic_ST.xy + _Metalic_ST.zw;
			o.Metallic = ( _MetaricColor * tex2D( _Metalic, uv_Metalic ) ).r;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
			clip( tex2DNode14.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
}