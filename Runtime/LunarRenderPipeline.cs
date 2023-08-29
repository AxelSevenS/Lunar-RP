using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
#if UNITY_EDITOR
using UnityEditor;
// using UnityEditor.Rendering.Universal;
#endif


namespace Seven.LunarRenderPipeline {

    public class LunarRenderPipeline : RenderPipeline {

        public const string PIPELINE_NAME = nameof(LunarRenderPipeline); 


        public static readonly ShaderTagId forwardLightmodeId = new(name: "LunarForward");
        public static readonly ShaderTagId defferedLightmodeId = new(name: "LunarGBuffer");

        public static readonly ProfilingSampler drawGizmosProfilingSample = new($"{PIPELINE_NAME}.DrawGizmos");
        public static readonly ProfilingSampler setupCullingParametersProfilingSample = new($"{PIPELINE_NAME}.SetupCullingParameters");
        public static readonly ProfilingSampler cameraSetupProfilingSampler = new($"{PIPELINE_NAME}.CameraSetup");
        public static readonly ProfilingSampler configureProfilingSampler = new($"{PIPELINE_NAME}.Configure");
        public static readonly ProfilingSampler beginFrameRenderingProfilingSampler = new($"{PIPELINE_NAME}.BeginFrameRender");
        public static readonly ProfilingSampler endFrameRenderingProfilingSampler = new($"{PIPELINE_NAME}.EndFrameRender");
        public static readonly ProfilingSampler renderProfilingSampler = new($"{PIPELINE_NAME}.Render");
        public static readonly ProfilingSampler getMainLightIndexProfilingSampler = new($"{PIPELINE_NAME}.GetMainLightIndex");

        private LunarRenderer _renderer = null;
        public static readonly LunarLightManager lightManager = new LunarLightManager();

        private readonly LunarRenderPipelineAsset _asset;

        private readonly Material errorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");




        public LunarRenderPipeline(LunarRenderPipelineAsset asset) {
            _asset = asset;

            Shader.globalRenderPipeline = PIPELINE_NAME;

            #if UNITY_EDITOR
                SupportedRenderingFeatures.active = new SupportedRenderingFeatures() {
                    reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.None,
                    defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive,
                    mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive | SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask,
                    lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
                    lightmapsModes = LightmapsMode.CombinedDirectional | LightmapsMode.NonDirectional,
                    lightProbeProxyVolumes = false,
                    motionVectors = true,
                    receiveShadows = false,
                    reflectionProbes = false,
                    reflectionProbesBlendDistance = true,
                    particleSystemInstancing = true,
                    overridesEnableLODCrossFade = true
                };
                // SceneViewDrawMode.SetupDrawMode();
            #endif
            SupportedRenderingFeatures.active.supportsHDR = /* true */asset.supportsHDR;

            GraphicsSettings.useScriptableRenderPipelineBatching = /* true */asset.useSRPBatcher;

            // // In QualitySettings.antiAliasing disabled state uses value 0, where in URP 1
            // int qualitySettingsMsaaSampleCount = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
            // bool msaaSampleCountNeedsUpdate = qualitySettingsMsaaSampleCount != asset.msaaSampleCount;

            // // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            // if (msaaSampleCountNeedsUpdate) {
            //     QualitySettings.antiAliasing = asset.msaaSampleCount;
            // }


            // Configure initial XR settings
            // MSAASamples msaaSamples = (MSAASamples)Mathf.Clamp(Mathf.NextPowerOfTwo(QualitySettings.antiAliasing), (int)MSAASamples.None, (int)MSAASamples.MSAA8x);
            // XRSystem.SetDisplayMSAASamples(msaaSamples);
            // XRSystem.SetRenderScale(asset.renderScale);

            // Lightmapping.SetDelegate(lightsDelegate);
            CameraCaptureBridge.enabled = true;

            // RenderingUtils.ClearSystemInfoCache();

            // DecalProjector.defaultMaterial = asset.decalMaterial;

            // s_RenderGraph = new RenderGraph("LunarRenderGraph");

            DebugManager.instance.RefreshEditor();
            // m_DebugDisplaySettingsUI.RegisterDebug(UniversalRenderPipelineDebugDisplaySettings.Instance);

            QualitySettings.enableLODCrossFade = /* true */asset.enableLODCrossFade;
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            Shader.globalRenderPipeline = string.Empty;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
            // #if UNITY_EDITOR
            //     SceneViewDrawMode.ResetDrawMode();
            // #endif

            // Lightmapping.ResetDelegate();
            CameraCaptureBridge.enabled = false;

            // m_DebugDisplaySettingsUI.UnregisterDebug(UniversalRenderPipelineDebugDisplaySettings.Instance);

            // s_RenderGraph.Cleanup();
        }


