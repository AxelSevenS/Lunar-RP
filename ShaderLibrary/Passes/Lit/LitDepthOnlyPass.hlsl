#ifndef LIT_DEPTHONLY_PASS_INCLUDED
#define LIT_DEPTHONLY_PASS_INCLUDED


#pragma exclude_renderers gles gles3 glcore
#pragma target 4.5

// -------------------------------------
// Material Keywords
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

//--------------------------------------
// GPU Instancing
#pragma multi_compile_instancing
#pragma multi_compile _ DOTS_INSTANCING_ON

// #include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/Passes/Input/LitInput.hlsl"

#pragma vertex DepthOnlyVertex
#pragma fragment DepthOnlyFragment



VertexOutput DepthOnlyVertex(VertexInput input) {
    
    VertexOutput output = (VertexOutput)0;
    InitializeVertexOutput(output, input);

    return output;
}

half4 DepthOnlyFragment(VertexOutput input) : SV_TARGET {

    // GPU Instancing support
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    return 0;
}

#endif