using System.Runtime.InteropServices;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
// using UnityEditor.Rendering.Universal;
#endif

using UnityEngine.Rendering;

using Unity.Collections;
// using Unity.Jobs;
// using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;


namespace LunarRenderPipeline {

    public static class LunarLightManager {

        ///////////////////////////////////////////////////////
        // Shader Property IDs
        ///////////////////////////////////////////////////////

        public static readonly int _AdditionalLightsBufferId = Shader.PropertyToID("_AdditionalLightsBuffer");
        public static readonly int _AdditionalLightsIndicesId = Shader.PropertyToID("_AdditionalLightsIndices");

        public static readonly int _MainLightPositionId = Shader.PropertyToID("_MainLightPosition");
        public static readonly int _MainLightColorId = Shader.PropertyToID("_MainLightColor");
        public static readonly int _MainLightOcclusionProbesChannelId = Shader.PropertyToID("_MainLightOcclusionProbes");
        public static readonly int _MainLightLayerMaskId = Shader.PropertyToID("_MainLightLayerMask");

        public static readonly int _AdditionalLightsCountId = Shader.PropertyToID("_AdditionalLightsCount");
        public static readonly int _AdditionalLightsPositionId = Shader.PropertyToID("_AdditionalLightsPosition");
        public static readonly int _AdditionalLightsColorId = Shader.PropertyToID("_AdditionalLightsColor");
        public static readonly int _AdditionalLightsAttenuationId = Shader.PropertyToID("_AdditionalLightsAttenuation");
        public static readonly int _AdditionalLightsSpotDirId = Shader.PropertyToID("_AdditionalLightsSpotDir");
        public static readonly int _AdditionalLightOcclusionProbeChannelId = Shader.PropertyToID("_AdditionalLightsOcclusionProbes");
        public static readonly int _AdditionalLightsLayerMasksId = Shader.PropertyToID("_AdditionalLightsLayerMasks");

        public static readonly int _AmbientSkyColorId = Shader.PropertyToID("unity_AmbientSky");
        public static readonly int _AmbientEquatorColorId = Shader.PropertyToID("unity_AmbientEquator");
        public static readonly int _AmbientGroundColorId = Shader.PropertyToID("unity_AmbientGround");
        public static readonly int _SubtractiveShadowColorId = Shader.PropertyToID("_SubtractiveShadowColor");


        ///////////////////////////////////////////////////////
        // Shader Keywords
        ///////////////////////////////////////////////////////

        /// <summary> Keyword used for shadows without cascades. </summary>
        public const string MainLightShadows = "_MAIN_LIGHT_SHADOWS";

        /// <summary> Keyword used for shadows with cascades. </summary>
        public const string MainLightShadowCascades = "_MAIN_LIGHT_SHADOWS_CASCADE";

        /// <summary> Keyword used for screen space shadows. </summary>
        public const string MainLightShadowScreen = "_MAIN_LIGHT_SHADOWS_SCREEN";

        /// <summary> Keyword used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias. </summary>
        public const string CastingPunctualLightShadow = "_CASTING_PUNCTUAL_LIGHT_SHADOW";

        /// <summary> Keyword used for per pixel additional lights. </summary>
        public const string AdditionalLights = "_ADDITIONAL_LIGHTS";

        // /// <summary> Keyword used for Forward+. </summary>
        // internal const string ForwardPlus = "_FORWARD_PLUS";

        /// <summary> Keyword used for shadows on additional lights. </summary>
        public const string AdditionalLightShadows = "_ADDITIONAL_LIGHT_SHADOWS";

        /// <summary> Keyword used for Box Projection with Reflection Probes. </summary>
        public const string ReflectionProbeBoxProjection = "_REFLECTION_PROBE_BOX_PROJECTION";

        /// <summary> Keyword used for Reflection probe blending. </summary>
        public const string ReflectionProbeBlending = "_REFLECTION_PROBE_BLENDING";

        /// <summary> Keyword used for soft shadows. </summary>
        public const string SoftShadows = "_SHADOWS_SOFT";

