Shader "Custom/UI/FlashlightFog"
{
    Properties
    {
        [MainTexture] _MainTex("Base Texture (Not Used)", 2D) = "white" {}

        [Header(Flashlight)]
        _FlashlightCenter("Center (Screen Pixels)", Vector) = (0, 0, 0, 0)
        _Radius("Radius (Pixels)", Float) = 150
        _Softness("Edge Softness (Pixels)", Float) = 40

        [Header(Flashlight Light Color)]
        [HDR] _LightColor("Light Color (HDR)", Color) = (1, 0.95, 0.8, 1)
        _LightIntensity("Light Intensity", Range(0, 5)) = 1.0
        _LightFalloff("Light Falloff (Center to Edge)", Range(0.1, 5)) = 1.5

        [Header(Edge Animation)]
        _EdgeNoiseScale("Edge Noise Scale", Float) = 3.0
        _EdgeNoiseStrength("Edge Noise Strength (Pixels)", Float) = 30
        _EdgeNoiseSpeed("Edge Noise Speed", Float) = 1.5

        [Header(Smoke Domain Warping)]
        _SmokeScale("Smoke Scale", Float) = 1.5
        _SmokeSpeed("Smoke Speed (X, Y)", Vector) = (0.08, 0.05, 0, 0)
        _WarpStrength("Warp Strength", Float) = 1.5
        _SmokeContrast("Smoke Contrast (1-8)", Float) = 3.0
        _SmokeDensity("Smoke Density", Float) = 1.0

        [MainColor] _FogColor("Fog Color (RGB + Alpha)", Color) = (0, 0, 0, 0.95)

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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FlashlightCenter;
                float  _Radius;
                float  _Softness;

                half4  _LightColor;
                float  _LightIntensity;
                float  _LightFalloff;

                float  _EdgeNoiseScale;
                float  _EdgeNoiseStrength;
                float  _EdgeNoiseSpeed;

                float  _SmokeScale;
                float2 _SmokeSpeed;
                float  _WarpStrength;
                float  _SmokeContrast;
                float  _SmokeDensity;

                half4  _FogColor;

                float4 _MainTex_ST;
            CBUFFER_END

            // ================================
            // Noise
            // ================================

            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float a = hash21(i + float2(0.0, 0.0));
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float totalWeight = 0.0;

                for (int i = 0; i < 4; i++)
                {
                    value       += amplitude * valueNoise(p);
                    totalWeight += amplitude;
                    p           *= 2.0;
                    amplitude   *= 0.5;
                }

                return value / totalWeight;
            }

            float edgeNoise(float2 screenPixel, float2 center, float time)
            {
                float2 dir = screenPixel - center;
                float angle = atan2(dir.y, dir.x);
                float angleFrac = angle / 6.283185 + 0.5;

                float2 noiseCoord = float2(
                    angleFrac * _EdgeNoiseScale,
                    time * _EdgeNoiseSpeed
                );

                float n1 = fbm(noiseCoord);
                float n2 = fbm(noiseCoord * 1.7 + float2(5.2, 1.3));

                return (n1 * 0.6 + n2 * 0.4) * 2.0 - 1.0;
            }

            float smokeDensityFunc(float2 uv, float time)
            {
                float2 q = float2(
                    fbm(uv + float2(0.0, time) * _SmokeSpeed),
                    fbm(uv + float2(1.7, 9.2) + float2(0.0, time) * _SmokeSpeed * 0.8)
                );

                float2 r = float2(
                    fbm(uv + _WarpStrength * q + float2(1.7, 9.2) + float2(0.15, 0.05) * time),
                    fbm(uv + _WarpStrength * q + float2(8.3, 2.8) + float2(0.1, 0.08) * time)
                );

                float f = fbm(uv + _WarpStrength * r);
                f = pow(saturate(f), _SmokeContrast);
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
                OUT.uv          = IN.uv;
                OUT.color       = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenPixel = IN.positionHCS.xy;
                float2 center = _FlashlightCenter.xy;

                // -------------------------------------------------------
                // 1. Динамические края
                // -------------------------------------------------------
                float edgeOffset = edgeNoise(screenPixel, center, _Time.y) * _EdgeNoiseStrength;
                float dynamicRadius = _Radius + edgeOffset;

                // -------------------------------------------------------
                // 2. Расстояние и маска тумана
                // -------------------------------------------------------
                float dist = distance(screenPixel, center);
                float flashlightMask = smoothstep(
                    dynamicRadius,
                    dynamicRadius + max(_Softness, 0.5),
                    dist
                );

                // -------------------------------------------------------
                // 3. Маска света (для подкраски области фонарика)
                //    1.0 в центре → 0.0 на краю радиуса
                // -------------------------------------------------------
                float lightMask = 1.0 - saturate(pow(dist / max(_Radius, 1.0), _LightFalloff));

                // -------------------------------------------------------
                // 4. Дым
                // -------------------------------------------------------
                float2 smokeUV = IN.uv * _SmokeScale;
                float smoke = smokeDensityFunc(smokeUV, _Time.y);

                // -------------------------------------------------------
                // 5. Плотность тумана
                // -------------------------------------------------------
                float fogDensity = flashlightMask * (1.0 - smoke * _SmokeDensity);

                // -------------------------------------------------------
                // 6. Цвет тумана
                // -------------------------------------------------------
                half4 fogCol = _FogColor;
                fogCol.a *= fogDensity;

                // -------------------------------------------------------
                // 7. Подкраска области фонарика цветом
                //    Свет добавляется поверх сцены (аддитивно внутри круга)
                //    lightMask = 1 в центре, 0 снаружи
                //    Когда fogDensity высокий — свет не виден (туман закрывает)
                //    Когда fogDensity низкий (внутри круга) — свет виден
                // -------------------------------------------------------
                half3 lightContribution = _LightColor.rgb * _LightIntensity * lightMask;

                // Смешиваем: туман + подсветка в прозрачной области
                // Внутри круга fogCol.a ≈ 0, поэтому свет виден
                // Снаружи fogCol.a ≈ 1, туман перекрывает свет
                half3 finalRGB = lerp(lightContribution, fogCol.rgb, fogDensity);
                half  finalA   = max(fogDensity, lightMask * _LightIntensity * _LightColor.a);

                half4 col = half4(finalRGB, finalA);
                col *= IN.color;

                return col;
            }
            ENDHLSL
        }
    }
}