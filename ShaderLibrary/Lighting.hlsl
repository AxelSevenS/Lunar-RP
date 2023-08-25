#ifndef LUNAR_LIGHTING_INCLUDED
#define LUNAR_LIGHTING_INCLUDED

// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/LightingData.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/CustomLighting.hlsl"

#if defined(LIGHTMAP_ON)
    #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) float2 lmName : TEXCOORD##index
    #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT) OUT.xy = lightmapUV.xy * lightmapScaleOffset.xy + lightmapScaleOffset.zw;
    #define OUTPUT_SH(normalWS, OUT)
#else
    #define DECLARE_LIGHTMAP_OR_SH(lmName, shName, index) half3 shName : TEXCOORD##index
    #define OUTPUT_LIGHTMAP_UV(lightmapUV, lightmapScaleOffset, OUT)
    #define OUTPUT_SH(normalWS, OUT) OUT.xyz = SampleSHVertex(normalWS)
#endif


// half3 VertexLighting(float3 positionWS, half3 normalWS) {
//     half3 vertexLightColor = half3(0.0, 0.0, 0.0);

//     #ifdef _ADDITIONAL_LIGHTS_VERTEX
//         uint lightsCount = GetAdditionalLightsCount();
//         LIGHT_LOOP_BEGIN(lightsCount)
//             Light light = GetAdditionalLight(lightIndex, positionWS);
//             half3 lightColor = light.color * light.distanceAttenuation;
//             vertexLightColor += LightingLambert(lightColor, light.direction, normalWS);
//         LIGHT_LOOP_END
//     #endif

//     return vertexLightColor;
// }

half3 CalculateLightingColor(LightingData lightingData, half3 albedo) {
    half3 lightingColor = 0;

    lightingColor = _SubtractiveShadowColor * albedo;

    if (IsOnlyAOLightingFeatureEnabled()) {
        return lightingData.giColor; // Contains white + AO
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_GLOBAL_ILLUMINATION)) {
        lightingColor += lightingData.giColor;
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_MAIN_LIGHT)) {
        lightingColor += lightingData.mainLightColor;
    }

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_ADDITIONAL_LIGHTS)) {
        lightingColor += lightingData.additionalLightsColor;
    }

    // if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_VERTEX_LIGHTING)) {
    //     lightingColor += lightingData.vertexLightingColor;
    // }

    lightingColor *= albedo;

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_EMISSION)) {
        lightingColor += lightingData.emissionColor;
    }

    return lightingColor;
}

half4 CalculateFinalColor(LightingData lightingData, half alpha) {
    half3 finalColor = CalculateLightingColor(lightingData, 1);

    return half4(finalColor, alpha);
}

half4 CalculateFinalColor(LightingData lightingData, half3 albedo, half alpha, float fogCoord) {
    #if defined(_FOG_FRAGMENT)
        #if (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))
            float viewZ = -fogCoord;
            float nearToFarZ = max(viewZ - _ProjectionParams.y, 0);
            half fogFactor = ComputeFogFactorZ0ToFar(nearToFarZ);
        #else
            half fogFactor = 0;
        #endif
    #else
        half fogFactor = fogCoord;
    #endif
    half3 lightingColor = CalculateLightingColor(lightingData, albedo);
    half3 finalColor = MixFog(lightingColor, fogFactor);

    return half4(finalColor, alpha);
}


