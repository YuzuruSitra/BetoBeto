Shader "BetoBeto/Prop Transparent Fresnel"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base map", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint and opacity", Color) = (.3,.8,.9,.35)
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Float) = 1
        _Smoothness ("Smoothness", Range(0,1)) = .85
        _Metallic ("Metallic", Range(0,1)) = 0
        [HDR] _FresnelColor ("Fresnel glow", Color) = (.65,.95,1,1)
        _FresnelStrength ("Glow strength", Range(0,4)) = 1.5
        _FresnelPower ("Glow falloff", Range(.5,8)) = 2.2
        _EdgeOpacity ("Edge opacity", Range(0,1)) = .65
        _FillStrength ("Translucent color fill", Range(0,1)) = 0
        _SparkleStrength ("Sparkle strength", Range(0,5)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "LitFresnel"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor, _FresnelColor;
                half _BumpScale, _Smoothness, _Metallic, _FresnelStrength, _FresnelPower, _EdgeOpacity;
                half _FillStrength, _SparkleStrength;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half fog : TEXCOORD4;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = p.positionCS; output.positionWS = p.positionWS;
                output.normalWS = n.normalWS;
                output.tangentWS = half4(n.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fog = ComputeFogFactor(p.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 n = NormalizeNormalPerPixel(input.normalWS);
                half3 normalTS = SampleNormal(input.uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
                #ifdef _NORMALMAP
                    half3 tangent = normalize(input.tangentWS.xyz);
                    half3 bitangent = cross(n, tangent) * input.tangentWS.w;
                    n = normalize(TransformTangentToWorld(normalTS, half3x3(tangent, bitangent, n)));
                #endif
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = pow(saturate(1 - dot(n, view)), _FresnelPower);
                half4 base = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)) * _BaseColor;
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = base.rgb; surface.alpha = max(base.a, fresnel * _EdgeOpacity);
                surface.metallic = _Metallic; surface.specular = half3(.5,.5,.5);
                surface.smoothness = _Smoothness; surface.normalTS = normalTS; surface.occlusion = 1;
                surface.emission = _FresnelColor.rgb * fresnel * _FresnelStrength + base.rgb * _FillStrength;
                float2 grid = input.positionWS.xz * 5;
                float2 cell = floor(grid);
                float seed = frac(sin(dot(cell, float2(127.1,311.7))) * 43758.5453);
                float2 p = frac(grid) - float2(.2 + .6 * seed, .2 + .6 * frac(seed * 13.7));
                float star = pow(saturate(1 - abs(p.x) / .035), 2) * pow(saturate(1 - abs(p.y) / .23), 2)
                    + pow(saturate(1 - abs(p.y) / .035), 2) * pow(saturate(1 - abs(p.x) / .23), 2);
                float twinkle = pow(saturate(sin(_Time.y * 3.5 + seed * 20)), 12) * step(.55, seed);
                float sparkle = saturate(star * twinkle) * _SparkleStrength;
                surface.emission += half3(.88,.97,1) * sparkle * 2;
                surface.alpha = max(surface.alpha, saturate(sparkle * .8));
                InputData data = (InputData)0;
                data.positionWS = input.positionWS; data.normalWS = n; data.viewDirectionWS = view;
                data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                data.bakedGI = SampleSH(n); data.vertexLighting = VertexLighting(input.positionWS, n);
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask = half4(1,1,1,1);
                half4 color = UniversalFragmentPBR(data, surface);
                color.rgb = MixFog(color.rgb, input.fog);
                color.a = surface.alpha;
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
