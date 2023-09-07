using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LunarRenderPipeline {

    public sealed class LunarRenderer {

        public static readonly int _timeId = Shader.PropertyToID("_Time");
        public static readonly int _sinTimeId = Shader.PropertyToID("_SinTime");
        public static readonly int _cosTimeId = Shader.PropertyToID("_CosTime");
        public static readonly int _deltaTimeId = Shader.PropertyToID("unity_DeltaTime");
        public static readonly int _timeParametersId = Shader.PropertyToID("_TimeParameters");
        
        private RTHandle _cameraColorTargetHandle;
        private RTHandle _cameraDepthTargetHandle;

        private readonly List<LunarRendererFeature> _rendererFeatures = new(); 
        private readonly List<LunarRenderPass> _renderPasses = new();


        public RTHandle cameraColorTarget => _cameraColorTargetHandle;
        public RTHandle cameraDepthTarget => _cameraDepthTargetHandle;

        /// <summary>
        /// Initializes all Renderer Features.
        /// </summary>
        public LunarRenderer(List<LunarRendererFeature> rendererFeatures) {

            for (int i = 0; i < rendererFeatures.Count; i++) {
                LunarRendererFeature feature = rendererFeatures[i];
                
                if (feature == null) {
                    continue;
                }

                feature.Create();
                _rendererFeatures.Add(feature);
            }
        }



        public void EnqueuePass(LunarRenderPass pass) {
            _renderPasses.Add(pass);
        }

        public void ConfigureTarget(RTHandle colorTargetHandle, RTHandle depthTargetHandle) {
            _cameraColorTargetHandle = colorTargetHandle;
            _cameraDepthTargetHandle = depthTargetHandle;
        }


        /// <summary>
        /// Gets all Render Passes from all Renderer Features.
        /// </summary>
        internal void AddRenderPasses(ref RenderingData renderingData) {
            // Add render passes from custom renderer features
            for (int i = 0; i < _rendererFeatures.Count; ++i) {
                if (!_rendererFeatures[i].isActive) {
                    continue;
                }

                _rendererFeatures[i].AddRenderPasses(this, ref renderingData);
            }

            // TODO: Add all Post-Proccessing Render Passes

            LunarRenderPass.Dependency passDependencies = LunarRenderPass.Dependency.None;
            for (int i = 0; i < _renderPasses.Count; i++) {
                passDependencies |= _renderPasses[i].dependencies;
            }
            ResolvePassDependencies(passDependencies);

            _renderPasses.Sort((a, b) => a.renderPassEvent.CompareTo(b.renderPassEvent));



            static void ResolvePassDependencies(LunarRenderPass.Dependency dependencies) {

                if ((dependencies & LunarRenderPass.Dependency.Depth) != 0) {
                    // _renderPasses.Add(new DepthPrePass());
                }

                if ((dependencies & LunarRenderPass.Dependency.Normal) != 0) {
                    // _renderPasses.Add(new NormalPrePass());
                }

                if ((dependencies & LunarRenderPass.Dependency.Color) != 0) {
                    // _renderPasses.Add(new ColorPrePass());
                }
            }
        }

        public void OnPreCullRenderPasses(in CameraData cameraData) {
            for (int i = 0; i < _renderPasses.Count; i++) {
                _renderPasses[i].OnCameraPreCull(this, in cameraData);
            }
        }
        
        public void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData, LunarRenderPipelineAsset asset) {
            // TODO: PerObjectCulling also affect reflection probes. Enabling it for now.
            // if (asset.additionalLightsRenderingMode == LightRenderingMode.Disabled ||
            //     asset.maxAdditionalLightsCount == 0)
            // if (renderingModeActual == RenderingMode.ForwardPlus)
            // {
            //     cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling;
            // }

            // We disable shadow casters if both shadow casting modes are turned off
            // or the shadow distance has been turned down to zero
            bool isShadowCastingDisabled = false/* !asset.supportsMainLightShadows && !asset.supportsAdditionalLightShadows */;
            bool isShadowDistanceZero = Mathf.Approximately(cameraData.maxShadowDistance, 0.0f);
            if (isShadowCastingDisabled || isShadowDistanceZero) {
                cullingParameters.cullingOptions &= ~CullingOptions.ShadowCasters;
            }

            // if (this.renderingModeActual == RenderingMode.Deferred)
            //     cullingParameters.maximumVisibleLights = 0xFFFF;
            // else
            {
                // We set the number of maximum visible lights allowed and we add one for the mainlight...
                //
                // Note: However ScriptableRenderContext.Cull() does not differentiate between light types.
                //       If there is no active main light in the scene, ScriptableRenderContext.Cull() might return  ( cullingParameters.maximumVisibleLights )  visible additional lights.
                //       i.e ScriptableRenderContext.Cull() might return  ( LunarLightManager.maxVisibleAdditionalLights + 1 )  visible additional lights !
                cullingParameters.maximumVisibleLights = LunarLightManager.maxVisibleAdditionalLights + 1;
            }
            cullingParameters.shadowDistance = cameraData.maxShadowDistance;

            cullingParameters.conservativeEnclosingSphere = asset.conservativeEnclosingSphere;

            cullingParameters.numIterationsEnclosingSphere = asset.numIterationsEnclosingSphere;
        }

        /// <summary>
        /// Executes all the Render Passes that have been queued up.
        /// </summary>
        public void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

            // Setup Passes
            // using (new ProfilingScope(null, LunarRenderPipeline.setupPassesProfilingSampler)) {
                for (int i = 0; i < _rendererFeatures.Count; i++) {
                    _rendererFeatures[i].SetupRenderPasses(this, renderingData);
                }
            // }
            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();


            // Setup Passes on Camera Setup
            using (new ProfilingScope(null, LunarRenderPipeline.cameraSetupProfilingSampler)) {
                for (int i = 0; i < _renderPasses.Count; ++i) {
                    _renderPasses[i].OnCameraSetup(renderingData.commandBuffer, ref renderingData);
                }
            }
            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();


            // Set Time Variables
            using (new ProfilingScope(null, LunarRenderPipeline.timeVariablesProfilingSampler)) {
                float time = Time.time;
                float deltaTime = Time.deltaTime;
                float smoothDeltaTime = Time.smoothDeltaTime;
                
                float timeEights = time / 8f;
                float timeFourth = time / 4f;
                float timeHalf = time / 2f;

                Vector4 timeVector = time * new Vector4(1f / 20f, 1f, 2f, 3f);
                Vector4 sinTimeVector = new(Mathf.Sin(timeEights), Mathf.Sin(timeFourth), Mathf.Sin(timeHalf), Mathf.Sin(time));
                Vector4 cosTimeVector = new(Mathf.Cos(timeEights), Mathf.Cos(timeFourth), Mathf.Cos(timeHalf), Mathf.Cos(time));
                Vector4 deltaTimeVector = new(deltaTime, 1f / deltaTime, smoothDeltaTime, 1f / smoothDeltaTime);
                Vector4 timeParametersVector = new(time, Mathf.Sin(time), Mathf.Cos(time), 0.0f);

                renderingData.commandBuffer.SetGlobalVector(_timeId, timeVector);
                renderingData.commandBuffer.SetGlobalVector(_sinTimeId, sinTimeVector);
                renderingData.commandBuffer.SetGlobalVector(_cosTimeId, cosTimeVector);
                renderingData.commandBuffer.SetGlobalVector(_deltaTimeId, deltaTimeVector);
                renderingData.commandBuffer.SetGlobalVector(_timeParametersId, timeParametersVector);
            }
            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();


            // Configure Passes
            using (new ProfilingScope(null, LunarRenderPipeline.configureProfilingSampler)) {
                for (int i = 0; i < _renderPasses.Count; ++i) {
                    _renderPasses[i].Configure(renderingData.commandBuffer, renderingData.cameraData.cameraTargetDescriptor);
                }
            }
            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();


            Dictionary<LunarRenderPass.Event, LunarRenderPass[]> segmented = CreatePassBlocks(
                LunarRenderPass.Event.BeforeRenderingPrePasses,
                LunarRenderPass.Event.AfterRenderingOpaques,
                LunarRenderPass.Event.AfterRenderingTransparents,
                LunarRenderPass.Event.AfterRendering
            );

            LunarRenderPass[] beforeRendering = segmented[LunarRenderPass.Event.BeforeRenderingPrePasses];
            LunarRenderPass[] opaquePasses = segmented[LunarRenderPass.Event.AfterRenderingOpaques];
            LunarRenderPass[] transparentPasses = segmented[LunarRenderPass.Event.AfterRenderingTransparents];
            LunarRenderPass[] afterRendering = segmented[LunarRenderPass.Event.AfterRendering];


            // Execute Passes
            using (new ProfilingScope(null, LunarRenderPipeline.executeRenderPassesProfilingSampler)) {
                ExecutePassBlock(beforeRendering, context, ref renderingData);

                ExecutePassBlock(opaquePasses, context, ref renderingData);
                ExecutePassBlock(transparentPasses, context, ref renderingData);

                DrawGizmos(context, renderingData.cameraData.camera, GizmoSubset.PreImageEffects, ref renderingData);

                ExecutePassBlock(afterRendering, context, ref renderingData);

                DrawWireOverlay(context, renderingData.cameraData.camera);
                DrawGizmos(context, renderingData.cameraData.camera, GizmoSubset.PostImageEffects, ref renderingData);
            }

            // Cleanup Passes
            using (new ProfilingScope(null, LunarRenderPipeline.cameraFinishProfilingSampler)) {
                for (int i = 0; i < _renderPasses.Count; ++i) {
                    _renderPasses[i].OnCameraCleanup(renderingData.commandBuffer);
                }
            }
            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();
        }


        [Conditional("UNITY_EDITOR")]
        void DrawGizmos(ScriptableRenderContext context, Camera camera, GizmoSubset gizmoSubset, ref RenderingData renderingData) {
            #if UNITY_EDITOR
                if (!Handles.ShouldRenderGizmos() || camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
                    return;

                CommandBuffer cmd = renderingData.commandBuffer;
                using (new ProfilingScope(cmd, LunarRenderPipeline.drawGizmosProfilingSampler)) {
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    context.DrawGizmos(camera, gizmoSubset);
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            #endif
        }

        [Conditional("UNITY_EDITOR")]
        void DrawWireOverlay(ScriptableRenderContext context, Camera camera) {
            context.DrawWireOverlay(camera);
        }

        /// <summary>
        /// Creates blocks out of the Render Passes.<br/>
        /// For each maximum Event given, a block will be created and added to the final Dictionary.<br/><br/>
        /// The first block will contain all render passes with an event lower than the first maximum.<br/>
        /// The following blocks will contain all render passes with an event higher than the previous maximum and lower than the current maximum.
        /// </summary>
        /// <param name="maximums"> The maximums are used to segment the render passes into different blocks. </param>
        /// <returns>
        /// A dictionary with the maximums as keys and the render passes as values.
        /// </returns>
        private Dictionary<LunarRenderPass.Event, LunarRenderPass[]> CreatePassBlocks( params LunarRenderPass.Event[] maximums ) {
            Dictionary<LunarRenderPass.Event, LunarRenderPass[]> renderPasses = new();

            int[] ranges = new int[maximums.Length + 1];

            int currRangeIndex = 0;
            int currRenderPass = 0;
            ranges[currRangeIndex++] = 0;

            // For each block, it finds the first render pass index that has an event
            // higher than the block maximum.
            for (int i = 0; i < maximums.Length - 1; ++i) {
                while (currRenderPass < _renderPasses.Count &&
                        _renderPasses[currRenderPass].renderPassEvent < maximums[i])
                    currRenderPass++;

                ranges[currRangeIndex++] = currRenderPass;
            }
            ranges[currRangeIndex] = _renderPasses.Count;


            for (int i = 0; i < maximums.Length; i++) {
                renderPasses[maximums[i]] = _renderPasses.GetRange(ranges[i], ranges[i + 1] - ranges[i]).ToArray();
            }

            return renderPasses;
        }

        private void ExecutePassBlock(LunarRenderPass[] block, ScriptableRenderContext context, ref RenderingData renderingData) {
            for (int i = 0; i < block.Length; i++) {
                LunarRenderPass renderPass = block[i];

                // Set the cmd RenderTarget to the renderPass' RenderTarget or the camera's RenderTarget if the renderPass doesn't have one.
                if (renderPass._useColorTarget || renderPass._useDepthTarget) {
                    RenderTargetIdentifier colorTargetHandle = renderPass._useColorTarget ? renderPass._colorTargetHandle : _cameraColorTargetHandle.nameID;
                    RenderTargetIdentifier depthTargetHandle = renderPass._useDepthTarget ? renderPass._depthTargetHandle : _cameraDepthTargetHandle.nameID;
                    ClearFlag clearFlag = renderPass._clearFlag;
                    Color clearColor = renderPass._clearColor;

                    CoreUtils.SetRenderTarget(renderingData.commandBuffer, colorTargetHandle, depthTargetHandle, clearFlag, clearColor);

                    context.ExecuteCommandBuffer(renderingData.commandBuffer);
                    renderingData.commandBuffer.Clear();
                }

                renderPass.Execute(context, ref renderingData);
                context.ExecuteCommandBuffer(renderingData.commandBuffer);
                renderingData.commandBuffer.Clear();

                // if (renderPass._useColorTarget || renderPass._useDepthTarget) {
                    CoreUtils.SetRenderTarget(renderingData.commandBuffer, _cameraColorTargetHandle.nameID, _cameraDepthTargetHandle.nameID, ClearFlag.None, Color.clear);
                    context.ExecuteCommandBuffer(renderingData.commandBuffer);
                    renderingData.commandBuffer.Clear();
                // }
            }
        }

    }
}