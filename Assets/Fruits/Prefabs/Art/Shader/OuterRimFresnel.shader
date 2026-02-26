Shader "Custom/OuterRimFresnelURP"
{
    Properties
    {
        _RimColor     ("Rim Color", Color) = (1,0.55,0.15,1)

        _Center       ("Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _RadiusScale  ("Radius Scale", Float) = 1.0

        _RimRadius    ("Rim Radius", Range(0,2)) = 0.95
        _RimWidth     ("Rim Width", Range(0.0005,0.75)) = 0.02
        _RimSoft      ("Rim Softness", Range(0.0005,0.2)) = 0.05

        _FresnelPow   ("Fresnel Power", Range(0.1,8)) = 2.0
        _Intensity    ("Intensity", Range(0,10)) = 2.0
        _Alpha        ("Alpha", Range(0,1)) = 0.7

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

                half4 _RimColor;

                float4 _Center;
                float  _RadiusScale;

                float  _RimRadius;
                float  _RimWidth;
                float  _RimSoft;

                float  _FresnelPow;
                float  _Intensity;
                float  _Alpha;
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
                float4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // Polar radius from center
                float2 d = (uv - _Center.xy);
                float r = length(d) * _RadiusScale;

                // 1) Ring band centered on _RimRadius
                float halfW = _RimWidth * 0.5;
                float ring = 1.0 - smoothstep(halfW, halfW + _RimSoft, abs(r - _RimRadius));

                // 2) "Fresnel-like" edge boost (brighter nearer the rim)
                //    This shapes glow so it feels like a rim highlight.
                float edge = saturate((r - (_RimRadius - _RimWidth * 2.0)) / max(_RimWidth * 2.0, 1e-5));
                float fres = pow(edge, _FresnelPow);

                float mask = saturate(ring * fres);

                half4 col;
                col.rgb = _RimColor.rgb * (mask * _Intensity);
                col.a   = saturate(mask * _Alpha * _RimColor.a);

                // Respect sprite alpha (so it only draws inside your circle sprite)
                col.a *= sprite.a;

                return col;
            }
            ENDHLSL
        }
    }
}