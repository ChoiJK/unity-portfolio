Shader "VideoFeedGaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Brightness;
            float2 _MainTex_TexelSize;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            float4 frag (in v2f i) : SV_Target
            {
    	        int width = 4;
                float4 result = float4(0.0, 0.0, 0.0, 1.0);
                float count = 0.0;               
                
                for (int x = -width; x <= width; x++)
                {
                    for (int y = -width; y <= width; y++)
                    {
                        result += tex2D(_MainTex, i.uv+ (float2(x, y) * _MainTex_TexelSize.xy));
                        count += 1.0;
                    }
                }
                result = (result / count);
                result *= _Brightness;
                result.a = 1.0;
                return result;
            }
            ENDCG
        }
    }
}
