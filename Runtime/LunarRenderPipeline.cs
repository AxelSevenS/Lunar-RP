using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering.Universal;
#endif


namespace Seven.LunarRenderPipeline {

    public class LunarRenderPipeline : RenderPipeline {

        public const string PIPELINE_NAME = nameof(LunarRenderPipeline); 


        public static readonly ShaderTagId forwardLightmodeId = new(name: "LunarForward");
        public static readonly ShaderTagId defferedLightmodeId = new(name: "LunarGBuffer");

        public static readonly ProfilingSampler renderProfilingSampler = new($"{PIPELINE_NAME}.Render");
        public static readonly ProfilingSampler getMainLightIndexProfilingSampler = new($"{PIPELINE_NAME}.GetMainLightIndex");

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

            CameraClearFlags clearFlags = camera.clearFlags;
            CommandBuffer cmd = CommandBufferPool.Get("Render Loop");

            cmd.ClearRenderTarget(
                (clearFlags & CameraClearFlags.Depth) != 0,
                (clearFlags & CameraClearFlags.Color) != 0,
                camera.backgroundColor
            );

            CameraData cameraData = CameraData.GetCameraData(camera);

            if ( !camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters) ) {
                return;
            }
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            

            cmd.SetGlobalVector(LunarLightManager._UnityLightDataId, new Vector4(1.0f, 1.0f, 1.0f, 0f)); // Doesn't work for some reason

            // Setup Lights
            RenderingData renderingData = RenderingData.GetRenderingData(_asset, ref cameraData, ref cullingResults, true, cmd);
            lightManager.ConfigureLights(cmd, ref renderingData);

            cmd.SetGlobalVector(LunarLightManager._UnityLightDataId, new Vector4(1.0f, 1.0f, 1.0f, 0f)); // Doesn't work for some reason
            
            using (new ProfilingScope(cmd, renderProfilingSampler)) {

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

                SortingSettings sortingSettings = new(camera);
                DrawingSettings drawingSettings = new(forwardLightmodeId, sortingSettings);
                drawingSettings.enableDynamicBatching = _asset.useSRPBatcher;
                drawingSettings.enableInstancing = _asset.useDynamicBatching;

                FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            cmd.SetGlobalVector(LunarLightManager._UnityLightDataId, new Vector4(1.0f, 1.0f, 1.0f, 0f)); // Doesn't work for some reason
            
                // Render opaque objects
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                filteringSettings.renderQueueRange = RenderQueueRange.opaque;
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

                // Render skybox if necessary
                if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) {
                    context.DrawSkybox(camera);
                }

                // Render transparent objects
                sortingSettings.criteria = SortingCriteria.CommonTransparent;
                filteringSettings.renderQueueRange = RenderQueueRange.transparent;
                context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            context.Submit();
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
            // #if UNITY_2021_1_OR_NEWER
            //     // using (new ProfilingScope(null, Profiling.Pipeline.beginContextRendering))
            //     // {
            //         BeginContextRendering(renderContext, cameras);
            //     // }
            // #else
            //     // using (new ProfilingScope(null, Profiling.Pipeline.beginFrameRendering))
            //     // {
            //         BeginFrameRendering(renderContext, cameras);
            //     // }
            // #endif

            for (int i = 0; i < cameras.Length; ++i) {
                Camera camera = cameras[i];

                BeginCameraRendering(context, camera);

                RenderCamera(context, camera);

                EndCameraRendering(context, camera);
            }
        }
    }
}
