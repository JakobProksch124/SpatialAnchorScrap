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
        // Clear BOTH rgb and alpha of the destination to 0 (Blend Zero Zero on all channels).
        // Alpha 0 reveals the underlay passthrough; rgb 0 avoids the leftover grey/white RGB
        // adding a haze under Meta's premultiplied underlay compositing.
        Blend Zero Zero

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
