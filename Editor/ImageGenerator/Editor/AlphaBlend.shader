Shader "Hidden/Pan/AlphaBlend"
{
    Properties
    {
        _SourceTex ("Source Texture", 2D) = "white" {}
        _TargetTex ("Target Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _SourceTex;
            sampler2D _TargetTex;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 sourceColor = tex2D(_SourceTex, i.uv);
                float4 targetColor = tex2D(_TargetTex, i.uv);

                // Alpha blending: source over target
                float alpha = sourceColor.a + targetColor.a * (1.0 - sourceColor.a);
                float3 color = (sourceColor.rgb * sourceColor.a + targetColor.rgb * targetColor.a * (1.0 - sourceColor.a)) / alpha;

                return float4(color, alpha);
            }
            ENDCG
        }
    }
}
