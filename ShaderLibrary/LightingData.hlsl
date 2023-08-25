#ifndef LUNAR_LIGHTING_DATA_INCLUDED
#define LUNAR_LIGHTING_DATA_INCLUDED

#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/Input.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/SurfaceData.hlsl"

struct LightingData {
    half3 mainLightColor;
    half3 additionalLightsColor;
    half3 vertexLightingColor;
    half3 giColor;
    half3 emissionColor;
};

LightingData CreateLightingData(InputData inputData, SurfaceData surfaceData) {
    LightingData lightingData;

    lightingData.mainLightColor = 0;
    lightingData.vertexLightingColor = 0;
    lightingData.additionalLightsColor = 0;
    lightingData.giColor = inputData.bakedGI;
    lightingData.emissionColor = surfaceData.emission;

    return lightingData;
}

#endif
