#ifndef LIT_SUBSHADER_INCLUDED
#define LIT_SUBSHADER_INCLUDED

#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/Input/UnlitInput.hlsl"
// #include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/GlobalIllumination.hlsl"



#ifndef CustomVertexDisplacement
    // Default vertex displacement for Lit shader

    void DefaultVertexDisplacement( inout VertexOutput output ) {
        
    }

    #define CustomVertexDisplacement(output) DefaultVertexDisplacement(output)
#endif

#ifndef CustomClipping
    bool DefaultClipping( in VertexOutput input, half facing ) {
        return false;
    }

    #define CustomClipping(input, facing) DefaultClipping(input, facing)
#endif

#ifndef CustomFragment
    // Default fragment shader for Lit shader 

    void DefaultFragment( inout SurfaceData surfaceData, inout InputData inputData, VertexOutput input, half facing ) {

        surfaceData.albedo = float3(10, 0, 10);
        surfaceData.alpha = 1;
        surfaceData.specular = 0;
        // surfaceData.metallic = 0;
        surfaceData.smoothness = 0;
        surfaceData.emission = half3(1, 0, 1);

    }

    #define CustomFragment(surfaceData, inputData, input, facing) DefaultFragment(surfaceData, inputData, input, facing)
#endif


void InitializeVertexOutput( inout VertexOutput output, VertexInput input ) {

    // GPU Instancing support
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    input.normalOS = normalize(input.normalOS);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS.xyz);

// #if (SHADERPASS == SHADERPASS_FORWARD) || (SHADERPASS == SHADERPASS_GBUFFER)
//     OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
//     #if defined(DYNAMICLIGHTMAP_ON)
//         output.dynamicLightmapUV.xy = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
//     #endif
//     // OUTPUT_SH(normalWS, output.vertexSH);
// #endif

    output.uv = input.uv;


    CustomVertexDisplacement( output );


    output.positionCS = TransformWorldToHClip( output.positionWS );
    #ifdef SHADERGRAPH_PREVIEW
        output.positionSS = float4(0,0,0,0);
    #else
        output.positionSS = GetVertexPositionNDC( output.positionCS );
    #endif
    
    half sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = TransformObjectToWorldNormal(input.tangentOS.xyz);
    output.bitangentWS = cross(output.normalWS.xyz, output.tangentWS.xyz) * sign;

    output.viewDirectionWS = GetWorldSpaceNormalizeViewDir(output.positionWS);

    
}

void InitializeLightingData( inout SurfaceData surfaceData, inout InputData inputData, VertexOutput input ) {

    surfaceData = InitiliazeEmptySurfaceData();

    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.normalWS = input.normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(inputData.positionWS);

    inputData.normalizedScreenSpaceUV = input.positionSS;

    // Tangent to World space conversion matrix
    inputData.tangentToWorld = half3x3(
        input.bitangentWS.x, input.tangentWS.x, input.normalWS.x, 
        input.bitangentWS.y, input.tangentWS.y, input.normalWS.y, 
        input.bitangentWS.z, input.tangentWS.z, input.normalWS.z
    );
    
}


#endif