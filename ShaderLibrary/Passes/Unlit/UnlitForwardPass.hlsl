#ifndef LIT_FORWARD_PASS_INCLUDED
#define LIT_FORWARD_PASS_INCLUDED


// -------------------------------------
// Material Keywords
#pragma shader_feature_local _NORMALMAP
#pragma shader_feature_local _PARALLAXMAP
#pragma shader_feature_local _RECEIVE_SHADOWS_OFF
#pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
#pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
#pragma shader_feature_local_fragment _EMISSION
#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
#pragma shader_feature_local_fragment _OCCLUSIONMAP
#pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
#pragma shader_feature_local_fragment _SPECULAR_SETUP

// -------------------------------------
// Universal Pipeline keywords
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
#pragma multi_compile_fragment _ _LIGHT_LAYERS
#pragma multi_compile_fragment _ _LIGHT_COOKIES
#pragma multi_compile _ _CLUSTERED_RENDERING

// -------------------------------------
// Unity defined keywords
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ DYNAMICLIGHTMAP_ON
#pragma multi_compile_fog
#pragma multi_compile_fragment _ DEBUG_DISPLAY

//--------------------------------------
// GPU Instancing
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#pragma multi_compile _ DOTS_INSTANCING_ON


#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/Input/UnlitInput.hlsl"

    
#pragma vertex ForwardPassVertex
#pragma fragment ForwardPassFragment

VertexOutput ForwardPassVertex(VertexInput input) {
    
    VertexOutput output = (VertexOutput)0;
    InitializeVertexOutput(output, input);

    return output;
}

float4 ForwardPassFragment(VertexOutput input, half facing : VFACE) : SV_Target {

    // GPU Instancing support
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    CustomClipping(input, facing);

    SurfaceData surfaceData = (SurfaceData)0;
    InputData inputData = (InputData)0;
    InitializeLightingData( surfaceData, inputData, input );

    CustomFragment(surfaceData, inputData, input, facing);

    return float4(surfaceData.albedo.xyz, surfaceData.alpha);
    // return LunarFragmentLit(inputData, surfaceData); 
}



#endif