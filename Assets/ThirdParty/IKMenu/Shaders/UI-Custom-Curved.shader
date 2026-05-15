// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "UI/Custom Curved"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)

        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15

        _CurvePower("Curve Power",Float) = -0.06
        _CurveWidth("Curve Width",Float) = 5.56
        _CurveOffset("Curve Offset",Float) = 0.9

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTestCompare("ZTest", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma only_renderers d3d11
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma require geometry

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct g2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _CurvePower;
            float _CurveWidth;
            float _CurveOffset;

            void curvature(inout float4 vertex) {
                vertex.z += sin((vertex.x + _CurveOffset) * _CurveWidth) * _CurvePower;
            }

            v2g vert(appdata_t v)
            {
                v2g OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = v.vertex;
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                OUT.color = v.color * _Color;
                return OUT;
            }

            v2g subdTri1[12];
            v2g subdTri2[12];

            void subdivideTri1(v2g A,v2g B,v2g C) {
                v2g AB;
                AB.vertex = lerp(A.vertex, B.vertex, 0.5f);
                AB.texcoord = lerp(A.texcoord, B.texcoord, 0.5f);
                AB.color = lerp(A.color, B.color, 0.5f);

                v2g AC;
                AC.vertex = lerp(A.vertex, C.vertex, 0.5f);
                AC.texcoord = lerp(A.texcoord, C.texcoord, 0.5f);
                AC.color = lerp(A.color, C.color, 0.5f);

                v2g CB;
                CB.vertex = lerp(C.vertex, B.vertex, 0.5f);
                CB.texcoord = lerp(C.texcoord, B.texcoord, 0.5f);
                CB.color = lerp(C.color, B.color, 0.5f);

                subdTri1[0] = A;
                subdTri1[1] = AB;
                subdTri1[2] = AC;
                subdTri1[3] = AC;
                subdTri1[4] = AB;
                subdTri1[5] = CB;
                subdTri1[6] = AB;
                subdTri1[7] = B;
                subdTri1[8] = CB;
                subdTri1[9] = AC;
                subdTri1[10] = CB;
                subdTri1[11] = C;
            }

            void subdivideTri2(v2g A, v2g B, v2g C) {
                v2g AB;
                AB.vertex = lerp(A.vertex, B.vertex, 0.5f);
                AB.texcoord = lerp(A.texcoord, B.texcoord, 0.5f);
                AB.color = lerp(A.color, B.color, 0.5f);

                v2g AC;
                AC.vertex = lerp(A.vertex, C.vertex, 0.5f);
                AC.texcoord = lerp(A.texcoord, C.texcoord, 0.5f);
                AC.color = lerp(A.color, C.color, 0.5f);

                v2g CB;
                CB.vertex = lerp(C.vertex, B.vertex, 0.5f);
                CB.texcoord = lerp(C.texcoord, B.texcoord, 0.5f);
                CB.color = lerp(C.color, B.color, 0.5f);

                subdTri2[0] = A;
                subdTri2[1] = AB;
                subdTri2[2] = AC;
                subdTri2[3] = AC;
                subdTri2[4] = AB;
                subdTri2[5] = CB;
                subdTri2[6] = AB;
                subdTri2[7] = B;
                subdTri2[8] = CB;
                subdTri2[9] = AC;
                subdTri2[10] = CB;
                subdTri2[11] = C;
            }

            [maxvertexcount(48)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
            {
                subdivideTri1(input[0], input[1], input[2]);

                for (int i = 0; i < 12; i+=3)
                {
                    subdivideTri2(subdTri1[i], subdTri1[i+1], subdTri1[i+2]);

                    for (int j = 0; j < 12; j += 3)
                    {

                        g2f o;

                        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(o);
                        curvature(subdTri2[j].vertex); 
                        o.vertex = UnityObjectToClipPos(subdTri2[j].vertex);
                        o.texcoord = subdTri2[j].texcoord;
                        o.color = subdTri2[j].color;
                        o.worldPosition = subdTri2[j].vertex;
                        triStream.Append(o);   

                        curvature(subdTri2[j+1].vertex);
                        o.vertex = UnityObjectToClipPos(subdTri2[j + 1].vertex);
                        o.texcoord = subdTri2[j + 1].texcoord;
                        o.color = subdTri2[j + 1].color;
                        o.worldPosition = subdTri2[j + 1].vertex;
                        triStream.Append(o);

                        curvature(subdTri2[j+2].vertex);
                        o.vertex = UnityObjectToClipPos(subdTri2[j + 2].vertex);
                        o.texcoord = subdTri2[j + 2].texcoord;
                        o.color = subdTri2[j + 2].color;
                        o.worldPosition = subdTri2[j + 2].vertex;
                        triStream.Append(o);

                        triStream.RestartStrip();
                    }
                }
            }

            fixed4 frag(g2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma only_renderers gles3
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.worldPosition = v.vertex;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
