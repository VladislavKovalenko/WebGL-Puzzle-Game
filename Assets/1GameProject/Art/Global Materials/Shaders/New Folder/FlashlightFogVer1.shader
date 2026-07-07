Shader "Custom/UI/FlashlightFog"
{
    Properties
    {
        [Header(Flashlight)]
        _FlashlightCenter("Center (Screen Pixels)", Vector) = (0, 0, 0, 0)
        _Radius("Radius (Pixels)", Float) = 150
        _Softness("Edge Softness (Pixels)", Float) = 40

        [Header(Smoke Domain Warping)]
        _SmokeScale("Smoke Scale", Float) = 1.5
        _SmokeSpeed("Smoke Speed (X, Y)", Vector) = (0.08, 0.05, 0, 0)
        _WarpStrength("Warp Strength", Float) = 1.5
        _SmokeContrast("Smoke Contrast (1-8)", Float) = 3.0
        _SmokeDensity("Smoke Density", Float) = 1.0

        [MainColor] _FogColor("Fog Color (RGB + Alpha)", Color) = (0, 0, 0, 0.95)

        // === UI-specific ===
        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            Name "FlashlightFogPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define PI 3.141592653589793
            #define TWOPI 6.283185307179586

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FlashlightCenter;
                float _Radius;
                float _Softness;

                float _SmokeScale;
                float2 _SmokeSpeed;
                float _WarpStrength;
                float _SmokeContrast;
                float _SmokeDensity;

                half4 _FogColor;
            CBUFFER_END

            // ================================
            // Hash-based noise (2D, без текстур)
            // ================================

            // Hash в диапазоне [0, 1]
            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // 2D value noise с интерполяцией (даёт плавные облака)
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                // Quintic interpolation — более плавная, чем smoothstep
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float a = hash21(i + float2(0.0, 0.0));
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // FBM — fractal brownian motion (4 октавы)
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float totalWeight = 0.0;

                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * valueNoise(p);
                    totalWeight += amplitude;
                    p *= 2.0;
                    amplitude *= 0.5;
                }

                return value / totalWeight;
            }

            // ================================
            // Domain Warping — та самая магия!
            // ================================
            // Взято из вашего референса и адаптировано под 2D
            float smokeDensity(float2 uv, float time)
            {
                // Первый слой шума — для искажения пространства
                float2 q = float2(
                    fbm(uv + float2(0.0, time) * _SmokeSpeed),
                    fbm(uv + float2(1.7, 9.2) + float2(0.0, time) * _SmokeSpeed * 0.8)
                );

                // Второй слой — уже искажённый первым (domain warping)
                float2 r = float2(
                    fbm(uv + _WarpStrength * q + float2(1.7, 9.2) + float2(0.15, 0.05) * time),
                    fbm(uv + _WarpStrength * q + float2(8.3, 2.8) + float2(0.1, 0.08) * time)
                );

                // Финальный шум — ещё раз искажённый
                float f = fbm(uv + _WarpStrength * r);

                // Контрастная кривая (как в вашем референсе через pow + smoothstep)
                f = pow(saturate(f), _SmokeContrast);

                // Финальная коррекция через smoothstep для мягких краёв клубов
                f = lerp(f, f * f, 0.4);

                return f;
            }

            // ================================
            // Vertex / Fragment
            // ================================

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Маска фонарика (как раньше)
                float2 screenPixel = (IN.screenPos.xy / IN.screenPos.w) * _ScreenParams.xy;
                float dist = distance(screenPixel, _FlashlightCenter.xy);
                float flashlightMask = smoothstep(_Radius, _Radius + max(_Softness, 0.5), dist);

                // 2. Дым с domain warping
                float2 smokeUV = IN.uv * _SmokeScale;
                float smoke = smokeDensity(smokeUV, _Time.y);

                // 3. Комбинация: туман + дым
                // Дым модулирует плотность тумана
                float fogDensity = flashlightMask * (1.0 - smoke * _SmokeDensity);

                // 4. Итоговый цвет
                half4 col = _FogColor;
                col.a *= fogDensity;
                col *= IN.color;

                return col;
            }
            ENDHLSL
        }
    }
}