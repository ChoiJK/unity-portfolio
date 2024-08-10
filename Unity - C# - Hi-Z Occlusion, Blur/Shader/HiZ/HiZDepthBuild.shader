Shader "HiZ/HiZDepthBuild" 
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _LightTexture ("Shadow Map", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vertex
            #pragma fragment blit
            #include "ShaderInclude_HiZ.cginc"
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vertex
            #pragma fragment reduce
            #include "ShaderInclude_HiZ.cginc"
            ENDCG
        }
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma target 4.6
            #pragma vertex vertex
            #pragma fragment blit
            #include "ShaderInclude_HiZ.cginc"
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma target 4.6
            #pragma vertex vertex
            #pragma fragment reduce
            #include "ShaderInclude_HiZ.cginc"
            ENDCG
        }
    }
}
