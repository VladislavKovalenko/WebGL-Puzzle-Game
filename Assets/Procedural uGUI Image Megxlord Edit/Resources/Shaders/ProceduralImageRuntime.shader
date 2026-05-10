Shader "UI/Procedural UI Image Gradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Toggle] _UseGradient ("Use Gradient", Float) = 0
        _GradientColor1 ("Color 1", Color) = (1,1,1,1)
        _GradientColor2 ("Color 2", Color) = (1,1,0,1)
        _GradientColor3 ("Color 3", Color) = (1,0,0,1)
        
        _GradientAngle ("Angle", Float) = 0
        _MiddlePoint ("Middle Point", Range(0.01, 0.99)) = 0.5
        [Toggle] _ThreeColors ("Use 3 Colors", Float) = 0

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
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP
          
            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : POSITION;
                fixed4 color    : COLOR;
                float4 worldPosition : TEXCOORD0;
                float4 radius : TEXCOORD1;
                float2 texcoord  : TEXCOORD2;
                float2 wh : TEXCOORD3;
                float lineWeight : TEXCOORD4;
                float pixelWorldScale : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };
          
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            // Gradient Properties
            float _UseGradient;
            float _ThreeColors;
            fixed4 _GradientColor1;
            fixed4 _GradientColor2;
            fixed4 _GradientColor3;
            float _GradientAngle;
            float _MiddlePoint;

            float2 decode2(float value) {
                float2 kEncodeMul = float2(1.0, 65535.0f);
                return frac(kEncodeMul * value);
            }
          
            v2f vert(appdata_t IN){
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = IN.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
             
                OUT.wh = IN.uv1;
                OUT.texcoord = TRANSFORM_TEX(IN.uv0, _MainTex);

                float minside = min(OUT.wh.x, OUT.wh.y);
                OUT.lineWeight = IN.uv3.x * minside / 2.0;
                OUT.radius = float4(decode2(IN.uv2.x), decode2(IN.uv2.y)) * minside;
                OUT.pixelWorldScale = clamp(IN.uv3.y, 0.01, 2048.0);
             
                OUT.color = IN.color * _Color;
                return OUT;
            }
          
            half visible(float2 pos, float4 r, float2 wh){
                half4 p = half4(pos, wh.x - pos.x, wh.y - pos.y);
                half v = min(min(min(p.x, p.y), p.z), p.w);
                bool4 b = bool4(all(p.xw < r[0]), all(p.zw < r[1]), all(p.zy < r[2]), all(p.xy < r[3]));
                half4 vis = r - half4(length(p.xw - r[0]), length(p.zw - r[1]), length(p.zy - r[2]), length(p.xy - r[3]));
                half4 foo = min(b * max(vis, 0), v) + (1 - b) * v;
                return any(b) * min(min(min(foo.x, foo.y), foo.z), foo.w) + v * (1 - any(b));
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd;
                half4 tintColor = IN.color;

                if (_UseGradient > 0.5)
                {
                    float angleRad = _GradientAngle * 3.14159265 / 180.0;
                    float2 dir = float2(cos(angleRad), sin(angleRad));
                    // Центрируем UV и проецируем на направление угла
                    float t = dot(IN.texcoord - 0.5, dir) + 0.5;
                    t = saturate(t);

                    half4 grad;
                    if (_ThreeColors > 0.5)
                    {
                        // Логика трех цветов с учетом Middle Point
                        if (t < _MiddlePoint)
                            grad = lerp(_GradientColor1, _GradientColor2, t / _MiddlePoint);
                        else
                            grad = lerp(_GradientColor2, _GradientColor3, (t - _MiddlePoint) / (1.0 - _MiddlePoint));
                    }
                    else
                    {
                        grad = lerp(_GradientColor1, _GradientColor2, t);
                    }

                    tintColor.rgb *= grad.rgb;
                    tintColor.a *= grad.a;
                }

                half4 color = texColor * tintColor;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                half v = visible(IN.texcoord * IN.wh, IN.radius, IN.wh);
                float l = (IN.lineWeight + 1.0 / IN.pixelWorldScale) / 2.0;
                color.a *= saturate((l - distance(v, l)) * IN.pixelWorldScale);
             
                if(color.a <= 0) discard;

                return color;
            }
            ENDCG
        }
    }
}