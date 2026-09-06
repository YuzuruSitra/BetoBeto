Shader "BetoBeto/Kitchen Table Wood"
{
    Properties
    {
        _BaseColor ("Honey maple", Color) = (.52,.33,.19,1)
        _Smoothness ("Varnish", Range(0,1)) = .35
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor; half _Smoothness;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct V { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; };
            V Vert(A a) { V o; o.positionWS=TransformObjectToWorld(a.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.normalWS=TransformObjectToWorldNormal(a.normalOS);return o; }
            half4 Frag(V i):SV_Target
            {
                float2 p=i.positionWS.xz;
                float plank=floor(p.y/1.4);
                float seed=frac(sin(plank*43.3)*5123.5);
                float warp=sin(p.x*.42+seed*9)*.22+sin(p.x*1.3+seed*6)*.04;
                float grain=.5+.5*sin((p.y+warp)*64+sin(p.x*.7)*2);
                float rings=.5+.5*sin((p.y+warp)*18);
                float seam=smoothstep(0,.02,min(frac(p.y/1.4),1-frac(p.y/1.4)));
                SurfaceData s=(SurfaceData)0;
                s.albedo=_BaseColor.rgb*(.81+.12*rings+.035*grain+seed*.1)*lerp(.53,1,seam);
                s.smoothness=_Smoothness;s.occlusion=1;s.alpha=1;s.normalTS=half3(0,0,1);
                InputData d=(InputData)0;
                d.positionWS=i.positionWS;d.normalWS=normalize(i.normalWS);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(d.normalWS);
                d.shadowMask=1;d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                return UniversalFragmentPBR(d,s);
            }
            ENDHLSL
        }
    }
}
