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
        _CeilingMap ("Kitchen ceiling reflection", 2D) = "black" {}
        _CeilingStrength ("Ceiling reflection opacity", Range(0,1)) = 0
        _CeilingScale ("Ceiling projection scale", Float) = .24
        _CeilingEyeWarp ("Ceiling view distortion", Range(0,1)) = .18
        _LiquidNormalStrength ("Liquid ripple normal strength", Range(0,.5)) = 0
        _LiquidWaveScale ("Liquid waves per world unit", Range(.5,8)) = 2.6
        _LiquidWaveSpeed ("Liquid ripple speed", Range(0,3)) = .7
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
            TEXTURE2D(_CeilingMap); SAMPLER(sampler_CeilingMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor, _FresnelColor;
                half _BumpScale, _Smoothness, _Metallic, _FresnelStrength, _FresnelPower, _EdgeOpacity;
                half _FillStrength, _SparkleStrength;
                float _CeilingStrength, _CeilingScale, _CeilingEyeWarp;
                float _LiquidNormalStrength, _LiquidWaveScale, _LiquidWaveSpeed;
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
                half3 stillNormal=n;
                if (_LiquidNormalStrength > 0)
                {
                    // Analytic height gradients of three crossing waves: smooth liquid normals,
                    // with no mesh displacement, tangent/UV dependence, or extra texture fetches.
                    float2 wavePosition=input.positionWS.xz*(_LiquidWaveScale*6.2831853);
                    float time=_Time.y*_LiquidWaveSpeed;
                    float2 d0=float2(.9659,.2588),d1=float2(-.4226,.9063),d2=float2(.5736,-.8192);
                    float2 slope=d0*cos(dot(wavePosition,d0)+time)*.55
                        +d1*cos(dot(wavePosition,d1)*1.73-time*1.13+1.7)*.30
                        +d2*cos(dot(wavePosition,d2)*2.57+time*.53+3.1)*.15;
                    slope*=_LiquidNormalStrength*saturate(stillNormal.y)*saturate(stillNormal.y);
                    half3 gradient=half3(slope.x,0,slope.y);
                    gradient-=stillNormal*dot(gradient,stillNormal);
                    n=normalize(stillNormal-gradient);
                }
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
                if (_CeilingStrength > 0)
                {
                    // Project the upper kitchen panorama onto the puddle, independently of its rotation.
                    // The wide window/ceiling shapes provide a dielectric reflection, not chrome bands.
                    float2 right=UNITY_MATRIX_V[0].xz;
                    right*=rsqrt(max(dot(right,right),.0001));
                    float2 forward=float2(-right.y,right.x);
                    float2 plane=input.positionWS.xz-TransformObjectToWorld(float3(0,0,0)).xz;
                    float2 projected=float2(dot(plane,right),dot(plane,forward));
                    float4 clip=TransformWorldToHClip(input.positionWS);
                    float2 ndc=clip.xy/max(abs(clip.w),.0001);
                    float2 slope=ndc/float2(UNITY_MATRIX_P._m00,UNITY_MATRIX_P._m11);
                    float aspect=abs(UNITY_MATRIX_P._m11/UNITY_MATRIX_P._m00);
                    slope=lerp(slope,ndc*float2(aspect,1)*.41421356,unity_OrthoParams.w);
                    float2 eye=normalize(float3(slope,1)).xy;
                    float2 ceilingUV=float2(.39,.74)+projected*_CeilingScale;
                    ceilingUV+=eye*_CeilingEyeWarp;
                    // Slight stationary curvature gives the liquid a soft lens-like distortion.
                    ceilingUV+=projected*dot(projected,projected)*.035;
                    // The same wave normals bend both PBR highlights and the ceiling image.
                    float2 ripple=(n-stillNormal).xz;
                    ceilingUV+=float2(dot(ripple,right),dot(ripple,forward))*.18;
                    ceilingUV.y=clamp(ceilingUV.y,.54,.96);
                    half3 ceiling=SAMPLE_TEXTURE2D_LOD(_CeilingMap,sampler_CeilingMap,ceilingUV,2).rgb;
                    half brightness=Luminance(ceiling);
                    ceiling=lerp(brightness.xxx,ceiling,.3);
                    half grazing=pow(1-saturate(dot(n,view)),2);
                    half reflectedAlpha=saturate(brightness*_CeilingStrength*(.7+.3*grazing)*saturate(n.y));
                    // Composite only the reflected patches; clear areas retain the original floor visibility.
                    half alpha=surface.alpha+reflectedAlpha*(1-surface.alpha);
                    color.rgb=(color.rgb*surface.alpha+ceiling*reflectedAlpha*(1-surface.alpha))/max(alpha,.0001);
                    surface.alpha=alpha;
                }
                color.rgb = MixFog(color.rgb, input.fog);
                color.a = surface.alpha;
                return color;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
