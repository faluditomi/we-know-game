// Procedural backdrop for the board and the menu: a dark field with a soft neon
// core, a warm complementary glow low in the frame, a faint grid and a vignette. Drawn on a single camera-facing quad by
// NeonBackdrop, so it costs one unlit draw call at any resolution.
Shader "WeKnow/Neon Backdrop"
{
    Properties
    {
        [HDR] _BaseColorTop    ("Top Colour", Color)        = (0.023, 0.043, 0.055, 1)
        [HDR] _BaseColorBottom ("Bottom Colour", Color)     = (0.008, 0.012, 0.016, 1)
        [HDR] _GlowColor       ("Glow Colour", Color)       = (0.055, 0.345, 0.333, 1)
        _GlowCenter            ("Glow Centre (UV)", Vector) = (0.5, 0.56, 0, 0)
        _GlowRadius            ("Glow Radius", Range(0.05, 2)) = 0.85
        _GlowFalloff           ("Glow Falloff", Range(0.5, 8)) = 2.2
        [HDR] _WarmColor       ("Warm Glow Colour", Color)  = (0.300, 0.110, 0.028, 1)
        _WarmCenter            ("Warm Glow Centre (UV)", Vector) = (0.12, 0.08, 0, 0)
        _WarmRadius            ("Warm Glow Radius", Range(0.05, 3)) = 1.15
        _WarmFalloff           ("Warm Glow Falloff", Range(0.5, 8)) = 2.6
        [HDR] _GridColor       ("Grid Colour", Color)       = (0.063, 0.267, 0.263, 1)
        _GridScale             ("Grid Cells Across", Range(2, 64)) = 18
        _GridWidth             ("Grid Line Width", Range(0.001, 0.2)) = 0.022
        _GridFade              ("Grid Fade From Centre", Range(0, 4)) = 1.4
        _Vignette              ("Vignette", Range(0, 2))    = 0.85
        _Aspect                ("Aspect (set by script)", Float) = 1.7777
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Background" }

        Pass
        {
            Name "NeonBackdrop"
            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColorTop;
                float4 _BaseColorBottom;
                float4 _GlowColor;
                float4 _GlowCenter;
                float  _GlowRadius;
                float  _GlowFalloff;
                float4 _WarmColor;
                float4 _WarmCenter;
                float  _WarmRadius;
                float  _WarmFalloff;
                float4 _GridColor;
                float  _GridScale;
                float  _GridWidth;
                float  _GridFade;
                float  _Vignette;
                float  _Aspect;
            CBUFFER_END

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            // Ordered dither, so the wide gradients do not band on 8-bit displays.
            float Dither(float2 positionSS)
            {
                float2 p = floor(fmod(positionSS, 4.0));
                float table[16] = { 0,  8,  2, 10,
                                   12,  4, 14,  6,
                                    3, 11,  1,  9,
                                   15,  7, 13,  5 };
                int index = (int)(p.y * 4.0 + p.x);
                return (table[index] / 16.0 - 0.5) / 255.0;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv;

                // Distance from the glow centre, corrected so the glow stays round on any aspect.
                float2 d = uv - _GlowCenter.xy;
                d.x *= max(_Aspect, 0.0001);
                float radial = saturate(length(d) / max(_GlowRadius, 0.0001));

                float3 base = lerp(_BaseColorBottom.rgb, _BaseColorTop.rgb, saturate(uv.y));
                float glow = pow(saturate(1.0 - radial), max(_GlowFalloff, 0.0001));
                float3 col = base + _GlowColor.rgb * glow;

                // A warm counterpoint low in the frame. Teal's complement, kept dim and
                // off to one side so the backdrop has depth without competing with the
                // tiles, which own the neon end of the palette.
                float2 w = uv - _WarmCenter.xy;
                w.x *= max(_Aspect, 0.0001);
                float warmRadial = saturate(length(w) / max(_WarmRadius, 0.0001));
                float warm = pow(saturate(1.0 - warmRadial), max(_WarmFalloff, 0.0001));
                col += _WarmColor.rgb * warm;

                // Faint grid, strongest near the glow and gone by the edges.
                float2 cell = uv * float2(_GridScale * max(_Aspect, 0.0001), _GridScale);
                float2 line2 = abs(frac(cell) - 0.5);
                float2 fw = fwidth(cell) * 0.5 + 1e-5;
                float2 lines = 1.0 - smoothstep(float2(0,0), fw + _GridWidth, line2);
                float grid = saturate(max(lines.x, lines.y));
                float gridFade = pow(saturate(1.0 - radial), max(_GridFade, 0.0001));
                float warmGridFade = pow(saturate(1.0 - warmRadial), max(_GridFade, 0.0001));
                col += _GridColor.rgb * grid * gridFade;
                col += _WarmColor.rgb * grid * warmGridFade * 0.8;

                // Vignette towards the frame edges.
                float2 c = (uv - 0.5) * 2.0;
                float vig = saturate(1.0 - dot(c, c) * 0.25 * _Vignette);
                col *= vig;

                col += Dither(i.positionCS.xy);
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