        /// <summary> Keyword used for Mixed Lights in Subtractive lighting mode. </summary>
        public const string MixedLightingSubtractive = "_MIXED_LIGHTING_SUBTRACTIVE"; // Backward compatibility

        /// <summary> Keyword used for mixing lightmap shadows. </summary>
        public const string LightmapShadowMixing = "LIGHTMAP_SHADOW_MIXING";

        /// <summary> Keyword used for Shadowmask. </summary>
        public const string ShadowsShadowMask = "SHADOWS_SHADOWMASK";

        /// <summary> Keyword used for Light Layers. </summary>
        public const string LightLayers = "_LIGHT_LAYERS";

        /// <summary> Keyword used for RenderPass. </summary>
        public const string RenderPassEnabled = "_RENDER_PASS_ENABLED";

        /// <summary> Keyword used for Billboard cameras. </summary>
        public const string BillboardFaceCameraPos = "BILLBOARD_FACE_CAMERA_POS";

        /// <summary> Keyword used for Light Cookies. </summary>
        public const string LightCookies = "_LIGHT_COOKIES";

        /// <summary> Keyword used for no Multi Sampling Anti-Aliasing (MSAA). </summary>
        public const string DepthNoMsaa = "_DEPTH_NO_MSAA";

        /// <summary> Keyword used for Multi Sampling Anti-Aliasing (MSAA) with 2 per pixel sample count. </summary>
        public const string DepthMsaa2 = "_DEPTH_MSAA_2";

        /// <summary> Keyword used for Multi Sampling Anti-Aliasing (MSAA) with 4 per pixel sample count. </summary>
        public const string DepthMsaa4 = "_DEPTH_MSAA_4";

        /// <summary> Keyword used for Multi Sampling Anti-Aliasing (MSAA) with 8 per pixel sample count. </summary>
        public const string DepthMsaa8 = "_DEPTH_MSAA_8";

        /// <summary> Keyword used for Linear to SRGB conversions. </summary>
        public const string LinearToSRGBConversion = "_LINEAR_TO_SRGB_CONVERSION";

        /// <summary> Keyword used for less expensive Linear to SRGB conversions. </summary>
        internal const string UseFastSRGBLinearConversion = "_USE_FAST_SRGB_LINEAR_CONVERSION";

        /// <summary> Keyword used for first target in the DBuffer. </summary>
        public const string DBufferMRT1 = "_DBUFFER_MRT1";

        /// <summary> Keyword used for second target in the DBuffer. </summary>
        public const string DBufferMRT2 = "_DBUFFER_MRT2";

        /// <summary> Keyword used for third target in the DBuffer. </summary>
        public const string DBufferMRT3 = "_DBUFFER_MRT3";

        /// <summary> Keyword used for low quality normal reconstruction in Decals. </summary>
        public const string DecalNormalBlendLow = "_DECAL_NORMAL_BLEND_LOW";

        /// <summary> Keyword used for medium quality normal reconstruction in Decals. </summary>
        public const string DecalNormalBlendMedium = "_DECAL_NORMAL_BLEND_MEDIUM";

        /// <summary> Keyword used for high quality normal reconstruction in Decals. </summary>
        public const string DecalNormalBlendHigh = "_DECAL_NORMAL_BLEND_HIGH";

        /// <summary> Keyword used for Decal Layers. </summary>
        public const string DecalLayers = "_DECAL_LAYERS";

        /// <summary> Keyword used for writing Rendering Layers. </summary>
        public const string WriteRenderingLayers = "_WRITE_RENDERING_LAYERS";


        ///////////////////////////////////////////////////////
        // Buffers
        ///////////////////////////////////////////////////////

        private static ComputeBuffer _LightDataBuffer = null;
        private static ComputeBuffer _LightIndicesBuffer = null;

        // private static ComputeBuffer _AdditionalLightShadowParamsStructuredBuffer = null;
        // private static ComputeBuffer _AdditionalLightShadowSliceMatricesStructuredBuffer = null;

