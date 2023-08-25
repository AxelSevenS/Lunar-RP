#ifndef CEL_LIGHTING_INCLUDED
#define CEL_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/GlobalIllumination.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.seven.lunar-render-pipeline/ShaderLibrary/Input.hlsl"
#include "Packages/com.seven.utility/ShaderLibrary/MathUtility.hlsl"

#define _AmbientLight _MainLightColor.rgb*0.35


half3 ColorSaturation(half3 color, half saturation) {
    if (saturation == 0)
        return color;

    half3 hsv = RgbToHsv(color);

    half3 saturatedColor = HsvToRgb(half3(hsv.r, saturation, hsv.b));

    return saturate(saturatedColor);
}

half PhongReflection( half3 normal, half3 viewDir, half3 lightDir, half smoothness ) {
    half3 V = normalize( -viewDir );
    half3 R = reflect( normalize( lightDir ), normalize( normal ) );
    return pow( saturate( dot( V, R ) ), smoothness );
}

half GetAccent(half luminance) {
    half safeLuminance = saturate(luminance);

    // P-Curve
    // half h = 4.5 * safeLuminance + 1;
    // return h * exp(1-h);

    // Simplified Bezier Curve
    half bezier = pow(1 - safeLuminance, 1.5 * safeLuminance);
    return saturate(bezier);
}

half GetSpecular(half3 worldNormal, half3 worldViewDirection, half3 lightDirectionWS, half smoothness, half shade) {
    half phong = PhongReflection(worldNormal, worldViewDirection, lightDirectionWS, smoothness*100);
    return smoothstep(0.15, 1.0, phong * shade);
}

half GetRadiance(half3 worldNormal, half3 lightDirectionWS) {
    return saturate( dot(worldNormal, lightDirectionWS) );
}

half GetShade(half radiance, half attenuation) {
    // both of these values are between 0 and 1
    const half shadeUpperLimit = 0.15; // This can be changed to make the light cover more or less of the object.
    const half lightLowerLimit = 0.55; // This can be changed to make the light more or less cartoony.

    half smoothedRadiance = smoothstep(shadeUpperLimit, lightLowerLimit, radiance) * smoothstep(0, lightLowerLimit - shadeUpperLimit, attenuation);
    // smoothedRadiance = remap(smoothedRadiance, shadeUpperLimit, lightLowerLimit, 0, 1);

    return smoothedRadiance;
}



// --------------------------------------------------------------------------------------
// This is the function that is called by both Deferred and Forward rendering paths
// It is called once per light
// It uses InputData and SurfaceData to get the data it needs to calculate the lighting
// To customize the lighting, simply modify the code below

// InputData contains :
// inputData.positionWS : world space position of the fragment
// inputData.positionCS : clip space position of the fragment
// inputData.normalWS : world space normal of the fragment
// inputData.viewDirectionWS : world space view direction of the fragment
// inputData.fogCoord : fog coord of the fragment
// inputData.vertexLighting : vertex lighting of the fragment
// inputData.normalizedScreenSpaceUV : normalized screen space UV of the fragment
// inputData.shadowMask : shadow mask of the fragment
// inputData.tangentToWorld : tangent to world matrix of the fragment

// SurfaceData contains :
// surfaceData.albedo : albedo of the fragment
// surfaceData.specular : specular color of the fragment
// surfaceData.metallic : metallic of the fragment
// surfaceData.smoothness : smoothness of the fragment
// surfaceData.normalTS : tangent space normal of the fragment
// surfaceData.emission : emission color of the fragment
// surfaceData.occlusion : occlusion of the fragment
// surfaceData.alpha : alpha of the fragment
half3 CustomLighting( InputData inputData, SurfaceData surfaceData, Light light ) {
    light.direction = normalize(light.direction);
    
    half radiance = GetRadiance(inputData.normalWS, light.direction);
    half shade = GetShade(radiance, (light.distanceAttenuation /* * light.shadowAttenuation */));

    half3 finalColor = shade * light.color;
    
    half accentIntensity = (1 - surfaceData.smoothness);
    if (accentIntensity > 0) {
        half accent = GetAccent(shade);

        half3 hsv = RgbToHsv(surfaceData.albedo);
        half saturation = hsv.g * (1 + accentIntensity * 2);
        half3 saturatedColor = saturate(HsvToRgb(half3(hsv.r, saturation, hsv.b))) * light.color;

        finalColor = lerp(finalColor, saturatedColor, accent * shade);
    }


    half specularIntensity = length(surfaceData.specular);
    if (specularIntensity > 0) {
        half specular = GetSpecular(inputData.normalWS, inputData.viewDirectionWS, light.direction, surfaceData.smoothness, shade);
        finalColor += light.color * surfaceData.specular * specular;
    }

    return finalColor;

}


#endif