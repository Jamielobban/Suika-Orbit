Shader "Custom/SpiralArmsURP"
{
    Properties
    {
        _ArmColor    ("Arm Color", Color) = (1,1,1,1)
        _BgColor     ("Background Tint", Color) = (1,1,1,0) // usually transparent

        _Center      ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _RadiusScale ("Radius Scale", Float) = 1.0

        _Arms        ("Arm Count", Range(1,8)) = 3
        _Twist       ("Twist (spiral tightness)", Range(0,20)) = 8.0
        _RotateSpeed ("Rotate Speed", Float) = 0.4

        _ArmWidth    ("Arm Width", Range(0.001, 1)) = 0.18
        _ArmSoft     ("Arm Softness", Range(0.0, 0.5)) = 0.12

        _InnerFade   ("Inner Fade Radius", Range(0,1)) = 0.08
        _OuterFade   ("Outer Fade Radius", Range(0,1)) = 0.95
        _FadeSoft    ("Fade Softness", Range(0.001,0.5)) = 0.10

        _Alpha       ("Alpha Strength", Range(0,1)) = 0.25

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

                half4 _ArmColor;
                half4 _BgColor;

                float4 _Center;
                float _RadiusScale;

                float _Arms;
                float _Twist;
                float _RotateSpeed;

                float _ArmWidth;
                float _ArmSoft;

                float _InnerFade;
                float _OuterFade;
                float _FadeSoft;

                float _Alpha;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // keep sprite alpha as cutout/mask
                float4 spriteSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // --- Centered coords (NO pixelation)
                float2 d = (uv - _Center.xy);
                float r = length(d) * _RadiusScale;
                float ang = atan2(d.y, d.x); // [-pi, pi]

                // --- Spiral phase: arms + twist with radius, plus rotation over time
                float arms = max(_Arms, 1.0);
                float phase = ang * arms + r * _Twist - (_Time.y * _RotateSpeed);

                // --- Arm shape
                float band = cos(phase); // [-1..1]

                // Threshold band near 1 => arm
                float w = saturate(_ArmWidth);
                float s = max(_ArmSoft, 1e-5);
                float armMask = smoothstep(1.0 - w - s, 1.0 - w, band);

                // --- Radial fade: vanish near center and near edge
                float inner = smoothstep(_InnerFade, _InnerFade + _FadeSoft, r);
                float outer = 1.0 - smoothstep(_OuterFade, _OuterFade + _FadeSoft, r);
                float radialMask = saturate(inner * outer);

                float mask = armMask * radialMask;

                half4 col = lerp(_BgColor, _ArmColor, mask);

                // alpha from mask + strength + arm alpha, then multiply by sprite alpha
                col.a = saturate(mask * _Alpha * _ArmColor.a);
                col.a *= spriteSample.a;

                return col;
            }
            ENDHLSL
        }
    }
}