        private static Vector4[] _AdditionalLightPositions;
        private static Vector4[] _AdditionalLightColors;
        private static Vector4[] _AdditionalLightAttenuations;
        private static Vector4[] _AdditionalLightSpotDirections;
        private static Vector4[] _AdditionalLightOcclusionProbeChannels;
        private static float[] _AdditionalLightsLayerMasks;  // Unity has no support for binding uint arrays. We will use asuint() in the shader instead.


        ///////////////////////////////////////////////////////
        // Properties
        ///////////////////////////////////////////////////////

        internal static bool useStructuredBuffer {
            // There are some performance issues with StructuredBuffers in some platforms.
            // We fallback to UBO in those cases.
            get {
                // TODO: For now disabling SSBO until figure out Vulkan binding issues.
                // When enabling this also enable USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA in shader side in Input.hlsl
                return false;

                // We don't use SSBO in D3D because we can't figure out without adding shader variants if platforms is D3D10.
                //GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
                //return !Application.isMobilePlatform &&
                //    (deviceType == GraphicsDeviceType.Metal || deviceType == GraphicsDeviceType.Vulkan ||
                //     deviceType == GraphicsDeviceType.PlayStation4 || deviceType == GraphicsDeviceType.PlayStation5 || deviceType == GraphicsDeviceType.XboxOne);
            }
        }


        ///////////////////////////////////////////////////////
        // Fields
        ///////////////////////////////////////////////////////

        /// <summary>
        /// The max number of lights that can be shaded per object (in the for loop in the shader).
        /// </summary>
        public static readonly int maxPerObjectLights;

        /// <summary>
        /// The max number of additional lights that can can affect each GameObject.
        /// </summary>
        public static readonly int maxVisibleAdditionalLights;



        static LunarLightManager() {
            maxPerObjectLights = GetMaxPerObjectLights();
            maxVisibleAdditionalLights = GetMaxVisibleAdditionalLights();

            int GetMaxPerObjectLights() {
                return (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2) ? 4 : 8;
            }

            int GetMaxVisibleAdditionalLights() {

                // These limits have to match same limits in Input.hlsl
                const int k_MaxVisibleAdditionalLightsMobileShaderLevelLessThan45 = 16;
                const int k_MaxVisibleAdditionalLightsMobile = 32;
                const int k_MaxVisibleAdditionalLightsNonMobile = 256;

                bool isMobile = GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.SHADER_API_MOBILE);
                if (isMobile && (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && Graphics.minOpenGLESVersion <= OpenGLESVersion.OpenGLES30)))
                    return k_MaxVisibleAdditionalLightsMobileShaderLevelLessThan45;

                // GLES can be selected as platform on Windows (not a mobile platform) but uniform buffer size so we must use a low light count.
                return 
                    (isMobile || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                    ? k_MaxVisibleAdditionalLightsMobile 
                    : k_MaxVisibleAdditionalLightsNonMobile;
            }
        }


        public static void Setup() {
            if ( useStructuredBuffer ) {

                if (_LightDataBuffer.count != maxVisibleAdditionalLights) {
                    _LightDataBuffer?.Dispose();
                    _LightDataBuffer = null;
                }
                _LightDataBuffer ??= new ComputeBuffer(maxVisibleAdditionalLights, Marshal.SizeOf<LightData>());

            } else {

                _AdditionalLightPositions = new Vector4[maxVisibleAdditionalLights];
                _AdditionalLightColors = new Vector4[maxVisibleAdditionalLights];
                _AdditionalLightAttenuations = new Vector4[maxVisibleAdditionalLights];
                _AdditionalLightSpotDirections = new Vector4[maxVisibleAdditionalLights];
                _AdditionalLightOcclusionProbeChannels = new Vector4[maxVisibleAdditionalLights];
                _AdditionalLightsLayerMasks = new float[maxVisibleAdditionalLights];

            }

        }

