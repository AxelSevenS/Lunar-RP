using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;


namespace LunarRenderPipeline {

    /// <summary>
    /// Container struct for various data used for lights in URP.
    /// </summary>
    // [GenerateHLSL(PackingRules.Exact, false)]
    public struct LightData {
        /// <summary>
        /// The position of the light.
        /// </summary>
        public Vector4 position;

        /// <summary>
        /// The color of the light.
        /// </summary>
        public Vector4 color;

        /// <summary>
        /// The attenuation of the light.
        /// </summary>
        public Vector4 attenuation;

        /// <summary>
        /// The direction of the light (Spot light).
        /// </summary>
        public Vector4 spotDirection;

        /// <summary>
        /// The channel for probe occlusion.
        /// </summary>
        public Vector4 occlusionProbeChannels;

        /// <summary>
        /// The layer mask used.
        /// </summary>
        public uint layerMask;

        public static LightData GetLightData(ref VisibleLight visibleLight) {
            LightData data = new() {
                // Default Values
                position = new Vector4(0, 0, 1f, 0),
                color = Color.black,
                attenuation = new Vector4(0, 1f, 0, 1f),
                spotDirection = new Vector4(0, 0, 1f, 0),
                occlusionProbeChannels = Vector4.zero
            };


            Light light = visibleLight.light;
            Matrix4x4 lightLocalToWorld = visibleLight.localToWorldMatrix;
            LightType lightType = visibleLight.lightType;

            if (lightType == LightType.Directional) {
                Vector4 dir = -lightLocalToWorld.GetColumn(2);
                data.position = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            } else {
                Vector4 pos = lightLocalToWorld.GetColumn(3);
                data.position = new Vector4(pos.x, pos.y, pos.z, 1.0f);

                // Light attenuation in universal matches the unity vanilla one (HINT_NICE_QUALITY).
                // attenuation = 1.0 / distanceToLightSqr
                // The smoothing factor makes sure that the light intensity is zero at the light range limit.
                // (We used to offer two different smoothing factors.)

                // The current smoothing factor matches the one used in the Unity lightmapper.
                // smoothFactor = (1.0 - saturate((distanceSqr * 1.0 / lightRangeSqr)^2))^2
                float lightRange = visibleLight.range;
                float lightRangeSqr = lightRange * lightRange;
                float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
                float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
                float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
                float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightRangeSqr);

                // On all devices: Use the smoothing factor that matches the GI.
                data.attenuation.x = oneOverLightRangeSqr;
                data.attenuation.y = lightRangeSqrOverFadeRangeSqr;

                if (lightType == LightType.Spot) {
                    // Spot Attenuation with a linear falloff can be defined as
                    // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
                    // This can be rewritten as
                    // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
                    // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
                    // If we precompute the terms in a MAD instruction
                    float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * visibleLight.spotAngle * 0.5f);
                    // We need to do a null check for particle lights
                    // This should be changed in the future
                    // Particle lights will use an inline function
                    float cosInnerAngle;
                    if (light?.innerSpotAngle != null) {
                        cosInnerAngle = Mathf.Cos(light.innerSpotAngle * Mathf.Deg2Rad * 0.5f);
                    } else {
                        cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(visibleLight.spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
                    }

                    float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
                    float invAngleRange = 1.0f / smoothAngleRange;
                    float add = -cosOuterAngle * invAngleRange;

                    data.attenuation.z = invAngleRange;
                    data.attenuation.w = add;
                    // GetSpotAngleAttenuation(visibleLight.spotAngle, light?.innerSpotAngle, ref data.attenuation);


                    // Get Spot Direction
                    Vector4 dir = lightLocalToWorld.GetColumn(2);
                    data.spotDirection = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);
                    // GetSpotDirection(ref lightLocalToWorld, out data.spotDirection);
                }
            }

            // VisibleLight.finalColor already returns color in active color space
            data.color = visibleLight.finalColor;

            if (
                light != null && 
                light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                0 <= light.bakingOutput.occlusionMaskChannel &&
                light.bakingOutput.occlusionMaskChannel < 4
            ) {
                data.occlusionProbeChannels[light.bakingOutput.occlusionMaskChannel] = 1.0f;
            }

            return data;
        }
        public static LightData GetLightData(NativeArray<VisibleLight> lights, int lightIndex) {
            // Default Values
            LightData data = new() {
                position = new Vector4(0, 0, 1f, 0),
                color = Color.black,
                attenuation = new Vector4(0, 1f, 0, 0),
                spotDirection = new Vector4(0, 0, 1f, 0),
                occlusionProbeChannels = Vector4.zero
            };

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return data;

            // Avoid memcpys. Pass by ref and locals for multiple uses.
            ref VisibleLight visibleLight = ref lights.UnsafeElementAtMutable(lightIndex);
            data = GetLightData(ref visibleLight);
            return data;
        }
    }

}