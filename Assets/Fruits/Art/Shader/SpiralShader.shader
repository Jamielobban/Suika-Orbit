Shader "Custom/BrokenRingsURP"
{
    Properties
    {
        _LineColor   ("Line Color", Color) = (0,0,0,1)
        _BgColor     ("Background Tint", Color) = (0,0,0,0)

        _Center      ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _RadiusScale ("Radius Scale", Float) = 1.0

        _RingCount   ("Ring Count", Range(1,12)) = 5
        _RingStart   ("Ring Start Radius", Range(0,1)) = 0.10
        _RingSpacing ("Ring Spacing", Range(0,1)) = 0.06

        _LineThickness ("Line Thickness", Range(0.0005, 0.05)) = 0.008
        _LineSoft      ("Line Softness",  Range(0.0001, 0.05)) = 0.004

        _DashSegments   ("Dash Segments (per ring)", Range(4,128)) = 28
        _DashMin        ("Dash Min Length", Range(0,1)) = 0.15
        _DashMax        ("Dash Max Length", Range(0,1)) = 0.55

        // Replaces the old _DashJitter
        _RingPhaseJitter ("Ring Phase Jitter", Range(0,1)) = 1.0
        _CellJitter      ("Per-Cell Jitter", Range(0,1)) = 0.35
        _SkipChance      ("Skip Chance", Range(0,1)) = 0.35
        _RingSegJitter   ("Ring Seg Count Jitter", Range(0,1)) = 0.35

        _RotateSpeed  ("Rotate Speed", Float) = 0.15
        _Alpha        ("Alpha Strength", Range(0,1)) = 1.0

        _InnerFade    ("Inner Fade Radius", Range(0,1)) = 0.06
        _OuterFade    ("Outer Fade Radius", Range(0,1)) = 0.60
        _FadeSoft     ("Fade Softness", Range(0.001,0.5)) = 0.10

        _MainTex ("Sprite Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);

                half4 _LineColor;
                half4 _BgColor;

                float4 _Center;
                float _RadiusScale;

                float _RingCount;
                float _RingStart;
                float _RingSpacing;

                float _LineThickness;
                float _LineSoft;

                float _DashSegments;
                float _DashMin;
                float _DashMax;

                float _RingPhaseJitter;
                float _CellJitter;
                float _SkipChance;
                float _RingSegJitter;

                float _RotateSpeed;
                float _Alpha;

                float _InnerFade;
                float _OuterFade;
                float _FadeSoft;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // Simple stable hash
            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float ringBand(float r, float ringR, float halfThickness, float soft)
            {
                float d = abs(r - ringR);
                return 1.0 - smoothstep(halfThickness, halfThickness + soft, d);
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Centered polar coords
                float2 d = (uv - _Center.xy);
                float r = length(d) * _RadiusScale;
                float ang = atan2(d.y, d.x);                 // [-PI, PI]
                float ang01 = (ang + PI) * (1.0 / (2.0*PI)); // [0,1)

                // Rotate over time
                ang01 = frac(ang01 + _Time.y * _RotateSpeed);

                // Radial fade
                float inner = smoothstep(_InnerFade, _InnerFade + _FadeSoft, r);
                float outer = 1.0 - smoothstep(_OuterFade, _OuterFade + _FadeSoft, r);
                float radialMask = saturate(inner * outer);

                // Build dashed rings
                float rings = 0.0;

                int maxRings = 12; // matches property max
                [loop]
                for (int i = 0; i < maxRings; i++)
                {
                    if (i >= (int)_RingCount) break;

                    float fi = (float)i;

                    // Ring radius (+ tiny per-ring jitter for variety)
                    float ringR = _RingStart + fi * _RingSpacing;
                    ringR += (hash21(float2(fi, 5.73)) - 0.5) * 0.002;

                    // Ring thickness mask
                    float band = ringBand(r, ringR, _LineThickness * 0.5, _LineSoft);

                    // --- Per-ring segment count variation (prevents "same side" / uniform look)
                    float segBase = _DashSegments + fi * 2.0;
                    float segVar  = (hash21(float2(fi, 41.2)) - 0.5) * 2.0 * _RingSegJitter * _DashSegments;
                    float segs    = max(4.0, segBase + segVar);

                    // --- Per-ring phase (wrapped!)
                    float ringPhase = (hash21(float2(fi, 19.7)) - 0.5) * _RingPhaseJitter;
                    float t = frac(ang01 + ringPhase);   // IMPORTANT: wrap to [0..1)

                    // --- Cell id & local position within cell
                    float x    = t * segs;
                    float cell = floor(x);
                    float f    = frac(x);

                    // --- Per-cell jitter (shifts dash inside the cell; nicer than angle jitter)
                    float j = (hash21(float2(cell, fi * 7.1)) - 0.5) * _CellJitter;
                    f = frac(f + j);

                    // --- Dash length per cell (+ subtle per-ring cohesion)
                    float ringLenBias = hash21(float2(fi, 88.8));
                    float duty = lerp(_DashMin, _DashMax, hash21(float2(cell + 3.3, fi + 1.7)));
                    duty = lerp(duty, lerp(_DashMin, _DashMax, ringLenBias), 0.25);

                    // --- Skip some dashes (controllable) with per-ring bias
                    float ringSkipBias = lerp(0.15, 0.55, hash21(float2(fi, 12.4)));
                    float skip = saturate(_SkipChance * ringSkipBias);
                    float keep = step(skip, hash21(float2(cell + 13.3, fi + 7.7)));

                    float dash = step(f, duty) * keep;

                    rings = max(rings, band * dash);
                }

                float mask = saturate(rings * radialMask);

                half4 col = lerp(_BgColor, _LineColor, mask);
                col.a = saturate(mask * _Alpha * _LineColor.a);

                // Respect sprite alpha as a mask
                col.a *= sprite.a;

                return col;
            }
            ENDHLSL
        }
    }
}