////////////////////////////////////////////////////////////////////////////////
/// Lit
////////////////////////////////////////////////////////////////////////////////
half4 LunarFragmentLit(InputData inputData, SurfaceData surfaceData) {
    #if defined(_SPECULARHIGHLIGHTS_OFF)
        bool specularHighlightsOff = true;
    #else
        bool specularHighlightsOff = false;
    #endif
    // BRDFData brdfData;

    // NOTE: can modify "surfaceData"...
    // InitializeBRDFData(surfaceData, brdfData);

    #if defined(DEBUG_DISPLAY)
        half4 debugColor;

        // if (CanDebugOverrideOutputColor(inputData, surfaceData, brdfData, debugColor)) {
        //     return debugColor;
        // }
        // TODO: replace current URP-inherited debug function
    #endif

    // Clear-coat calculation...
    // BRDFData brdfDataClearCoat = CreateClearCoatBRDFData(surfaceData, brdfData);
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    uint meshRenderingLayers = GetMeshRenderingLayer();
    Light mainLight = GetMainLight(inputData, shadowMask, aoFactor);

    // NOTE: We don't apply AO to the GI here because it's done in the lighting calculation below...
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    // lightingData.giColor = GlobalIllumination(brdfData, brdfDataClearCoat, surfaceData.clearCoatMask,
    //                                           inputData.bakedGI, aoFactor.indirectAmbientOcclusion, inputData.positionWS,
    //                                           inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV);
    // TODO: replace current URP-inherited GI Function
    // lightingData.giColor = GlobalIllumination();

#ifdef _LIGHT_LAYERS
    if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
    {
        lightingData.mainLightColor = CustomLighting(inputData, surfaceData, mainLight);
    }

    #if defined(_ADDITIONAL_LIGHTS)
        uint pixelLightCount = GetAdditionalLightsCount();

        // #if USE_FORWARD_PLUS
        // for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++) {
        //     FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

        //     Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

        //     #ifdef _LIGHT_LAYERS
        //         if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
        //     #endif
        //     {
        //         lightingData.additionalLightsColor += CustomLighting(inputData, surfaceData, light);
        //     }
        // }
        // #endif

        LIGHT_LOOP_BEGIN(pixelLightCount)
            Light light = GetAdditionalLight(lightIndex, inputData, shadowMask, aoFactor);

            #ifdef _LIGHT_LAYERS
                if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
            #endif
            {
                lightingData.additionalLightsColor += CustomLighting(inputData, surfaceData, light);
            }
        LIGHT_LOOP_END
    #endif

    #if REAL_IS_HALF
        // Clamp any half.inf+ to HALF_MAX
        return min(CalculateFinalColor(lightingData, surfaceData.albedo, surfaceData.alpha, inputData.fogCoord), HALF_MAX);
    #else
        return CalculateFinalColor(lightingData, surfaceData.albedo, surfaceData.alpha, inputData.fogCoord);
    #endif
}

// Deprecated: Use the version which takes "SurfaceData" instead of passing all of these arguments...
half4 LunarFragmentLit(InputData inputData, half3 albedo, half3 specular, half smoothness, half occlusion, half3 emission, half alpha) {
    SurfaceData surfaceData = InitiliazeEmptySurfaceData();

    surfaceData.albedo = albedo;
    surfaceData.specular = specular;
    surfaceData.smoothness = smoothness;
    surfaceData.emission = emission;
    surfaceData.occlusion = occlusion;
    surfaceData.alpha = alpha;

    return LunarFragmentLit(inputData, surfaceData);
}


////////////////////////////////////////////////////////////////////////////////
/// Unlit
////////////////////////////////////////////////////////////////////////////////
half4 LunarFragmentUnlit(InputData inputData, SurfaceData surfaceData) {

    #if defined(DEBUG_DISPLAY)
        half4 debugColor;

        if (CanDebugOverrideOutputColor(inputData, surfaceData, debugColor)) {
            return debugColor;
        }
    #endif

    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    LightingData lightingData = CreateLightingData(inputData, surfaceData);

    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION)) {
        lightingData.giColor *= aoFactor.indirectAmbientOcclusion;
    }

    return CalculateFinalColor(lightingData, surfaceData.albedo, surfaceData.alpha, inputData.fogCoord);
}

// Deprecated: Use the version which takes "SurfaceData" instead of passing all of these arguments...
half4 LunarFragmentUnlit(InputData inputData, half3 color, half alpha, half3 normalTS) {
    SurfaceData surfaceData = InitiliazeEmptySurfaceData();

    surfaceData.albedo = color;
    surfaceData.alpha = alpha;
    surfaceData.normalTS = normalTS;

    return LunarFragmentUnlit(inputData, surfaceData);
}

#endif
