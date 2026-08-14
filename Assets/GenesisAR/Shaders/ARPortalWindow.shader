Shader "GenesisAR/ARPortalWindow"
{
    // ─────────────────────────────────────────────────────────────
    //  ARPortalWindow.shader
    //  Renders an invisible stencil mask so that only geometry
    //  behind the portal quad is visible through the "window".
    //
    //  Usage:
    //   1. Assign this shader to the portal window quad's material.
    //   2. Set the portal world's geometry materials to use
    //      Stencil { Ref 1  Comp Equal } so they only render
    //      where the stencil buffer == 1 (inside the portal).
    //   3. Ensure this material's Queue is lower (e.g. Geometry-1)
    //      so the mask writes before the world renders.
    // ─────────────────────────────────────────────────────────────

    Properties
    {
        // Invisible to the user but exposes stencil ref in Inspector
        _StencilRef ("Stencil Reference", Int) = 1
    }

    SubShader
    {
        // Render before opaque geometry so the stencil is ready
        Tags
        {
            "Queue"           = "Geometry-1"
            "RenderType"      = "Opaque"
            "IgnoreProjector" = "True"
        }

        // ── Pass 1: Write stencil mask, do NOT write colour or depth ──
        Pass
        {
            Name "StencilWrite"

            // Colour & depth are invisible – only the stencil matters
            ColorMask 0
            ZWrite Off

            Stencil
            {
                Ref   [_StencilRef]
                Comp  Always
                Pass  Replace
            }

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f     { float4 pos    : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Fully transparent – stencil write only
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }

        // ── Pass 2: Depth fill so nearer real-world objects occlude ──
        //  Writes depth after the stencil pass so that AR camera
        //  occlusion (e.g. hands in front of portal) works correctly.
        Pass
        {
            Name "DepthFill"

            ColorMask 0
            ZWrite On
            ZTest  LEqual

            Stencil
            {
                Ref  [_StencilRef]
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f     { float4 pos    : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return fixed4(0,0,0,0); }
            ENDCG
        }
    }

    // Fallback keeps the quad invisible on older hardware
    Fallback "Hidden/InternalErrorShader"
}
