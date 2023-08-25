#ifndef LUNAR_META_PASS_INCLUDED
#define LUNAR_META_PASS_INCLUDED

#pragma only_renderers gles gles3 glcore d3d11
#pragma target 2.0

// #pragma shader_feature EDITOR_VISUALIZATION
#pragma shader_feature_local_fragment _SPECULAR_SETUP
#pragma shader_feature_local_fragment _EMISSION
#pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
#pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED

#pragma shader_feature_local_fragment _SPECGLOSSMAP


#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/Input/MetaInput.hlsl"
// #include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/MetaInput.hlsl"


#pragma vertex UniversalVertexMeta
#pragma fragment UniversalFragmentMetaLit

#ifndef CustomGIContribution
    // Default GI Contribution for Lit shader 

    void DefaultGIContribution( MetaVaryings varyings, inout half4 albedo, inout half4 specularColor ) {
        
        albedo = half4(1, 1, 1, 1);
        specularColor = half4(1, 1, 1, 1);

    }

    #define CustomGIContribution(input, albedo, specularColor) DefaultGIContribution(input, albedo, specularColor)
#endif

MetaVaryings UniversalVertexMeta(MetaAttributes input) {
    MetaVaryings output = (MetaVaryings)0;
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
    output.uv = TRANSFORM_TEX(input.uv0, _MainTex);
    return output;
}

half4 UniversalFragmentMetaLit(MetaVaryings input) : SV_Target {

    half4 albedo;
    half4 specularColor;
    CustomGIContribution(input, albedo, specularColor);

    half3 diffuse = albedo.rgb * (1.0.xxx - specularColor.rgb);

    MetaInput metaInput;
    metaInput.Albedo = diffuse + specularColor * 0.5;
    metaInput.Emission = 10 * albedo.rgb;
#ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = input.VizUV;
    metaInput.LightCoord = input.LightCoord;
#endif

    return UnityMetaFragment(metaInput);
}

#endif