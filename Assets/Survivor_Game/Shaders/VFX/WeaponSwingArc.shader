Shader "Survivor/VFX/Weapon Swing Arc"
{
    Properties
    {
        [HDR] _OuterColor("Outer Color", Color) = (1, 0.08, 0.01, 0.35)
        [HDR] _MainColor("Main Color", Color) = (1, 0.25, 0.03, 1)
        [HDR] _CoreColor("Core Color", Color) = (4, 1.2, 0.25, 1)
        _ArcRadius("Arc Radius", Range(0.05, 1.2)) = 0.62
        _ArcThickness("Arc Thickness", Range(0.005, 0.5)) = 0.12
        _ArcAngle("Arc Angle", Range(1, 359)) = 150
        _CenterAngle("Center Angle", Range(-180, 180)) = 0
        _TaperPower("Taper Power", Range(0.1, 8)) = 1.5
        _EdgeSoftness("Edge Softness", Range(0.001, 0.2)) = 0.025
        _Reveal("Reveal", Range(0, 1)) = 1
        _Fade("Fade", Range(0, 1)) = 0
        _Reverse("Reverse", Float) = 0
        _Opacity("Opacity", Range(0, 1)) = 1
        _MotionDirection("Motion Direction", Vector) = (1, 0, 0, 0)
        _MotionStrength("Motion Strength", Range(0, 1)) = 0
        _BlurLength("Blur Length", Range(0, 0.5)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            Name "WeaponSwingArc"
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _OuterColor;
                half4 _MainColor;
                half4 _CoreColor;
                float _ArcRadius;
                float _ArcThickness;
                float _ArcAngle;
                float _CenterAngle;
                float _TaperPower;
                float _EdgeSoftness;
                float _Reveal;
                float _Fade;
                float _Reverse;
                float _Opacity;
                float4 _MotionDirection;
                float _MotionStrength;
                float _BlurLength;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 EvaluateArc(float2 samplePosition)
            {
                float radius = length(samplePosition);
                float angle = atan2(samplePosition.y, samplePosition.x);
                float center = radians(_CenterAngle);
                float relativeAngle = atan2(sin(angle - center), cos(angle - center));
                float halfArc = max(radians(_ArcAngle) * 0.5, 0.001);
                float angleFeather = max(_EdgeSoftness / max(_ArcRadius, 0.001), 0.002);
                float angleMask = 1.0 - smoothstep(
                    halfArc - angleFeather,
                    halfArc,
                    abs(relativeAngle));

                float progress = saturate(relativeAngle / (halfArc * 2.0) + 0.5);
                progress = lerp(progress, 1.0 - progress, step(0.5, _Reverse));
                float revealMask = 1.0 - smoothstep(
                    _Reveal - angleFeather,
                    _Reveal,
                    progress);
                float fadeMask = smoothstep(
                    _Fade - angleFeather,
                    _Fade,
                    progress);

                float taper = pow(max(sin(progress * PI), 0.0001), _TaperPower);
                float thickness = max(_ArcThickness * taper, 0.001);
                float radialDistance = abs(radius - _ArcRadius);
                float outer = 1.0 - smoothstep(
                    thickness * 1.75,
                    thickness * 1.75 + _EdgeSoftness,
                    radialDistance);
                float main = 1.0 - smoothstep(
                    thickness,
                    thickness + _EdgeSoftness,
                    radialDistance);
                float core = 1.0 - smoothstep(
                    thickness * 0.22,
                    thickness * 0.22 + _EdgeSoftness * 0.5,
                    radialDistance);

                float mask = angleMask * revealMask * fadeMask;
                half3 color = _OuterColor.rgb * outer * _OuterColor.a;
                color += _MainColor.rgb * main * _MainColor.a;
                color += _CoreColor.rgb * core * _CoreColor.a;
                return half4(color, saturate(max(outer, max(main, core)) * mask));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 samplePosition = (input.uv - 0.5) * 2.0;
                float blur = _BlurLength * saturate(_MotionStrength);

                half4 result;
                if (blur < 0.0005)
                {
                    // 정지에 가까우면 추가 샘플을 아예 건너뛴다.
                    result = EvaluateArc(samplePosition);
                }
                else
                {
                    float2 direction = _MotionDirection.xy;
                    float directionLength = length(direction);
                    direction = directionLength > 0.0001
                        ? direction / directionLength
                        : float2(1.0, 0.0);

                    // 이동 방향 기준 대칭 5탭 1D 가우시안.
                    // 가중치 합이 1이므로 속도가 올라가도 밝아지지 않고 번지기만 한다.
                    float2 nearStep = direction * blur * 0.5;
                    float2 farStep = direction * blur;
                    result  = EvaluateArc(samplePosition - farStep) * 0.0702;
                    result += EvaluateArc(samplePosition - nearStep) * 0.2445;
                    result += EvaluateArc(samplePosition) * 0.3706;
                    result += EvaluateArc(samplePosition + nearStep) * 0.2445;
                    result += EvaluateArc(samplePosition + farStep) * 0.0702;
                }

                result.a *= _Opacity;
                return result;
            }
            ENDHLSL
        }
    }
}
