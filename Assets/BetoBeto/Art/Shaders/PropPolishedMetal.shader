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
        _ReflectionStrength ("Kitchen reflection exposure", Range(0,4)) = .8
        _BladePlanarMap ("Planar silver reflection bands", 2D) = "gray" {}
        _PlanarStrength ("Planar reflection exposure", Range(0,4)) = .65
        _PlanarScale ("Bands per local unit", Float) = 1.3
        _EyeWarp ("View direction distortion", Range(0,1)) = .35
        _OrthoReflectionFov ("Reflection FOV for orthographic camera", Range(10,90)) = 45
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
            TEXTURE2D(_BladePlanarMap); SAMPLER(sampler_BladePlanarMap);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Smoothness, _Metallic, _ReflectionStrength;
                float _PlanarStrength, _PlanarScale;
                float _EyeWarp, _OrthoReflectionFov;
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
                // Project in camera-aligned world XZ, relative to the rotor centre.
                // The bands stay still as the blade spins underneath; object rotation never rotates UVs.
                float2 cameraRightXZ=UNITY_MATRIX_V[0].xz;
                cameraRightXZ*=rsqrt(max(dot(cameraRightXZ,cameraRightXZ),.0001));
                float2 cameraForwardXZ=float2(-cameraRightXZ.y,cameraRightXZ.x);
                float2 plane=input.positionWS.xz-TransformObjectToWorld(float3(0,0,0)).xz;
                float2 uv=float2(dot(plane,cameraRightXZ),dot(plane,cameraForwardXZ))*_PlanarScale+.5;
                // Perspective eye rays bias the reflection across the screen. Orthographic gameplay
                // uses a virtual reflection-only FOV; the camera and board projection stay unchanged.
                float4 clip=TransformWorldToHClip(input.positionWS);
                float2 ndc=clip.xy/max(abs(clip.w),.0001);
                float2 raySlope=ndc/float2(UNITY_MATRIX_P._m00,UNITY_MATRIX_P._m11);
                float aspect=abs(UNITY_MATRIX_P._m11/UNITY_MATRIX_P._m00);
                float2 orthoSlope=ndc*float2(aspect,1)*tan(radians(_OrthoReflectionFov*.5));
                raySlope=lerp(raySlope,orthoSlope,unity_OrthoParams.w);
                float2 eye=normalize(float3(raySlope,1)).xy;
                // Offset and gently stretch bands toward the peripheral eye direction.
                uv+=_EyeWarp*(eye+.45*eye*dot(uv-.5,eye));
                half3 planar=SAMPLE_TEXTURE2D(_BladePlanarMap,sampler_BladePlanarMap,uv).rgb;
                half3 reflected=environment*_ReflectionStrength+planar*_PlanarStrength;
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
