#ifndef LUNAR_SURFACE_DATA_INCLUDED
#define LUNAR_SURFACE_DATA_INCLUDED

// Must match Lunar ShaderGraph master node
struct SurfaceData {
    half3   albedo;
    half3   specular;
    // half    metallic;
    half    smoothness;
    half3   normalTS;
    half3   emission;
    half    occlusion;
    half    alpha;
    // half    clearCoatMask;
    // half    clearCoatSmoothness;
};

SurfaceData InitiliazeEmptySurfaceData() {

    SurfaceData surfaceData;

    surfaceData.albedo = 1.0.xxx;
    surfaceData.specular = 0.0.xxx;
    // surfaceData.metallic = 0;
    surfaceData.smoothness = 1;
    surfaceData.normalTS = half3(0, 0, 1);
    surfaceData.emission = 0.0.xxx;
    surfaceData.occlusion = 1;
    surfaceData.alpha = 1;
    // surfaceData.clearCoatMask = 0;
    // surfaceData.clearCoatSmoothness = 0;

    return surfaceData;
}

#endif
