#ifndef LUNAR_LIT_INPUT_INCLUDED
#define LUNAR_LIT_INPUT_INCLUDED

#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/Lighting.hlsl"

struct VertexInput {
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput {
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float3 tangentWS : TEXCOORD3;
    float3 bitangentWS : TEXCOORD4;
    float3 positionSS : TEXCOORD5;
    float3 viewDirectionWS : TEXCOORD6;
    float2 uv : TEXCOORD0;

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
    #ifdef DYNAMICLIGHTMAP_ON
        float2 dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
    #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#endif // LUNAR_LIT_INPUT_INCLUDED