Shader "EntryCue/PassthroughHole"
{
    // Punches a "window to reality": forces the framebuffer alpha to 0 in this region so the
    // reconstructed passthrough UNDERLAY shows through — even over opaque VR content. Writes
    // ONLY the alpha channel (ColorMask A) and forces it to 0 (Blend Zero Zero). Renders late
    // (Transparent+20) so it clears alpha after the panels have written theirs.
    Properties { }
    SubShader
    {
        Tags { "Queue"="Transparent+20" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        ColorMask A
        Blend Zero Zero   // dst.a = src.a*0 + dst.a*0 = 0

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return fixed4(0, 0, 0, 0); }
            ENDCG
        }
    }
}