        private void RenderCamera(ScriptableRenderContext context, Camera camera) {
            context.SetupCameraProperties(camera);

            if ( !camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters) ) {
                return;
            }
            CullingResults cullingResults = context.Cull(ref cullingParameters);

            CommandBuffer cmd = CommandBufferPool.Get("Render Loop");

            CameraClearFlags clearFlags = camera.clearFlags;
            cmd.ClearRenderTarget(
                (clearFlags & CameraClearFlags.Depth) != 0,
                (clearFlags & CameraClearFlags.Color) != 0,
                camera.backgroundColor
            );

            CameraData cameraData = CameraData.GetCameraData(camera);
            

            // Setup Lights
            RenderingData renderingData = RenderingData.GetRenderingData(_asset, ref cameraData, ref cullingResults, true, cmd);
            lightManager.ConfigureLights(cmd, ref renderingData);

            
            using (new ProfilingScope(cmd, renderProfilingSampler)) {

                using (new ProfilingScope(null, setupCullingParametersProfilingSample)) {
                    // _renderer.OnPreCullRenderPasses(in cameraData);
                    // _renderer.SetupCullingParameters(ref cullingParameters, ref cameraData);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                

                if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview) {
                    ScriptableRenderContext.EmitGeometryForCamera(camera);
                }
                #if UNITY_EDITOR
                    if (camera.cameraType == CameraType.SceneView) {
                        ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                    }
                #endif

                // SortingSettings sortingSettings = new(camera);
                // DrawingSettings drawingSettings = new(forwardLightmodeId, sortingSettings) {
                //     enableDynamicBatching = _asset.useSRPBatcher,
                //     enableInstancing = _asset.useInstancing
                // };

                // FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            
                // // Render opaque objects
                // sortingSettings.criteria = SortingCriteria.CommonOpaque;
                // filteringSettings.renderQueueRange = RenderQueueRange.opaque;
                // context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

                // // Render skybox if necessary
                // if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) {
                //     context.DrawSkybox(camera);
                // }

                // // Render transparent objects
                // sortingSettings.criteria = SortingCriteria.CommonTransparent;
                // filteringSettings.renderQueueRange = RenderQueueRange.transparent;
                // context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);


                _renderer.AddRenderPasses(ref renderingData);

                _renderer.SetupRenderPasses(context, ref renderingData);

                _renderer.Execute(context, ref renderingData);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            context.Submit();
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {

            // List<LunarRendererFeature> rendererFeatures = new List<LunarRendererFeature>() { 
            //     ScriptableObject.CreateInstance<OpaqueRendererFeature>(),
            //     ScriptableObject.CreateInstance<TransparentRendererFeature>(),
            //     ScriptableObject.CreateInstance<SkyboxRendererFeature>(),
            // };

            _renderer = new LunarRenderer(_asset.rendererFeatures);

            using (new ProfilingScope(null, beginFrameRenderingProfilingSampler)) {
                BeginFrameRendering(context, cameras);
            }

            for (int i = 0; i < cameras.Length; ++i) {
                Camera camera = cameras[i];

                BeginCameraRendering(context, camera);

                RenderCamera(context, camera);

                EndCameraRendering(context, camera);
            }

            using (new ProfilingScope(null, endFrameRenderingProfilingSampler)) {
                EndFrameRendering(context, cameras);
            }
        }
    }
}
