Shader "Survivor/VFX/Sprite Sharp Hole Dissolve"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        _NoiseTex("Dissolve Noise Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1, 1, 1, 1)
        _Dissolve("Dissolve", Range(0, 1)) = 0
        _DissolveRotation("Dissolve Rotation (Clockwise)", Range(-180, 180)) = 0
        _NoiseTiling("Noise Tiling", Vector) = (1, 1, 0, 0)
        _NoiseAngle("Noise Angle", Range(-180, 180)) = 0
        _NoiseArcBendAngle("Noise Center Bend Angle", Range(-150, 150)) = 60
        _NoiseArcBendCenter("Noise Arc Bend Center", Vector) = (0.5, 0.5, 0, 0)
        _NoiseScrollSpeed("Noise UV Scroll Speed", Vector) = (0.8, 0, 0, 0)
        _NoiseContrast("Noise Contrast", Range(0.1, 4)) = 1.35
        _HoleSoftness("Hole Softness", Range(0.001, 0.2)) = 0.055
        _NoiseOffset("Noise Offset", Vector) = (0, 0, 0, 0)
        _DissolveDirection("Dissolve Direction", Vector) = (1, 0, 0, 0)
        _DirectionalInfluence("Progress Spread", Range(0, 1)) = 0.7
        _ShapeRoundness("Shape Roundness", Range(0, 1)) = 1
        _RadialCenter("Radial Center", Vector) = (0.5, 0.5, 0, 0)
        _RadialAspect("Radial Aspect", Vector) = (1, 1, 0, 0)
        [Toggle] _RadialInvert("Radial Outside In", Float) = 0
        [Toggle] _Reverse("Reverse", Float) = 0
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

        Pass
        {
            Name "SpriteSharpHoleDissolve"
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
                float _Dissolve;
                float _DissolveRotation;
                float4 _NoiseTex_ST;
                float4 _NoiseTiling;
                float _NoiseAngle;
                float _NoiseArcBendAngle;
                float4 _NoiseArcBendCenter;
                float4 _NoiseScrollSpeed;
                float _NoiseContrast;
                float _HoleSoftness;
                float4 _NoiseOffset;
                float4 _DissolveDirection;
                float _DirectionalInfluence;
                float _ShapeRoundness;
                float4 _RadialCenter;
                float4 _RadialAspect;
                float _RadialInvert;
                float _Reverse;
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
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) *
                    input.color;
                if (sprite.a <= 0.0001) discard;

                // Inverse sampling rotation turns the entire dissolve clockwise,
                // including the bend, noise and progression, around the UV center.
                float rotationRadians = radians(_DissolveRotation);
                float rotationSine = sin(rotationRadians);
                float rotationCosine = cos(rotationRadians);
                float2 effectCenteredUv = input.uv - 0.5;
                float2 dissolveUv = float2(
                    effectCenteredUv.x * rotationCosine - effectCenteredUv.y * rotationSine,
                    effectCenteredUv.x * rotationSine + effectCenteredUv.y * rotationCosine) + 0.5;

                float2 direction = _DissolveDirection.xy;
                direction = dot(direction, direction) > 0.0001
                    ? normalize(direction) : float2(1.0, 0.0);
                direction = lerp(direction, -direction, step(0.5, _Reverse));
                float2 centeredUv = dissolveUv - 0.5;
                float2 bendPosition = dissolveUv - _NoiseArcBendCenter.xy;
                float bendRadians = radians(clamp(
                    _NoiseArcBendAngle, -150.0, 150.0) * 0.5);
                float bendStrength = tan(bendRadians);
                float horizontalDistance = bendPosition.x;
                bendPosition.y += bendStrength * horizontalDistance *
                    horizontalDistance * 2.0;

                float2 bentUv = bendPosition + _NoiseArcBendCenter.xy;
                float noiseRadians = radians(_NoiseAngle);
                float noiseSine = sin(noiseRadians);
                float noiseCosine = cos(noiseRadians);
                float2 noiseCenteredUv = bentUv - 0.5;
                float2 noiseRotatedUv = float2(
                    noiseCenteredUv.x * noiseCosine - noiseCenteredUv.y * noiseSine,
                    noiseCenteredUv.x * noiseSine + noiseCenteredUv.y * noiseCosine);
                float2 noiseUv = noiseRotatedUv *
                    max(abs(_NoiseTiling.xy), 0.01) + 0.5 + _NoiseOffset.xy +
                    _NoiseScrollSpeed.xy * _Time.y;
                float noiseValue = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                    noiseUv).r;
                noiseValue = saturate((noiseValue - 0.5) * _NoiseContrast + 0.5);

                float directionalPosition = saturate(dot(centeredUv, direction) + 0.5);
                float2 radialAspect = max(abs(_RadialAspect.xy), 0.01);
                float2 radialUv = (dissolveUv - _RadialCenter.xy) * radialAspect;
                float radialPosition = saturate(length(radialUv) * 1.41421356);
                radialPosition = lerp(radialPosition, 1.0 - radialPosition,
                    step(0.5, _RadialInvert));
                float shapePosition = lerp(directionalPosition, radialPosition,
                    saturate(_ShapeRoundness));
                float influence = saturate(_DirectionalInfluence);
                float localProgress = saturate(
                    _Dissolve * (1.0 + influence) - shapePosition * influence);
                float active = step(0.001, localProgress);
                float endFade = 1.0 - smoothstep(0.88, 1.0, _Dissolve);
                float thresholdVisible = smoothstep(localProgress - _HoleSoftness,
                    localProgress + _HoleSoftness, noiseValue);
                float visible = lerp(1.0, thresholdVisible, active) * endFade;
                half3 color = sprite.rgb;
                half alpha = sprite.a * visible;
                clip(alpha - 0.001);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
