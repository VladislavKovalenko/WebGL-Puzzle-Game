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
                half4 color       : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4 color        : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FlashlightCenter;
                float  _Radius;
                float  _Softness;

                half4  _LightColor;
                half   _LightIntensity;
                half   _LightFalloff;

                half   _EdgeNoiseScale;
                half   _EdgeNoiseStrength;
                half   _EdgeNoiseSpeed;

                half   _SmokeScale;
                half2  _SmokeSpeed;
                half   _WarpStrength;
                half   _SmokeContrast;
                half   _SmokeDensity;

                half4  _FogColor;
                float4 _MainTex_ST;
            CBUFFER_END

            half hash21(half2 p)
            {
                half3 p3 = frac(half3(p.xyx) * half3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33h);
                return frac((p3.x + p3.y) * p3.z);
            }

            half valueNoise(half2 p)
            {
                half2 i = floor(p);
                half2 f = frac(p);
                half2 u = f * f * f * (f * (f * 6.0h - 15.0h) + 10.0h);

                half a = hash21(i + half2(0.0h, 0.0h));
                half b = hash21(i + half2(1.0h, 0.0h));
                half c = hash21(i + half2(0.0h, 1.0h));
                half d = hash21(i + half2(1.0h, 1.0h));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half fbm(half2 p)
            {
                half value = 0.0h;
                half amplitude = 0.5h;
                half totalWeight = 0.0h;

                UNITY_UNROLL
                for (int i = 0; i < 3; i++)
                {
                    value       += amplitude * valueNoise(p);
                    totalWeight += amplitude;
                    p           *= 2.0h;
                    amplitude   *= 0.5h;
                }

                return value / totalWeight;
            }

            half edgeNoise(float2 screenPixel, float2 center, half time)
            {
                float2 dir = screenPixel - center;
                half angle = atan2(dir.y, dir.x);
                half angleFrac = angle / 6.283185h + 0.5h;

                half2 noiseCoord = half2(
                    angleFrac * _EdgeNoiseScale,
                    time * _EdgeNoiseSpeed
                );

                half n1 = fbm(noiseCoord);
                half n2 = fbm(noiseCoord * 1.7h + half2(5.2h, 1.3h));

                return (n1 * 0.6h + n2 * 0.4h) * 2.0h - 1.0h;
            }

            half smokeDensityFunc(half2 uv, half time)
            {
                half2 q = half2(
                    fbm(uv + half2(0.0h, time) * _SmokeSpeed),
                    fbm(uv + half2(1.7h, 9.2h) + half2(0.0h, time) * _SmokeSpeed * 0.8h)
                );

                half2 r = half2(
                    fbm(uv + _WarpStrength * q + half2(1.7h, 9.2h) + half2(0.15h, 0.05h) * time),
                    fbm(uv + _WarpStrength * q + half2(8.3h, 2.8h) + half2(0.1h, 0.08h) * time)
                );

                half f = fbm(uv + _WarpStrength * r);
                f = pow(saturate(f), _SmokeContrast);
                f = lerp(f, f * f, 0.4h);

                return f;
            }

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

                half edgeOffset = edgeNoise(screenPixel, center, (half)_Time.y) * _EdgeNoiseStrength;
                float dynamicRadius = _Radius + (float)edgeOffset;

                float dist = distance(screenPixel, center);
                half flashlightMask = (half)smoothstep(
                    dynamicRadius,
                    dynamicRadius + max(_Softness, 0.5),
                    dist
                );

                half lightMask = 1.0h - saturate(pow((half)(dist / max(_Radius, 1.0)), _LightFalloff));

                half2 smokeUV = (half2)IN.uv * _SmokeScale;
                half smoke = smokeDensityFunc(smokeUV, (half)_Time.y);

                half fogDensity = flashlightMask * (1.0h - smoke * _SmokeDensity);

                half4 col = _FogColor;
                col.a *= fogDensity;

                half3 lightTint = _LightColor.rgb * _LightIntensity * lightMask * (1.0h - fogDensity);
                col.rgb += lightTint;

                col *= IN.color;

                return col;
            }
            ENDHLSL
        }
    }
}
