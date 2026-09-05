Shader "BetoBeto/Drool Liquid"
{
    Properties
    {
        _Tint ("Water tint", Color) = (0.64, 0.95, 0.86, 1)
        _Opacity ("Optical opacity", Range(0, 1)) = 0.24
        _Refraction ("Refraction UV distance", Range(0, 0.03)) = 0.009
        _RimColor ("Rim color", Color) = (0.65, 0.95, 0.88, 1)
        _RimStrength ("Rim light", Range(0, 2)) = 0.5
        _SpecularStrength ("Specular reflection", Range(0, 4)) = 1.7
        _SpecularPower ("Specular sharpness", Range(8, 256)) = 96
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" "RenderType"="Transparent" }
        Pass
        {
            Name "DroolLiquid"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half4 _RimColor;
                float _Opacity, _Refraction, _RimStrength, _SpecularStrength, _SpecularPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                half4 color : COLOR;
                half fog : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs basis = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = basis.normalWS;
                output.tangentWS = basis.tangentWS;
                output.uv = input.uv;
                output.color = input.color;
                output.fog = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float across = input.uv.y * 2 - 1;
                float coverage = 1 - smoothstep(.72, 1, abs(across));
                coverage *= input.color.a;
                // Reconstruct a rounded water tube from the camera-facing ribbon's lighting basis.
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float3 front = normalize(input.normalWS);
                front *= dot(front, view) < 0 ? -1 : 1;
                float3 side = normalize(cross(front, normalize(input.tangentWS)));
                float wave = sin(input.uv.x * 45 + input.positionWS.y * 17) * .075;
                float3 normal = normalize(front * sqrt(saturate(1 - across * across)) + side * (across + wave));
                float fresnel = pow(1 - saturate(dot(normal, view)), 3);
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float2 bend = mul((float3x3)UNITY_MATRIX_V, normal).xy;
                half3 background = SampleSceneColor(saturate(uv + bend * _Refraction * coverage));
                // Most transmitted color is the distorted background, with only a faint mint tint.
                half3 transmitted = background * lerp(half3(1, 1, 1), _Tint.rgb, .09);
                Light light = GetMainLight();
                float3 halfway = SafeNormalize(light.direction + view);
                float specular = pow(saturate(dot(normal, halfway)), _SpecularPower);
                float broadReflection = pow(saturate(dot(normal, halfway)), 18) * .1;
                half3 highlight = light.color * (specular + broadReflection) * _SpecularStrength;
                highlight += _RimColor.rgb * fresnel * _RimStrength;
                float alpha = saturate((_Opacity + fresnel * .22) * coverage);
                half3 color = transmitted * alpha + highlight * coverage;
                color = MixFogColor(color, unity_FogColor.rgb * alpha, input.fog);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
