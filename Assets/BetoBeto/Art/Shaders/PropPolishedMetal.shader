Shader "BetoBeto/Polished Prop Metal"
{
    Properties
    {
        [MainColor] _BaseColor ("Silver tint", Color) = (.78,.85,.92,1)
        _BaseMap ("Base map", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _Smoothness ("Polish", Range(0,1)) = .84
        _Metallic ("Metallic", Range(0,1)) = 1
        _ReflectionStrength ("Kitchen reflection exposure", Range(0,4)) = 2.2
        _MetalReflection ("Blade contrast reflection", Cube) = "black" {}
        _MetalReflectionStrength ("Additive highlight reflection", Range(0,2)) = .28
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURECUBE(_MetalReflection); SAMPLER(sampler_MetalReflection);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness, _Metallic, _ReflectionStrength, _MetalReflectionStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings
            {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                float3 positionOS:TEXCOORD1;
                half3 normalWS:TEXCOORD2;
                half fog:TEXCOORD3;
            };
            Varyings Vert(Attributes input)
            {
                Varyings o; VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS=p.positionCS; o.positionWS=p.positionWS; o.positionOS=input.positionOS.xyz;
                o.normalWS=TransformObjectToWorldNormal(input.normalOS); o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half3 n=normalize(input.normalWS), v=GetWorldSpaceNormalizeViewDir(input.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord=ComputeScreenPos(TransformWorldToHClip(input.positionWS));
                #else
                    float4 shadowCoord=TransformWorldToShadowCoord(input.positionWS);
                #endif
                Light light=GetMainLight(shadowCoord,input.positionWS,half4(1,1,1,1));
                SurfaceData surface=(SurfaceData)0;
                surface.albedo=_BaseColor.rgb;surface.metallic=_Metallic;
                surface.smoothness=_Smoothness;surface.alpha=1;surface.occlusion=1;
                BRDFData brdf;InitializeBRDFData(surface,brdf);
                half3 direction=reflect(-v,n);
                half3 environment=GlossyEnvironmentReflection(direction,input.positionWS,1-_Smoothness,1);
                half mip=PerceptualRoughnessToMipmapLevel(1-_Smoothness);
                half3 dedicated=SAMPLE_TEXTURECUBE_LOD(_MetalReflection,sampler_MetalReflection,direction,mip).rgb;
                // Preserve the kitchen reflection; black in the highlight map contributes nothing.
                half3 reflected=environment*_ReflectionStrength+dedicated*_MetalReflectionStrength;
                half fresnel=Pow4(1-saturate(dot(n,v)));
                half3 color=LightingPhysicallyBased(brdf,light,n,v)
                    +EnvironmentBRDF(brdf,SampleSH(n),reflected,fresnel);
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack Off
}
