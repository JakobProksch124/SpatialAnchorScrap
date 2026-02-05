Shader "Custom/HorizontalFadeImage"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FadeEdgeStart ("Fade Edge Start", Range(0.0, 0.5)) = 0.15
        _Color ("Tint Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _FadeEdgeStart;
                half4 _Color;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample the texture
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Apply tint color
                texColor *= _Color;

                // Calculate edge fade for all 4 edges (left, right, top, bottom)
                // Fade from edges (0 and 1) towards center (0.5)
                float fadeLeft = smoothstep(0.0, _FadeEdgeStart, input.uv.x);
                float fadeRight = smoothstep(1.0, 1.0 - _FadeEdgeStart, input.uv.x);
                float fadeBottom = smoothstep(0.0, _FadeEdgeStart, input.uv.y);
                float fadeTop = smoothstep(1.0, 1.0 - _FadeEdgeStart, input.uv.y);

                // Combine all edge fades
                float edgeFade = fadeLeft * fadeRight * fadeBottom * fadeTop;

                // Apply edge fade to alpha
                texColor.a *= edgeFade;

                return texColor;
            }
            ENDHLSL
        }
    }
}
