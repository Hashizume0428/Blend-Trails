Shader "Color Puzzle/Stage Palette UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _FirstColor;
            float4 _SecondColor;
            float4 _MixedColor;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            // Recolor only the three authored ink hues. Gray, white, shadows and alpha stay intact.
            float3 PaletteInk(float3 rgb)
            {
                float low = min(rgb.r, min(rgb.g, rgb.b));
                float chroma = max(rgb.r, max(rgb.g, rgb.b)) - low;
                if (chroma < 0.01) return rgb;
                float3 hue = (rgb - low) / chroma;
                float3 source = float3(229,75,75) / 255.0;
                float3 target = _FirstColor.rgb;
                float distance = dot(hue - float3(1,0,0), hue - float3(1,0,0));
                float3 blueHue = float3(0,67.0/154.0,1);
                float blueDistance = dot(hue - blueHue, hue - blueHue);
                if (blueDistance < distance)
                {
                    distance = blueDistance;
                    source = float3(75,142,229) / 255.0;
                    target = _SecondColor.rgb;
                }
                float3 purpleHue = float3(116.0/154.0,0,1);
                float purpleDistance = dot(hue - purpleHue, hue - purpleHue);
                if (purpleDistance < distance)
                {
                    distance = purpleDistance;
                    source = float3(191,75,229) / 255.0;
                    target = _MixedColor.rgb;
                }
                return distance < 0.03 ? saturate(rgb + (target - source) * chroma / (154.0/255.0)) : rgb;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.texcoord) + _TextureSampleAdd;
                #ifndef UNITY_COLORSPACE_GAMMA
                color.rgb = LinearToGammaSpace(color.rgb);
                #endif
                color.rgb = PaletteInk(color.rgb);
                #ifndef UNITY_COLORSPACE_GAMMA
                color.rgb = GammaToLinearSpace(color.rgb);
                #endif
                color *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
