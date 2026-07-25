// Crisp SDF rounded-rectangle for world-space UI (panels, status box, buttons,
// cards). Fixes the sprite/9-slice artifacts on Quest: perfect corners at any
// size, a clean inner border, and a vertical fill gradient. Drawn on a RawImage
// quad (UV 0..1); the size in canvas px is fed by RoundedRectUI.
Shader "EntryCue/RoundedRect"
{
    Properties
    {
        _ColorTop ("Fill Top", Color) = (0.1, 0.1, 0.12, 0.92)
        _ColorBottom ("Fill Bottom", Color) = (0.06, 0.06, 0.08, 0.95)
        _BorderColor ("Border", Color) = (1, 1, 1, 0)
        _Size ("Size (px)", Vector) = (200, 80, 0, 0)
        _Radius ("Corner Radius (px)", Float) = 16
        _Border ("Border Width (px)", Float) = 0
        _Softness ("Edge Softness (px)", Float) = 1.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        // RGB blends translucent as usual; ALPHA channel accumulates toward the destination
        // (One, OneMinusSrcAlpha) so a translucent panel keeps dest alpha = 1 over opaque VR
        // content (no passthrough bleed) but still reveals passthrough over an alpha-0 buffer (AR).
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            fixed4 _ColorTop, _ColorBottom, _BorderColor;
            float4 _Size;
            float _Radius, _Border, _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 halfSize = _Size.xy * 0.5;
                float r = min(_Radius, min(halfSize.x, halfSize.y)); // never exceed a stadium
                float2 p = (i.uv - 0.5) * _Size.xy;
                float2 q = abs(p) - (halfSize - r);
                float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r; // signed dist, <0 inside

                float aa = max(_Softness, 0.001);
                float coverage = 1.0 - smoothstep(0.0, aa, d);           // outer AA edge
                fixed4 fill = lerp(_ColorBottom, _ColorTop, saturate(i.uv.y));

                fixed4 col = fill;
                if (_BorderColor.a > 0.001 && _Border > 0.001)
                {
                    float border = smoothstep(-_Border - aa, -_Border + aa, d); // 1 near edge, 0 interior
                    col = lerp(fill, _BorderColor, border * _BorderColor.a);
                }

                col.a *= coverage * i.color.a;
                return col;
            }
            ENDCG
        }
    }
}
