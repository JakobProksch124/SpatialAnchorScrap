// Living-blob head shader for the transition cue (v2, "dock-eyes" design).
// Opaque three-stop gradient sphere (pink -> violet -> blue) with a white
// fresnel rim, a bright highlight traveling around the rim, and procedural
// vertex wobble. Driven per-state by CueHeadVisuals.
Shader "EntryCue/CueOrb"
{
    Properties
    {
        _HiColor ("Gradient Top", Color) = (0.94, 0.72, 1.0, 1)
        _MidColor ("Gradient Mid", Color) = (0.545, 0.36, 0.965, 1)
        _LoColor ("Gradient Low", Color) = (0.306, 0.66, 1.0, 1)
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.0
        _RimGlow ("Rim Glow", Range(0, 3)) = 1.1
        _TravelSpeed ("Rim Travel Speed", Range(0, 8)) = 1.26
        _Glow ("Glow", Range(0, 4)) = 1.0
        _NoiseAmp ("Wobble Amplitude", Range(0, 0.25)) = 0.03
        _NoiseFreq ("Wobble Frequency", Range(0, 30)) = 6
        _NoiseSpeed ("Wobble Speed", Range(0, 12)) = 2.1
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Back
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _HiColor, _MidColor, _LoColor, _RimColor;
            float _RimPower, _RimGlow, _TravelSpeed, _Glow;
            float _NoiseAmp, _NoiseFreq, _NoiseSpeed;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 objPos : TEXCOORD2;
            };

            float wobble(float3 p, float t)
            {
                return sin(p.x * _NoiseFreq + t)
                     * sin(p.y * _NoiseFreq * 1.31 + t * 1.7)
                     * sin(p.z * _NoiseFreq * 0.73 + t * 1.3);
            }

            v2f vert(appdata v)
            {
                v2f o;
                float t = _Time.y * _NoiseSpeed;
                v.vertex.xyz += v.normal * wobble(v.vertex.xyz, t) * _NoiseAmp;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.objPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // three-stop gradient along a top-left-ish axis in object space
                float f = saturate(dot(normalize(i.objPos), normalize(float3(-0.45, 0.75, 0.3))) * 0.5 + 0.5);
                fixed3 grad = f > 0.5
                    ? lerp(_MidColor.rgb, _HiColor.rgb, (f - 0.5) * 2.0)
                    : lerp(_LoColor.rgb, _MidColor.rgb, f * 2.0);

                float3 n = normalize(i.worldNormal);
                float3 v = normalize(i.viewDir);

                // soft fake lighting from top-left-front for a glossy 3D read
                float3 lightDir = normalize(float3(-0.5, 0.8, 0.6));
                float diff = saturate(dot(n, lightDir)) * 0.35 + 0.75;
                float spec = pow(saturate(dot(n, normalize(lightDir + v))), 42.0);

                float ndv = saturate(dot(v, n));
                float rim = pow(1.0 - ndv, _RimPower);

                // traveling bright point racing the rim
                float ang = atan2(i.objPos.y, i.objPos.x);
                float travel = 0.5 + 0.5 * sin(ang - _Time.y * _TravelSpeed * 3.0);
                travel = pow(travel, 6.0);

                fixed3 col = grad * diff * _Glow
                           + fixed3(1, 1, 1) * spec * 0.55
                           + _RimColor.rgb * rim * _RimGlow
                           + _RimColor.rgb * rim * travel * _RimGlow * 1.4;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    Fallback "Diffuse"
}
