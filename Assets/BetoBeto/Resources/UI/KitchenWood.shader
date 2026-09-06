Shader "BetoBeto/Kitchen Wood Backdrop"
{
    Properties { _MainTex ("Kitchen artwork", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        Pass
        {
            ZWrite On Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_ST;
            struct Varyings { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(appdata_base v)
            {
                Varyings o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.texcoord, _MainTex); return o;
            }
            fixed4 frag(Varyings i) : SV_Target { return tex2D(_MainTex, i.uv); }
            ENDCG
        }
    }
}
