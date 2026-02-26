Shader "Custom/NebulaScrollURP"
{
    Properties
    {
        _MainTex ("Nebula Texture", 2D) = "white" {}
        _Color   ("Tint", Color) = (1,1,1,1)

        _Scroll ("Scroll XY", Vector) = (0.01, 0.0, 0, 0)
        _RotationSpeed ("Rotation Speed", Float) = 0.0

        _Intensity ("Brightness", Range(0,5)) = 1.0
        _Alpha ("Overall Alpha", Range(0,1)) = 0.12

        // subtle breakup (optional but nice)
        _NoiseStrength ("Noise Breakup", Range(0,1)) = 0.15
        _NoiseScale ("Noise Scale", Range(0.1,10)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

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

                float4 _Color;
                float4 _Scroll;
                float  _RotationSpeed;
                float  _Intensity;
                float  _Alpha;

                float  _NoiseStrength;
                float  _NoiseScale;
            CBUFFER_END

            // cheap hash noise
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

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

                // ----- slow scroll -----
                uv += _Scroll.xy * _Time.y;

                // ----- optional gentle rotation -----
                if (abs(_RotationSpeed) > 0.0001)
                {
                    float angle = _Time.y * _RotationSpeed;
                    float s = sin(angle);
                    float c = cos(angle);

                    float2 centered = uv - 0.5;
                    centered = float2(
                        centered.x * c - centered.y * s,
                        centered.x * s + centered.y * c
                    );
                    uv = centered + 0.5;
                }

                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                // ----- subtle noise breakup (VERY cheap) -----
                float noise = hash21(floor(uv * _NoiseScale));
                float breakup = lerp(1.0, noise, _NoiseStrength);

                half3 rgb = tex.rgb * _Color.rgb * _Intensity * breakup;
                half  a   = tex.a * _Color.a * _Alpha;

                return half4(rgb, a);
            }
            ENDHLSL
        }
    }
}