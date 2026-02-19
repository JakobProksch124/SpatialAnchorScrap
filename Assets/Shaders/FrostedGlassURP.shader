
Shader "Custom/FrostedGlassURP"
{
    Properties
    {
        _Tint("Tint", Color) = (1,1,1,1)
        _Alpha("Alpha", Range(0,1)) = 0.4
        _Distortion("Distortion", Range(0,2)) = 0.6
        _BlurRadius("Blur Radius", Range(0,3)) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;

            float4 _Tint;
            float _Alpha;
            float _Distortion;
            float _BlurRadius;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 screenPos  : TEXCOORD1;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.positionCS);
                return o;
            }

            float4 SampleOpaque(float2 suv)
            {
                #if defined(UNITY_SINGLE_PASS_STEREO)
                    suv = UnityStereoTransformScreenSpaceTex(suv);
                #endif
                return SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, suv);
            }

            float4 frag (Varyings i) : SV_Target
            {
                float2 suv = i.screenPos.xy / i.screenPos.w;

                // Distortion via normal map (klein halten!)
                float3 n = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv));
                float2 distortion = n.xy * (_Distortion * 0.01);

                float2 texel = _CameraOpaqueTexture_TexelSize.xy * _BlurRadius;

                float4 c0 = SampleOpaque(suv + distortion);
                float4 c1 = SampleOpaque(suv + distortion + float2( texel.x, 0));
                float4 c2 = SampleOpaque(suv + distortion + float2(-texel.x, 0));
                float4 c3 = SampleOpaque(suv + distortion + float2(0,  texel.y));
                float4 c4 = SampleOpaque(suv + distortion + float2(0, -texel.y));

                float4 blurred = (c0 + c1 + c2 + c3 + c4) * 0.2;

                float3 rgb = blurred.rgb * _Tint.rgb;
                return float4(rgb, _Alpha);
            }
            ENDHLSL
        }
    }
}
