Shader "Hidden/FullScreen/RetroDither"
{
    Properties
    {
        _DarkColor("Dark Color", Color) = (0.03, 0.00, 0.00, 1.0)
        _LightColor("Light Color", Color) = (0.90, 0.10, 0.08, 1.0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "RetroDitherPass"

            HLSLPROGRAM
            // Используем стандартный вершинный шейдер для FullScreen Pass из Blit.hlsl
            #pragma vertex Vert 
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // ЯВНО объявляем сэмплер, чтобы исправить ошибку "Cannot resolve symbol"
            SAMPLER(sampler_BlitTexture);

            // Оборачиваем свойства материала в CBUFFER для оптимизации (SRP Batcher)
            CBUFFER_START(UnityPerMaterial)
                half4 _DarkColor;
                half4 _LightColor;
            CBUFFER_END

            // Оптимизированная матрица Байера
            float bayer4(float2 p) 
            {
                uint x = (uint)p.x & 3; 
                uint y = (uint)p.y & 3;
                
                static const float b[16] = {
                    0.0, 8.0, 2.0, 10.0,
                    12.0, 4.0, 14.0, 6.0,
                    3.0, 11.0, 1.0, 9.0,
                    15.0, 7.0, 13.0, 5.0
                };
                
                return b[y * 4 + x] / 16.0;
            }

            float random(float2 st) 
            {
                return frac(sin(dot(st, float2(12.9898, 78.233))) * 43758.5453123);
            }

            // Varyings объявлен внутри Blit.hlsl, поэтому мы используем его
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float2 fragCoord = input.positionCS.xy;

                // Читаем пиксель экрана. _BlitTexture уже объявлена в Blit.hlsl
                half3 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv).rgb;
                
                float lum = dot(col, float3(0.299, 0.587, 0.114));
                lum = pow(lum, 1.3);
                
                float threshold = bayer4(fragCoord) * 0.30 - 0.15;
                lum = saturate(lum + threshold);
                float bands = floor(lum * 4.0) / 3.0;

                half3 color = lerp(_DarkColor.rgb, _LightColor.rgb, bands);
                color += (random(fragCoord * 0.5) - 0.5) * 0.04;
                
                float vig = 1.0 - dot(uv - 0.5, uv - 0.5) * 1.6;
                color *= saturate(vig);

                return half4(saturate(color), 1.0);
            }
            ENDHLSL
        }
    }
}