        // Main Light is always a directional light
        public static int GetMainLightIndex(/* UniversalRenderPipelineAsset settings,  */NativeArray<VisibleLight> visibleLights) {
            using ProfilingScope profilingScope = new ProfilingScope(null, LunarRenderPipeline.getMainLightIndexProfilingSampler);

            int totalVisibleLights = visibleLights.Length;

            if (totalVisibleLights == 0)
                return -1;

            Light sunLight = RenderSettings.sun;
            int brightestDirectionalLightIndex = -1;
            float brightestLightIntensity = 0.0f;
            for (int i = 0; i < totalVisibleLights; ++i) {
                ref VisibleLight visibleLight = ref visibleLights.UnsafeElementAtMutable(i);
                Light currLight = visibleLight.light;

                // Particle system lights have the light property as null. We sort lights so all particles lights
                // come last. Therefore, if first light is particle light then all lights are particle lights.
                // In this case we either have no main light or already found it.
                if (currLight == null)
                    continue;

                if (visibleLight.lightType == LightType.Directional) {
                    // Sun source needs be a directional light
                    if (currLight == sunLight)
                        return i;

                    // In case no sun light is present we will return the brightest directional light
                    if (currLight.intensity > brightestLightIntensity) {
                        brightestLightIntensity = currLight.intensity;
                        brightestDirectionalLightIndex = i;
                    }
                }
            }

            return brightestDirectionalLightIndex;
        }


        public static void ConfigureMainLight(CommandBuffer cmd, ref RenderingData renderingData) {
            
            CullingResults cullingResults = renderingData.cullingResults;

            // Get main light
            LightData mainLightData = LightData.GetLightData(cullingResults.visibleLights, renderingData.mainLightIndex);

            cmd.SetGlobalVector(_MainLightPositionId, mainLightData.position);
            cmd.SetGlobalVector(_MainLightColorId, mainLightData.color);
            cmd.SetGlobalVector(_MainLightOcclusionProbesChannelId, mainLightData.occlusionProbeChannels);
            cmd.SetGlobalInt(_MainLightLayerMaskId, (int)mainLightData.layerMask);
        }

        public static void ConfigureAdditionalLights(CommandBuffer cmd, ref RenderingData renderingData) {
            
            CullingResults cullingResults = renderingData.cullingResults;
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            int totalLightsCount = visibleLights.Length;
            int additionalLightsCount = renderingData.additionalLightsCount;

            // TODO: Only do this once on Pipeline Startup.
            Setup();
                    
            if (additionalLightsCount > 0) {
                if ( useStructuredBuffer ) {

                    NativeArray<LightData> additionalLightsData = new NativeArray<LightData>(additionalLightsCount, Allocator.Temp);
                    for (int i = 0, lightIter = 0; i < totalLightsCount && lightIter < additionalLightsCount; ++i) {

                        if (renderingData.mainLightIndex == i) continue;

                        ref VisibleLight visibleLight = ref visibleLights.UnsafeElementAtMutable(i);
                        Light currLight = visibleLight.light;

                        if (currLight == null) continue;


                        LightData lightData = LightData.GetLightData(ref visibleLight);
                        
                        additionalLightsData[lightIter] = lightData;
                        lightIter++;
                    }
                    _LightDataBuffer.SetData(additionalLightsData);
                    additionalLightsData.Dispose();
                    
                    cmd.SetGlobalBuffer(_AdditionalLightsBufferId, _LightDataBuffer);


                    int lightIndices = cullingResults.lightAndReflectionProbeIndexCount;
                    if (_LightIndicesBuffer.count != lightIndices) {
                        _LightIndicesBuffer.Dispose();
                        _LightIndicesBuffer = null;
                    }
                    _LightIndicesBuffer ??= new ComputeBuffer(lightIndices, sizeof(int));
                    cmd.SetGlobalBuffer(_AdditionalLightsIndicesId, _LightIndicesBuffer);

                } else {

                    for (int i = 0, addLights = 0; i < totalLightsCount && addLights < additionalLightsCount; ++i) {

                        if (renderingData.mainLightIndex == i) continue;

                        ref VisibleLight visibleLight = ref visibleLights.UnsafeElementAtMutable(i);
                        Light currLight = visibleLight.light;

                        if (currLight == null) continue;


                        LightData lightData = LightData.GetLightData(ref visibleLight); 

                        _AdditionalLightPositions[addLights] = lightData.position;
                        _AdditionalLightColors[addLights] = lightData.color;
                        _AdditionalLightAttenuations[addLights] = lightData.attenuation;
                        _AdditionalLightSpotDirections[addLights] = lightData.spotDirection;
                        _AdditionalLightOcclusionProbeChannels[addLights] = lightData.occlusionProbeChannels;
                        _AdditionalLightsLayerMasks[addLights] = lightData.layerMask;

                        addLights++;
                    }

                    cmd.SetGlobalVectorArray(_AdditionalLightsPositionId, _AdditionalLightPositions);
                    cmd.SetGlobalVectorArray(_AdditionalLightsColorId, _AdditionalLightColors);
                    cmd.SetGlobalVectorArray(_AdditionalLightsAttenuationId, _AdditionalLightAttenuations);
                    cmd.SetGlobalVectorArray(_AdditionalLightsSpotDirId, _AdditionalLightSpotDirections);
                    cmd.SetGlobalVectorArray(_AdditionalLightOcclusionProbeChannelId, _AdditionalLightOcclusionProbeChannels);
                    cmd.SetGlobalFloatArray(_AdditionalLightsLayerMasksId, _AdditionalLightsLayerMasks);
                    
                }

                cmd.SetGlobalVector(_AdditionalLightsCountId, new Vector4(additionalLightsCount, 0, 0, 0));
            } else {
                cmd.SetGlobalVector(_AdditionalLightsCountId, Vector4.zero);
            }
        }


