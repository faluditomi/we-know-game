// The face a tile shows once it is revealed: a lit panel rather than a painted
// plate, so the logo on top reads as something displayed on a screen. The panel
// stays bright on purpose — the tile logos include dark artwork, and a dark
// hologram would swallow it.
//
// TileController tints _Hue per tile from the tile's own colour, through a
// MaterialPropertyBlock, so every design keeps its identity without the frame
// having to give the pair away.
//
// The same shader lights the face-down side of a matched pair while it is
// presented. There the two tiles join into one wide panel, so _EdgeMask turns
// off the inner edge glow on the seam side (x = left, y = right, z = bottom,
// w = top; 1 = glow, 0 = none) and the pair reads as one continuous screen.
Shader "WeKnow/Hologram Screen"
{
    Properties
    {
        _BaseColor      ("Panel Colour", Color)            = (0.90, 0.94, 0.95, 1)
        _Hue            ("Tile Hue", Color)                = (0.25, 0.95, 0.90, 1)
        _HueStrength    ("Hue Strength", Range(0, 1))      = 0.30
        [HDR] _GlowColor("Emission", Color)                = (0.18, 0.28, 0.30, 1)
        _ScanCount      ("Scanline Count", Range(0, 400))  = 150
        _ScanStrength   ("Scanline Strength", Range(0, 1)) = 0.20
        _ScanSpeed      ("Scanline Drift", Range(-4, 4))   = 0.18
        _SweepStrength  ("Sweep Strength", Range(0, 1))    = 0.26
        _SweepSpeed     ("Sweep Speed", Range(0, 4))       = 0.90
        _Sweep2Strength ("Second Sweep Strength", Range(0, 1)) = 0.16
        _Sweep2Speed    ("Second Sweep Speed", Range(-4, 4))   = -1.55
        _GlitchStrength ("Glitch Band Strength", Range(0, 1))  = 0.10
        _EdgeGlow       ("Edge Glow", Range(0, 2))         = 0.85
        _EdgeWidth      ("Edge Width", Range(0.001, 0.5))  = 0.07
        _Flicker        ("Flicker", Range(0, 0.2))         = 0.03
        _EdgeMask       ("Edge Mask (L, R, B, T)", Vector) = (1, 1, 1, 1)
        _Brightness     ("Brightness", Range(0, 2))        = 0.85
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "HologramScreen"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Hue;
                float  _HueStrength;
                float4 _GlowColor;
                float  _ScanCount;
                float  _ScanStrength;
                float  _ScanSpeed;
                float  _SweepStrength;
                float  _SweepSpeed;
                float  _Sweep2Strength;
                float  _Sweep2Speed;
                float  _GlitchStrength;
                float  _EdgeGlow;
                float  _EdgeWidth;
                float  _Flicker;
                float4 _EdgeMask;
                float  _Brightness;
            CBUFFER_END

            // Victory moment surge, 0..1, set globally by GameManager. Kept out of the material
            // constant buffer so every tile reacts without per-material writes.
            float _VictorySurge;

            Varyings vert (Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.positionOS = v.positionOS.xyz;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // The plate is a flat panel, so its own object-space XY is a more
                // reliable surface coordinate than whatever UVs the model carries.
                float2 panel = saturate(i.positionOS.xy / 0.41 * 0.5 + 0.5);

                float3 hue = _Hue.rgb;
                float3 col = lerp(_BaseColor.rgb, _BaseColor.rgb * hue, _HueStrength);

                // Fine horizontal scanlines, drifting slowly.
                float scan = sin((panel.y * _ScanCount) + _Time.y * _ScanSpeed * 6.2831);
                col *= 1.0 - _ScanStrength * (0.5 + 0.5 * scan);

                // A soft bright band sweeping up the panel, like a refresh pass.
                float sweep = frac(panel.y - _Time.y * _SweepSpeed);
                col += hue * _SweepStrength * pow(saturate(1.0 - abs(sweep - 0.5) * 3.0), 3.0);

                // A thinner, faster band running the other way, so the two cross now and then.
                float sweep2 = frac(panel.y - _Time.y * _Sweep2Speed);
                col += hue * _Sweep2Strength * pow(saturate(1.0 - abs(sweep2 - 0.5) * 8.0), 2.0);

                // Short bright glitch bands that jump to a new height several times a second.
                float slot = floor(_Time.y * 5.0 * (1.0 + _VictorySurge));
                float bandY = frac(sin(slot * 12.9898) * 43758.5453);
                float bandOn = step(0.55, frac(sin(slot * 78.233) * 12543.123));
                float band = 1.0 - smoothstep(0.0, 0.025, abs(panel.y - bandY));
                col += hue * (_GlitchStrength + 0.35 * _VictorySurge) * band * bandOn;

                // Inner edge glow, so the panel reads as inset glass.
                // Masked-off edges are pushed out of reach so they never glow.
                float4 d = float4(panel.x, 1.0 - panel.x, panel.y, 1.0 - panel.y)
                         + (1.0 - saturate(_EdgeMask)) * 2.0;
                float edge = 1.0 - smoothstep(0.0, _EdgeWidth, min(min(d.x, d.y), min(d.z, d.w)));
                col += hue * _EdgeGlow * edge * 0.5;

                // Barely-there flicker: enough to feel alive, not enough to distract.
                col *= 1.0 + _Flicker * sin(_Time.y * 37.0) * 0.5;

                // A screen is self-lit: its brightness must not change as the tile turns
                // relative to the scene light (a diffuse term made one face of a spinning
                // tile visibly darker than the other). _Brightness matches the old lit
                // value with the panel facing the camera.
                float3 lit = col * _Brightness * (1.0 + 0.6 * _VictorySurge);

                lit += _GlowColor.rgb * lerp(float3(1.0, 1.0, 1.0), hue, _HueStrength);
                return half4(lit, 1.0);
            }
            ENDHLSL
        }

        // Keeps the panel in the depth prepass / shadow logic URP expects.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Same constant buffer as the forward pass, so the SRP Batcher stays happy.
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor; float4 _Hue; float _HueStrength; float4 _GlowColor;
                float _ScanCount; float _ScanStrength; float _ScanSpeed;
                float _SweepStrength; float _SweepSpeed;
                float _Sweep2Strength; float _Sweep2Speed; float _GlitchStrength;
                float _EdgeGlow; float _EdgeWidth; float _Flicker; float4 _EdgeMask; float _Brightness;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; };
            V DepthVert (A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half4 DepthFrag (V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
