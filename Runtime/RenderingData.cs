using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Collections;


namespace LunarRenderPipeline {
    
    /// <summary>
    /// Struct that flattens several rendering settings used to render a camera stack.
    /// URP builds the <c>RenderingData</c> settings from several places, including the pipeline asset, camera and light settings.
    /// The settings also might vary on different platforms and depending on if Adaptive Performance is used.
    /// </summary>
    public struct RenderingData {
        internal CommandBuffer commandBuffer;

        /// <summary>
        /// Returns culling results that exposes handles to visible objects, lights and probes.
        /// You can use this to draw objects with <c>ScriptableRenderContext.DrawRenderers</c>
        /// <see cref="CullingResults"/>
        /// <seealso cref="ScriptableRenderContext"/>
        /// </summary>
        public CullingResults cullingResults;

        /// <summary>
        /// Holds several rendering settings related to camera.
        /// <see cref="CameraData"/>
        /// </summary>
        public CameraData cameraData;

        public LunarRenderer renderer;

        /// <summary>
        /// Holds the main light index from the <c>VisibleLight</c> list returned by culling. If there's no main light in the scene, <c>mainLightIndex</c> is set to -1.
        /// The main light is the directional light assigned as Sun source in light settings or the brightest directional light.
        /// <seealso cref="CullingResults"/>
        /// </summary>
        public int mainLightIndex;

        /// <summary>
        /// The number of additional lights visible by the camera.
        /// </summary>
        public int additionalLightsCount;

        /// <summary>
        /// Maximum amount of lights that can be shaded per-object. This value only affects forward rendering.
        /// </summary>
        public int maxPerObjectAdditionalLightsCount;

        /// <summary>
        /// True if light layers are enabled.
        /// </summary>
        public bool supportsLightLayers;

        /// <summary>
        /// True if additional lights enabled.
        /// </summary>
        public bool supportsAdditionalLights;

        /// <summary>
        /// True if main light shadows are enabled.
        /// </summary>
        public bool supportsMainLightShadows;

        /// <summary>
        /// The width of the main light shadow map.
        /// </summary>
        public int mainLightShadowmapWidth;

        /// <summary>
        /// The height of the main light shadow map.
        /// </summary>
        public int mainLightShadowmapHeight;

        /// <summary>
        /// The number of shadow cascades.
        /// </summary>
        public int mainLightShadowCascadesCount;

        /// <summary>
        /// The split between cascades.
        /// </summary>
        public Vector3 mainLightShadowCascadesSplit;

        /// <summary>
        /// Main light last cascade shadow fade border.
        /// Value represents the width of shadow fade that ranges from 0 to 1.
        /// Where value 0 is used for no shadow fade.
        /// </summary>
        public float mainLightShadowCascadeBorder;

        /// <summary>
        /// True if additional lights shadows are enabled.
        /// </summary>
        public bool supportsAdditionalLightShadows;

        /// <summary>
        /// The width of the additional light shadow map.
        /// </summary>
        public int additionalLightsShadowmapWidth;

        /// <summary>
        /// The height of the additional light shadow map.
        /// </summary>
        public int additionalLightsShadowmapHeight;

        /// <summary>
        /// True if soft shadows are enabled.
        /// </summary>
        public bool supportsSoftShadows;

        /// <summary>
        /// The number of bits used.
        /// </summary>
        public int shadowmapDepthBufferBits;

        /// <summary>
        /// A list of shadow bias.
        /// </summary>
        public List<Vector4> bias;

        /// <summary>
        /// A list of resolution for the shadow maps.
        /// </summary>
        public List<int> resolution;

        // /// <summary>
        // /// Holds several rendering settings related to shadows.
        // /// <see cref="ShadowData"/>
        // /// </summary>
        // public ShadowData shadowData;

        // /// <summary>
        // /// Holds several rendering settings and resources related to the integrated post-processing stack.
        // /// <see cref="PostProcessData"/>
        // /// </summary>
        // public PostProcessingData postProcessingData;

        /// <summary>
        /// True if the pipeline supports dynamic batching.
        /// This settings doesn't apply when drawing shadow casters. Dynamic batching is always disabled when drawing shadow casters.
        /// </summary>
        public bool supportsDynamicBatching;

        public bool supportsInstancing;

        // /// <summary>
        // /// Holds per-object data that are requested when drawing
        // /// <see cref="PerObjectData"/>
        // /// </summary>
        // public PerObjectData perObjectData;

        /// <summary>
        /// The sorting criteria used when drawing opaque objects by the internal URP render passes.
        /// When a GPU supports hidden surface removal, URP will rely on that information to avoid sorting opaque objects front to back and
        /// benefit for more optimal static batching.
        /// </summary>
        /// <seealso cref="SortingCriteria"/>
        public SortingCriteria defaultOpaqueSortFlags;

