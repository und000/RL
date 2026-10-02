Shader "Survivor/VFX/Sprite Flame Flow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [NoScaleOffset] _NoiseTex("Flame Flow Noise", 2D) = "gray" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        [HDR] _CoreColor("Core Color", Color) = (4, 1.4, 0.15, 1)
        [HDR] _MidColor("Middle Color", Color) = (2.2, 0.28, 0.015, 1)
        [HDR] _TipColor("Tip Color", Color) = (0.55, 0.015, 0.002, 0.45)
        _Intensity("Color Intensity", Range(0, 5)) = 1
        _Opacity("Opacity", Range(0, 1)) = 1

        _NoiseTilingA("Primary Noise Tiling", Vector) = (1.35, 1.15, 0, 0)
        _NoiseScrollA("Primary Noise Scroll", Vector) = (0.04, -0.45, 0, 0)
        _NoiseTilingB("Secondary Noise Tiling", Vector) = (2.7, 2.1, 0, 0)
        _NoiseScrollB("Secondary Noise Scroll", Vector) = (-0.08, -0.8, 0, 0)
        _NoiseBlend("Secondary Noise Blend", Range(0, 1)) = 0.5
        _DistortionStrength("Noise Distortion", Range(0, 0.5)) = 0.08

        _BaseWidth("Base Width", Range(0.05, 1.5)) = 0.95
        _TipWidth("Tip Width", Range(0, 1.5)) = 0.12
        _TaperPower("Taper Power", Range(0.1, 8)) = 1.35
        _NoiseThreshold("Noise Threshold", Range(0, 1)) = 0.42
        _BaseSolidness("Base Solidness", Range(0, 1)) = 0.34
        _TipBreakup("Tip Breakup", Range(0, 1)) = 0.22
        _EdgeSoftness("Edge Softness", Range(0.001, 0.3)) = 0.075
        _BottomFade("Bottom Fade", Range(0.001, 0.5)) = 0.025
        _TopFade("Top Fade", Range(0.001, 0.5)) = 0.12

        _FlickerSpeed("Flicker Speed", Range(0, 30)) = 7
        _FlickerStrength("Flicker Strength", Range(0, 0.25)) = 0.035
        _FlickerScale("Flicker Height Scale", Range(0, 30)) = 8

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 1
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
        Blend [_SrcBlend] [_DstBlend]
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "SpriteFlameFlow"
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
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                half4 _MidColor;
                half4 _TipColor;
                float _Intensity;
                float _Opacity;
                float4 _NoiseTilingA;
                float4 _NoiseScrollA;
                float4 _NoiseTilingB;
                float4 _NoiseScrollB;
                float _NoiseBlend;
                float _DistortionStrength;
                float _BaseWidth;
                float _TipWidth;
                float _TaperPower;
                float _NoiseThreshold;
                float _BaseSolidness;
                float _TipBreakup;
                float _EdgeSoftness;
                float _BottomFade;
                float _TopFade;
                float _FlickerSpeed;
                float _FlickerStrength;
                float _FlickerScale;
                float _SrcBlend;
                float _DstBlend;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            Varyings Vert(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (sprite.a <= 0.0001)
                {
                    discard;
                }

                float2 uv = input.uv;
                float height = saturate(uv.y);
                float time = _Time.y;
                float flickerWave = sin(time * _FlickerSpeed + height * _FlickerScale);
                flickerWave += sin(time * _FlickerSpeed * 1.73 - height * _FlickerScale * 0.61) * 0.5;
                float lateralFlicker = flickerWave * _FlickerStrength * lerp(0.2, 1.0, height);
                float2 flowUv = uv + float2(lateralFlicker, 0.0);

                float2 noiseUvA = flowUv * max(abs(_NoiseTilingA.xy), 0.01) +
                    _NoiseScrollA.xy * time;
                float noiseA = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUvA).r;
                float2 distortion = float2((noiseA - 0.5) * _DistortionStrength, 0.0);
                float2 noiseUvB = (flowUv + distortion) *
                    max(abs(_NoiseTilingB.xy), 0.01) + _NoiseScrollB.xy * time;
                float noiseB = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUvB).r;
                float noiseValue = saturate(lerp(noiseA, noiseB, _NoiseBlend));

                float taperedHeight = pow(max(height, 0.0001), _TaperPower);
                float flameWidth = lerp(_BaseWidth, _TipWidth, taperedHeight);
                float horizontalDistance = abs(
                    (uv.x - 0.5 + lateralFlicker + (noiseB - 0.5) *
                    _DistortionStrength) * 2.0);
                float sideMask = 1.0 - smoothstep(
                    flameWidth, flameWidth + _EdgeSoftness, horizontalDistance);

                float threshold = _NoiseThreshold -
                    _BaseSolidness * (1.0 - height) + _TipBreakup * height;
                float breakupMask = smoothstep(
                    threshold - _EdgeSoftness,
                    threshold + _EdgeSoftness,
                    noiseValue);
                float verticalMask = smoothstep(0.0, _BottomFade, height) *
                    (1.0 - smoothstep(1.0 - _TopFade, 1.0, height));
                float flameMask = sideMask * breakupMask * verticalMask * sprite.a;

                half4 lowerColor = lerp(_CoreColor, _MidColor, saturate(height * 2.0));
                half4 upperColor = lerp(
                    _MidColor, _TipColor, saturate((height - 0.5) * 2.0));
                half4 flameColor = lerp(lowerColor, upperColor, step(0.5, height));
                float hotNoise = saturate((noiseValue - threshold) * 2.0) *
                    (1.0 - height) * 0.35;
                flameColor.rgb = lerp(flameColor.rgb, _CoreColor.rgb, hotNoise);

                half3 color = flameColor.rgb * input.color.rgb * _Intensity;
                half alpha = flameMask * flameColor.a * input.color.a * _Opacity;
                clip(alpha - 0.001);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}

