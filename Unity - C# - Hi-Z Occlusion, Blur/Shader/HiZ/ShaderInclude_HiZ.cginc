#ifndef __HIZ_INCLUDE__
#define __HIZ_INCLUDE__

#include "UnityCG.cginc"

struct Input
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 scrPos : TEXCOORD1;
};

sampler2D _MainTex;
sampler2D _CameraDepthTexture;

float4 _MainTex_TexelSize;

Varyings vertex(in Input i)
{
    Varyings output;

    output.vertex = UnityObjectToClipPos(i.vertex.xyz);
    output.uv = i.uv;
    output.scrPos=ComputeScreenPos(output.vertex);

    return output;
}

float getDepth(float2 uv)
{
    float depth = tex2D(_CameraDepthTexture, uv).r;
    #if SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3
    return 2.0 - ((depth + 1.0));
    #else
    return depth;
    #endif
}

float4 blit(in Varyings input) : SV_Target
{
    float camDepth = getDepth(input.uv);
    return float4(camDepth, 0, 0 ,0);
}

float4 reduce(in Varyings input) : SV_Target
{
    float2 xy = input.uv * (_MainTex_TexelSize.zw-1);
    float2 uv0 = xy / _MainTex_TexelSize.zw;
    float2 uv1 = (xy + float2(1, 0)) / _MainTex_TexelSize.zw;
    float2 uv2 = (xy + float2(0, 1)) / _MainTex_TexelSize.zw;
    float2 uv3 = (xy + 1) / _MainTex_TexelSize.zw;
    float2 texels[2] = {
        float2(tex2Dlod(_MainTex, float4( uv0, 0, 0)).r, tex2Dlod(_MainTex, float4( uv1, 0, 0)).r),
        float2(tex2Dlod(_MainTex, float4( uv2, 0, 0)).r, tex2Dlod(_MainTex, float4( uv3, 0, 0)).r)
    };
    
    float4 r = float4(texels[0].rg, texels[1].rg);

    float minimum = min(min(min(r.x, r.y), r.z), r.w);
    return float4(minimum, 1.0, 1.0, 1.0);
}

#endif
