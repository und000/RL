Shader "Survivor/MovementStamp"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Smoke("Smoke Shape", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma multi_compile_instancing
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float _Smoke;
            CBUFFER_END
            struct Input { float4 position:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Output { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            Output vert(Input v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                SetUpSpriteInstanceProperties();
                v.position.xyz = UnityFlipSprite(v.position.xyz, unity_SpriteProps.xy);
                Output o;
                o.position=TransformObjectToHClip(v.position.xyz);
                o.uv=v.uv;
                o.color=v.color * unity_SpriteColor;
                return o;
            }
            half4 frag(Output i):SV_Target
            {
                float alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a;
                if (_Smoke > .5)
                {
                    float2 p=i.uv*2-1;
                    float r=length(p);
                    float edge=.65+.09*sin(atan2(p.y,p.x)*5);
                    alpha=1-smoothstep(edge*.45,edge,r);
                }
                return half4(i.color.rgb,i.color.a*alpha);
            }
            ENDHLSL
        }
    }
}