        /// <summary>
        /// Maximum shadow distance visible to the camera. When set to zero shadows will be disable for that camera.
        /// </summary>
        public float maxShadowDistance;

        // /// <summary>
        // /// True if post-processing effect is enabled while rendering the camera stack.
        // /// </summary>
        // public bool postProcessingEnabled;


        public static RenderingData GetRenderingData(LunarRenderPipelineAsset asset, LunarRenderer renderer, ref CameraData cameraData, ref CullingResults cullingResults, bool anyPostProcessingEnabled, CommandBuffer cmd) {
            RenderingData renderingData = new() {
                renderer = renderer
            };
            // using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeRenderingData);

            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            Camera camera = cameraData.camera;


            int mainLightIndex = LunarLightManager.GetMainLightIndex(/* settings,  */visibleLights);
            bool mainLightCastShadows = false;
            bool additionalLightsCastShadows = false;

            bool anyShadowsEnabled = true/* settings.supportsMainLightShadows || settings.supportsAdditionalLightShadows */;
            renderingData.maxShadowDistance = Mathf.Min(Mathf.Infinity/* settings.shadowDistance */, camera.farClipPlane);
            renderingData.maxShadowDistance = (anyShadowsEnabled && renderingData.maxShadowDistance >= camera.nearClipPlane) ? renderingData.maxShadowDistance : 0.0f;

            if (renderingData.maxShadowDistance > 0.0f) {
                mainLightCastShadows = (mainLightIndex != -1 && visibleLights[mainLightIndex].light != null && visibleLights[mainLightIndex].light.shadows != LightShadows.None);

                // If additional lights are shaded per-vertex they cannot cast shadows
                // if (settings.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
                // {
                    for (int i = 0; i < visibleLights.Length; ++i) {
                        if (i == mainLightIndex)
                            continue;

                        ref VisibleLight vl = ref visibleLights.UnsafeElementAtMutable(i);
                        Light light = vl.light;

                        // UniversalRP doesn't support additional directional light shadows yet
                        if ((vl.lightType == LightType.Spot || vl.lightType == LightType.Point) && light != null && light.shadows != LightShadows.None) {
                            additionalLightsCastShadows = true;
                            break;
                        }
                    }
                // }
            }

            renderingData.cullingResults = cullingResults;
            renderingData.cameraData = cameraData;

            // Light Settings
            renderingData.mainLightIndex = mainLightIndex;

            // if (asset.additionalLightsRenderingMode != LightRenderingMode.Disabled) {
                int additionalLightsCount = (mainLightIndex != -1) ? visibleLights.Length - 1 : visibleLights.Length;
                renderingData.additionalLightsCount = Math.Min(additionalLightsCount, LunarLightManager.maxVisibleAdditionalLights);
                renderingData.maxPerObjectAdditionalLightsCount = Math.Min(int.MaxValue/* asset.maxAdditionalLightsCount */, LunarLightManager.maxPerObjectLights);
            // } else {
            //     renderingData.additionalLightsCount = 0;
            //     renderingData.maxPerObjectAdditionalLightsCount = 0;
            // }

            renderingData.supportsAdditionalLights = true/* asset.additionalLightsRenderingMode != LightRenderingMode.Disabled */;
            // renderingData.shadeAdditionalLightsPerVertex = false/* asset.additionalLightsRenderingMode == LightRenderingMode.PerVertex */;
            // renderingData.supportsMixedLighting = asset.supportsMixedLighting;
            // renderingData.reflectionProbeBlending = asset.reflectionProbeBlending;
            // renderingData.reflectionProbeBoxProjection = asset.reflectionProbeBoxProjection;
            renderingData.supportsLightLayers = SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2 /* && asset.useRenderingLayers */;


            // InitializeLightData(asset, visibleLights, mainLightIndex, out renderingData.lightData);
            // InitializeShadowData(asset, visibleLights, mainLightCastShadows, additionalLightsCastShadows && !renderingData.lightData.shadeAdditionalLightsPerVertex, out renderingData.shadowData);
            // InitializePostProcessingData(asset, out renderingData.postProcessingData);
            renderingData.supportsDynamicBatching = asset.useSRPBatcher;
            renderingData.supportsInstancing = asset.useInstancing;
            // var isForwardPlus = cameraData.renderer is UniversalRenderer { renderingModeActual: RenderingMode.ForwardPlus };
            // renderingData.perObjectData = GetPerObjectLightFlags(renderingData.lightData.additionalLightsCount, isForwardPlus);
            // renderingData.postProcessingEnabled = anyPostProcessingEnabled;
            renderingData.commandBuffer = cmd;

            // CheckAndApplyDebugSettings(ref renderingData);

            return renderingData;
        }
    }


}