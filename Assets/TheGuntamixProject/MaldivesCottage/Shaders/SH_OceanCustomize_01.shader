Shader "SH_OceanCustomize_01"
{
	Properties
	{
		_WaveSpeed("Wave Speed", Float) = 1
		_WeveTile("Weve Tile", Float) = 1
		_WeveHeght("Weve Heght", Float) = 1
		_Watercolor("Water color", Color) = (0.2480123,0.5693429,0.7169812,0)
		_TopColor("Top Color", Color) = (0.2684229,0.7300734,0.8207547,0)
		_EdgeDistance("Edge Distance", Float) = 1
		_EdgePower("Edge Power", Range( 0 , 1)) = 1
		_NormalMap("NormalMap", 2D) = "white" {}
		_NormalSpeed("Normal Speed", Float) = 1
		_FoamSpeed("Foam Speed", Float) = 1
		_NormalStrength("Normal Strength", Range( 0 , 1)) = 1
		_NormalTile("Normal Tile", Float) = 1
		_SeaFoam("Sea Foam", 2D) = "white" {}
		_EdgeFoamTile("Edge Foam Tile", Float) = 1
		_SeaFormTile("Sea Form Tile", Float) = 1
		_FoamMaskStrength("Foam Mask Strength", Range( 0 , 2)) = 1
		_Depth("Depth", Float) = -4
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		GrabPass{ }
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#include "UnityStandardUtils.cginc"
		#include "UnityCG.cginc"
		#include "Tessellation.cginc"
		#pragma target 4.6
		#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex);
		#else
		#define ASE_DECLARE_SCREENSPACE_TEXTURE(tex) UNITY_DECLARE_SCREENSPACE_TEXTURE(tex)
		#endif
		#pragma surface surf Standard keepalpha noshadow vertex:vertexDataFunc tessellate:tessFunction 
		struct Input
		{
			float3 worldPos;
			float4 screenPos;
		};

		uniform float _WeveHeght;
		uniform float _WaveSpeed;
		uniform float _WeveTile;
		uniform sampler2D _NormalMap;
		uniform float _NormalSpeed;
		uniform float _NormalTile;
		uniform float _NormalStrength;
		uniform float4 _Watercolor;
		uniform float4 _TopColor;
		uniform sampler2D _SeaFoam;
		uniform float _SeaFormTile;
		uniform float _FoamMaskStrength;
		ASE_DECLARE_SCREENSPACE_TEXTURE( _GrabTexture )
		UNITY_DECLARE_DEPTH_TEXTURE( _CameraDepthTexture );
		uniform float4 _CameraDepthTexture_TexelSize;
		uniform float _Depth;
		uniform float _EdgeDistance;
		uniform float _FoamSpeed;
		uniform float _EdgeFoamTile;
		uniform float _EdgePower;


		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }

		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
		}


		inline float4 ASE_ComputeGrabScreenPos( float4 pos )
		{
			#if UNITY_UV_STARTS_AT_TOP
			float scale = -1.0;
			#else
			float scale = 1.0;
			#endif
			float4 o = pos;
			o.y = pos.w * 0.5f;
			o.y = ( pos.y - o.y ) * _ProjectionParams.x * scale + o.y;
			return o;
		}


		float4 tessFunction( appdata_full v0, appdata_full v1, appdata_full v2 )
		{
			float4 Tesselation145 = UnityDistanceBasedTess( v0.vertex, v1.vertex, v2.vertex, 0.0,80.0,( _WeveHeght * 8.0 ));
			return Tesselation145;
		}

		void vertexDataFunc( inout appdata_full v )
		{
			float temp_output_7_0 = ( _Time.y * _WaveSpeed );
			float2 _WeveDirection = float2(-1,0);
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float4 appendResult11 = (float4(ase_worldPos.x , ase_worldPos.z , 0.0 , 0.0));
			float4 WorldSpaceTile14 = appendResult11;
			float4 WaveTileUV27 = ( ( WorldSpaceTile14 * float4( float2( 0.15,0.02 ), 0.0 , 0.0 ) ) * _WeveTile );
			float2 panner3 = ( temp_output_7_0 * _WeveDirection + WaveTileUV27.xy);
			float simplePerlin2D1 = snoise( panner3 );
			simplePerlin2D1 = simplePerlin2D1*0.5 + 0.5;
			float2 panner29 = ( temp_output_7_0 * _WeveDirection + ( WorldSpaceTile14 * float4( 0.1,0.1,0,0 ) ).xy);
			float simplePerlin2D30 = snoise( panner29*0.1 );
			simplePerlin2D30 = simplePerlin2D30*0.5 + 0.5;
			float temp_output_32_0 = ( simplePerlin2D1 + simplePerlin2D30 );
			float3 WeveHeight37 = ( ( float3(0,1,0) * _WeveHeght ) * temp_output_32_0 );
			v.vertex.xyz += WeveHeight37;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float3 ase_worldPos = i.worldPos;
			float4 appendResult11 = (float4(ase_worldPos.x , ase_worldPos.z , 0.0 , 0.0));
			float4 WorldSpaceTile14 = appendResult11;
			float4 temp_output_81_0 = ( WorldSpaceTile14 * 10.0 );
			float2 panner67 = ( 1.0 * _Time.y * ( float2( 1,0 ) * _NormalSpeed ) + ( temp_output_81_0 * _NormalTile ).xy);
			float2 panner68 = ( 1.0 * _Time.y * ( float2( -1,0 ) * ( _NormalSpeed * 3.0 ) ) + ( temp_output_81_0 * ( _NormalTile * 5.0 ) ).xy);
			float3 Normals78 = BlendNormals( UnpackScaleNormal( tex2D( _NormalMap, panner67 ), _NormalStrength ) , UnpackScaleNormal( tex2D( _NormalMap, panner68 ), _NormalStrength ) );
			o.Normal = Normals78;
			float2 panner112 = ( 1.0 * _Time.y * float2( 0.04,-0.03 ) + ( WorldSpaceTile14 * 0.03 ).xy);
			float simplePerlin2D111 = snoise( panner112 );
			simplePerlin2D111 = simplePerlin2D111*0.5 + 0.5;
			float clampResult118 = clamp( ( tex2D( _SeaFoam, ( ( WorldSpaceTile14 / 1.0 ) * _SeaFormTile ).xy ).r * ( _FoamMaskStrength * simplePerlin2D111 ) ) , 0.0 , 1.0 );
			float SeaFoam108 = clampResult118;
			float temp_output_7_0 = ( _Time.y * _WaveSpeed );
			float2 _WeveDirection = float2(-1,0);
			float4 WaveTileUV27 = ( ( WorldSpaceTile14 * float4( float2( 0.15,0.02 ), 0.0 , 0.0 ) ) * _WeveTile );
			float2 panner3 = ( temp_output_7_0 * _WeveDirection + WaveTileUV27.xy);
			float simplePerlin2D1 = snoise( panner3 );
			simplePerlin2D1 = simplePerlin2D1*0.5 + 0.5;
			float2 panner29 = ( temp_output_7_0 * _WeveDirection + ( WorldSpaceTile14 * float4( 0.1,0.1,0,0 ) ).xy);
			float simplePerlin2D30 = snoise( panner29*0.1 );
			simplePerlin2D30 = simplePerlin2D30*0.5 + 0.5;
			float temp_output_32_0 = ( simplePerlin2D1 + simplePerlin2D30 );
			float WevePattern34 = temp_output_32_0;
			float clampResult47 = clamp( WevePattern34 , 0.0 , 1.0 );
			float4 lerpResult45 = lerp( _Watercolor , ( _TopColor + SeaFoam108 ) , clampResult47);
			float4 Albedo48 = lerpResult45;
			float4 ase_screenPos = float4( i.screenPos.xyz , i.screenPos.w + 0.00000000001 );
			float4 ase_grabScreenPos = ASE_ComputeGrabScreenPos( ase_screenPos );
			float4 ase_grabScreenPosNorm = ase_grabScreenPos / ase_grabScreenPos.w;
			float4 screenColor128 = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GrabTexture,( float3( (ase_grabScreenPosNorm).xy ,  0.0 ) + ( 0.1 * Normals78 ) ).xy);
			float4 clampResult129 = clamp( screenColor128 , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
			float4 Refraction130 = clampResult129;
			float4 ase_screenPosNorm = ase_screenPos / ase_screenPos.w;
			ase_screenPosNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_screenPosNorm.z : ase_screenPosNorm.z * 0.5 + 0.5;
			float screenDepth133 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth133 = abs( ( screenDepth133 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _Depth ) );
			float clampResult135 = clamp( ( 1.0 - distanceDepth133 ) , 0.0 , 1.0 );
			float Depth136 = clampResult135;
			float4 lerpResult137 = lerp( Albedo48 , Refraction130 , Depth136);
			o.Albedo = lerpResult137.rgb;
			float screenDepth51 = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE( _CameraDepthTexture, ase_screenPosNorm.xy ));
			float distanceDepth51 = abs( ( screenDepth51 - LinearEyeDepth( ase_screenPosNorm.z ) ) / ( _EdgeDistance ) );
			float2 panner97 = ( 1.0 * _Time.y * ( float2( 1,0 ) * _FoamSpeed ) + ( ( WorldSpaceTile14 / 1.0 ) * _EdgeFoamTile ).xy);
			float4 clampResult58 = clamp( ( ( ( 1.0 - distanceDepth51 ) + tex2D( _SeaFoam, panner97 ) ) * _EdgePower ) , float4( 0,0,0,0 ) , float4( 1,1,1,0 ) );
			float4 Edge56 = clampResult58;
			o.Emission = Edge56.rgb;
			o.Smoothness = 0.9;
			o.Alpha = 1;
		}

		ENDCG
	}
}