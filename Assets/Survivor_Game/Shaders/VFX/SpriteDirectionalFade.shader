Shader "Survivor/VFX/Sprite Directional Fade"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _FadeProgress("Fade Progress", Range(0, 1)) = 0
        _FadeDirection("Fade Direction", Vector) = (1, 0, 0, 0)
        _FadeSoftness("Fade Width", Range(0.001, 0.5)) = 0.12
        _FadePower("Fade Curve", Range(0.1, 8)) = 1
        [Toggle] _Reverse("Reverse Direction", Float) = 0
        [Toggle] _WiperFade("Pivot Wiper Fade", Float) = 0
        _WiperPivotOffset("Wiper Pivot Offset (Local)", Vector) = (0, 0, 0, 0)
        _WiperStartAngle("Wiper Start Angle", Range(-180, 180)) = 0
        [Toggle] _WiperReverseRotation("Reverse Wiper Rotation", Float) = 0
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip("Flip", Vector) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "CanUseSpriteAtlas"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "SpriteDirectionalFade"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float2 positionOS : TEXCOORD1;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _FadeProgress;
                float4 _FadeDirection;
                float _FadeSoftness;
                float _FadePower;
                float _Reverse;
                float _WiperFade;
                float4 _WiperPivotOffset;
                float _WiperStartAngle;
                float _WiperReverseRotation;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                output.positionOS = input.positionOS.xy;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) *
                    input.color;
                if (sprite.a <= 0.0001)
                {
                    discard;
                }

                float2 direction = _FadeDirection.xy;
                direction = dot(direction, direction) > 0.0001
                    ? normalize(direction)
                    : float2(1.0, 0.0);
                direction = lerp(direction, -direction, step(0.5, _Reverse));

                float halfProjectionRange = max(
                    (abs(direction.x) + abs(direction.y)) * 0.5,
                    0.0001);
                float directionalPosition = saturate(
                    (dot(input.uv - 0.5, direction) + halfProjectionRange) /
                    (halfProjectionRange * 2.0));

                float2 wiperPositionOS = input.positionOS - _WiperPivotOffset.xy;
                float pixelAngle = atan2(wiperPositionOS.y, wiperPositionOS.x);
                float startAngle = radians(_WiperStartAngle);
                float counterClockwiseDelta = frac(
                    (pixelAngle - startAngle) / (2.0 * PI));
                float clockwiseDelta = frac(
                    (startAngle - pixelAngle) / (2.0 * PI));
                float wiperPosition = lerp(
                    counterClockwiseDelta,
                    clockwiseDelta,
                    step(0.5, _WiperReverseRotation));
                float fadePosition = lerp(
                    directionalPosition,
                    wiperPosition,
                    step(0.5, _WiperFade));

                float softness = max(_FadeSoftness, 0.001);
                float fadeFront = lerp(
                    -softness,
                    1.0 + softness,
                    saturate(_FadeProgress));
                float visible = smoothstep(
                    fadeFront - softness,
                    fadeFront + softness,
                    fadePosition);
                visible = pow(saturate(visible), max(_FadePower, 0.1));

                half alpha = sprite.a * visible;
                clip(alpha - 0.001);
                return half4(sprite.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