        public static void ConfigureLights(CommandBuffer cmd, ref RenderingData renderingData) {

            // Ambient
            cmd.SetGlobalVector(_AmbientSkyColorId, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientSkyColor));
            cmd.SetGlobalVector(_AmbientEquatorColorId, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientEquatorColor));
            cmd.SetGlobalVector(_AmbientGroundColorId, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientGroundColor));

            // Used when subtractive mode is selected
            cmd.SetGlobalVector(_SubtractiveShadowColorId, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));
            
                    
            ConfigureMainLight(cmd, ref renderingData);
            ConfigureAdditionalLights(cmd, ref renderingData);

            // bool useForwardPlus = false/* renderingData.useForwardPlus */;

            bool lightCountCheck = (/* renderingData.cameraData.renderer.stripAdditionalLightOffVariants && */ renderingData.supportsAdditionalLights) || renderingData.additionalLightsCount > 0;
            // CoreUtils.SetKeyword(cmd, AdditionalLightsVertex, lightCountCheck && additionalLightsPerVertex && !useForwardPlus);
            CoreUtils.SetKeyword(cmd, AdditionalLights, lightCountCheck);

            // bool isShadowMask = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.ShadowMask;
            // bool isShadowMaskAlways = isShadowMask && QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask;
            // bool isSubtractive = renderingData.lightData.supportsMixedLighting && m_MixedLightingSetup == MixedLightingSetup.Subtractive;
            // CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LightmapShadowMixing, isSubtractive || isShadowMaskAlways);
            // CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ShadowsShadowMask, isShadowMask);
            // CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive, isSubtractive); // Backward compatibility

            // CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ReflectionProbeBlending, renderingData.lightData.reflectionProbeBlending);
            // CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ReflectionProbeBoxProjection, renderingData.lightData.reflectionProbeBoxProjection);

        }

    }

    static class NativeArrayExtensions {
        /// <summary>
        /// IMPORTANT: Make sure you do not write to the value! There are no checks for this!
        /// </summary>
        public static unsafe ref T UnsafeElementAt<T>(this NativeArray<T> array, int index) where T : struct {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        public static unsafe ref T UnsafeElementAtMutable<T>(this NativeArray<T> array, int index) where T : struct {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }
    }
}