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


namespace LunarRenderPipeline {

    public class LunarRenderPipeline : RenderPipeline {

        public const string PIPELINE_NAME = nameof(LunarRenderPipeline); 


        public static readonly ShaderTagId forwardLightmodeId = new(name: "LunarForward");
        public static readonly ShaderTagId defferedLightmodeId = new(name: "LunarGBuffer");

        public static readonly ProfilingSampler getMainLightIndexProfilingSampler = new($"{PIPELINE_NAME}.GetMainLightIndex");
        public static readonly ProfilingSampler beginFrameRenderingProfilingSampler = new($"{PIPELINE_NAME}.BeginFrameRender");
        public static readonly ProfilingSampler setupCullingParametersProfilingSampler = new($"{PIPELINE_NAME}.SetupCullingParameters");
        public static readonly ProfilingSampler renderProfilingSampler = new($"{PIPELINE_NAME}.Render");
        public static readonly ProfilingSampler cameraSetupProfilingSampler = new($"{PIPELINE_NAME}.CameraSetup");
        public static readonly ProfilingSampler configureProfilingSampler = new($"{PIPELINE_NAME}.Configure");
        public static readonly ProfilingSampler timeVariablesProfilingSampler = new($"{PIPELINE_NAME}.TimeVariables");
        public static readonly ProfilingSampler drawGizmosProfilingSampler = new($"{PIPELINE_NAME}.DrawGizmos");
        public static readonly ProfilingSampler executeRenderPassesProfilingSampler = new($"{PIPELINE_NAME}.ExecutePasses");
        public static readonly ProfilingSampler cameraFinishProfilingSampler = new($"{PIPELINE_NAME}.CameraFinish");
        public static readonly ProfilingSampler endFrameRenderingProfilingSampler = new($"{PIPELINE_NAME}.EndFrameRender");


        public static readonly RTHandle _cameraTargetHandle = RTHandles.Alloc(BuiltinRenderTextureType.CameraTarget);


        private LunarRenderer _renderer = null;

        private readonly LunarRenderPipelineAsset _asset;

        // private readonly Material errorMaterial = CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader");




        public LunarRenderPipeline(LunarRenderPipelineAsset asset) {
            _asset = asset;

            Shader.globalRenderPipeline = PIPELINE_NAME;

            // #if UNITY_EDITOR
            //     SupportedRenderingFeatures.active = new SupportedRenderingFeatures() {
            //         reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.None,
            //         defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive,
            //         mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive | SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask,
            //         lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed | LightmapBakeType.Realtime,
            //         lightmapsModes = LightmapsMode.CombinedDirectional | LightmapsMode.NonDirectional,
            //         lightProbeProxyVolumes = false,
            //         motionVectors = true,
            //         receiveShadows = true/* false */,
            //         reflectionProbes = false,
            //         reflectionProbesBlendDistance = true,
            //         particleSystemInstancing = true,
            //         overridesEnableLODCrossFade = true
            //     };
            //     // SceneViewDrawMode.SetupDrawMode();
            // #endif

            // Initial state of the RTHandle system.
            // We initialize to screen width/height to avoid multiple realloc that can lead to inflated memory usage (as releasing of memory is delayed).
            RTHandles.Initialize(Screen.width, Screen.height);

            SupportedRenderingFeatures.active.supportsHDR = asset.supportsHDR;
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;

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
            // CameraCaptureBridge.enabled = true;

            // RenderingUtils.ClearSystemInfoCache();

            // DecalProjector.defaultMaterial = asset.decalMaterial;

            // s_RenderGraph = new RenderGraph("LunarRenderGraph");

            DebugManager.instance.RefreshEditor();
            // m_DebugDisplaySettingsUI.RegisterDebug(UniversalRenderPipelineDebugDisplaySettings.Instance);

            QualitySettings.enableLODCrossFade = asset.enableLODCrossFade;
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            Shader.globalRenderPipeline = string.Empty;

            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
            // #if UNITY_EDITOR
            //     SceneViewDrawMode.ResetDrawMode();
            // #endif

            // Lightmapping.ResetDelegate();
            // CameraCaptureBridge.enabled = false;

            // m_DebugDisplaySettingsUI.UnregisterDebug(UniversalRenderPipelineDebugDisplaySettings.Instance);

            // s_RenderGraph.Cleanup();
        }


        private void RenderCamera(ScriptableRenderContext context, Camera camera) {
            CommandBuffer cmd = CommandBufferPool.Get("Render Loop");


            context.SetupCameraProperties(camera);

            GetCameraClearParameters(camera, out bool clearDepth, out bool clearColor, out Color backgroundColor);
            cmd.ClearRenderTarget( clearDepth, clearColor, backgroundColor );
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();


            // Culling
            if ( !camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters) ) {
                return;
            }
            CullingResults cullingResults = context.Cull(ref cullingParameters);

            
            // Setup Lights
            CameraData cameraData = CameraData.GetCameraData(_asset, camera);
            RenderingData renderingData = RenderingData.GetRenderingData(_asset, _renderer, ref cameraData, ref cullingResults, true, cmd);
            LunarLightManager.ConfigureLights(cmd, ref renderingData);

            
            using (new ProfilingScope(cmd, renderProfilingSampler)) {

                using (new ProfilingScope(null, setupCullingParametersProfilingSampler)) {
                    _renderer.OnPreCullRenderPasses(in cameraData);
                    _renderer.SetupCullingParameters(ref cullingParameters, ref cameraData, _asset);
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

                _renderer.AddRenderPasses(ref renderingData);

                _renderer.ConfigureTarget(_cameraTargetHandle, _cameraTargetHandle);

                _renderer.Execute(context, ref renderingData);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            context.Submit();
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {

            _renderer = new LunarRenderer(_asset.rendererFeatures);
            RTHandles.Initialize(Screen.width, Screen.height);

            using (new ProfilingScope(null, beginFrameRenderingProfilingSampler)) {
                BeginFrameRendering(context, cameras);
            }

            GraphicsSettings.lightsUseLinearIntensity = QualitySettings.activeColorSpace == ColorSpace.Linear;
            GraphicsSettings.lightsUseColorTemperature = true;
            GraphicsSettings.defaultRenderingLayerMask = 0x00000001; // TODO : Make this a variable

            for (int i = 0; i < cameras.Length; ++i) {
                Camera camera = cameras[i];

                // if ( camera.cameraType == CameraType.Game ) {

                //     RenderCamera(context, camera);

                // } else {

                    BeginCameraRendering(context, camera);

                    RenderCamera(context, camera);

                    EndCameraRendering(context, camera);

                // }

            }

            using (new ProfilingScope(null, endFrameRenderingProfilingSampler)) {
                EndFrameRendering(context, cameras);
            }
        }

        public void GetCameraClearParameters(Camera camera, out bool clearDepth, out bool clearColor, out Color backgroundColor) {
            CameraClearFlags clearFlags = camera.clearFlags;
            clearDepth = (clearFlags & CameraClearFlags.Depth) != 0;
            clearColor = (clearFlags & CameraClearFlags.Color) != 0;
            backgroundColor = camera.backgroundColor;
        }
    }
}
