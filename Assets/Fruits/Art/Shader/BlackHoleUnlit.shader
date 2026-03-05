Shader "Custom/BlackHoleUnlit_SubtleBreathing"
{
    Properties
    {
        [MainTexture]_MainTex ("Sprite (RGBA)", 2D) = "white" {}
        [MainColor]_Color ("Tint", Color) = (1,1,1,1)

        _Center ("Center UV", Vector) = (0.5, 0.5, 0, 0)

        // --- Radial look
        _BlackRadius ("Black Radius", Range(0,1)) = 0.15
        _BlackFeather ("Black Feather", Range(0.0001,0.5)) = 0.15

        _OuterRadius ("Outer Radius", Range(0,1)) = 0.60
        _OuterFeather ("Outer Feather", Range(0.0001,0.5)) = 0.30

        _OuterWhite ("Outer White Strength", Range(0,2)) = 1.0
        _InnerBlack ("Inner Black Strength", Range(0,2)) = 1.2

        // --- Subtle swirl
        _SwirlStrength ("Swirl Strength", Range(0,1)) = 0.08
        _SwirlSpeed ("Swirl Speed", Range(-5,5)) = 0.25
        _SwirlEps ("Swirl Eps", Range(0.0001,0.2)) = 0.03

        // --- Breathing
        _BreatheRate ("Breathe Rate", Range(0,5)) = 0.6
        _BreatheAmount ("Breathe Amount", Range(0,0.3)) = 0.06
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _Center;

            float _BlackRadius, _BlackFeather;
            float _OuterRadius, _OuterFeather;
            float _OuterWhite, _InnerBlack;

            float _SwirlStrength, _SwirlSpeed, _SwirlEps;

            float _BreatheRate, _BreatheAmount;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 c  = _Center.xy;

                float2 p = uv - c;
                float r = length(p);

                // --- Very subtle swirl
                float ang = atan2(p.y, p.x);
                float swirl = (_SwirlStrength / max(_SwirlEps, r)) + (_SwirlSpeed * _Time.y);
                float2 pw = float2(cos(ang + swirl), sin(ang + swirl)) * r;
                float2 uvW = c + pw;

                fixed4 tex = tex2D(_MainTex, uvW) * i.color;

                // --- Core black mask
                float core = smoothstep(_BlackRadius + _BlackFeather, _BlackRadius, r);

                // --- Outer white mask
                float outer = smoothstep(_OuterRadius - _OuterFeather, _OuterRadius, r);

                // --- Gentle breathing pulse
                float breathe = sin(_Time.y * _BreatheRate * 6.2831853);
                breathe *= _BreatheAmount;

                // Build brightness field
                float val = 0.0;
                val += outer * _OuterWhite;
                val -= core * _InnerBlack;
                val += breathe;

                val = saturate(val);

                // Use sprite luminance as soft detail modulation
                float spriteLuma = dot(tex.rgb, float3(0.299, 0.587, 0.114));
                float detail = lerp(0.9, 1.1, spriteLuma);

                float3 finalColor = val.xxx * detail;

                return fixed4(finalColor, tex.a);
            }
            ENDHLSL
        }
    }
}
