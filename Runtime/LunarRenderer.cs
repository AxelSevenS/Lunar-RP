using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Seven.LunarRenderPipeline {

    public sealed class LunarRenderer {

        private List<LunarRendererFeature> _rendererFeatures = new(); 
        private List<LunarRenderPass> _renderPasses = new();

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

        internal void ResolvePassDependencies(LunarRenderPass.Dependency dependencies) {

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

        [Conditional("UNITY_EDITOR")]
        void DrawGizmos(ScriptableRenderContext context, Camera camera, GizmoSubset gizmoSubset, ref RenderingData renderingData) {
            #if UNITY_EDITOR
                if (!Handles.ShouldRenderGizmos() || camera.sceneViewFilterMode == Camera.SceneViewFilterMode.ShowFiltered)
                    return;

                CommandBuffer cmd = renderingData.commandBuffer;
                using (new ProfilingScope(cmd, LunarRenderPipeline.drawGizmosProfilingSample)) {
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

        // internal void SegmentRenderPasses(
        //     out LunarRenderPass[] beforeRendering,
        //     out LunarRenderPass[] opaquePasses,
        //     // out LunarRenderPass[] skyboxPasses,
        //     out LunarRenderPass[] transparentPasses,
        //     out LunarRenderPass[] afterRendering
        // ) {
        //     int[] limits = new int[4] {
        //         (int)LunarRenderPass.Event.BeforeRenderingPrePasses,
        //         (int)LunarRenderPass.Event.AfterRenderingOpaques,
        //         // (int)LunarRenderPass.Event.AfterRenderingSkybox,
        //         (int)LunarRenderPass.Event.AfterRenderingTransparents,
        //         (int)LunarRenderPass.Event.AfterRenderingPostProcessing
        //     };

        //     int[] ranges = new int[5]{0, 0, 0, 0, 0};

        //     int currRangeIndex = 0;
        //     int currRenderPass = 0;
        //     ranges[currRangeIndex++] = 0;

        //     // For each block, it finds the first render pass index that has an event
        //     // higher than the block limit.
        //     for (int i = 0; i < limits.Length - 1; ++i) {
        //         while (currRenderPass < _renderPasses.Count &&
        //                 (int)_renderPasses[currRenderPass].renderPassEvent < limits[i])
        //             currRenderPass++;

        //         ranges[currRangeIndex++] = currRenderPass;
        //     }
        //     ranges[currRangeIndex] = _renderPasses.Count;


        //     beforeRendering = _renderPasses.GetRange(ranges[0], ranges[1] - ranges[0]).ToArray();
        //     opaquePasses = _renderPasses.GetRange(ranges[1], ranges[2] - ranges[1]).ToArray();
        //     // skyboxPasses = _renderPasses.GetRange(ranges[2], ranges[3] - ranges[2]).ToArray();
        //     transparentPasses = _renderPasses.GetRange(ranges[2], ranges[3] - ranges[2]).ToArray();
        //     afterRendering = _renderPasses.GetRange(ranges[3], ranges[4] - ranges[3]).ToArray();
        // }

        internal Dictionary<LunarRenderPass.Event, LunarRenderPass[]> SegmentRenderPasses( LunarRenderPass.Event[] limits ) {
            Dictionary<LunarRenderPass.Event, LunarRenderPass[]> renderPasses = new();

            int[] ranges = new int[5]{0, 0, 0, 0, 0};

            int currRangeIndex = 0;
            int currRenderPass = 0;
            ranges[currRangeIndex++] = 0;

            // For each block, it finds the first render pass index that has an event
            // higher than the block limit.
            for (int i = 0; i < limits.Length - 1; ++i) {
                while (currRenderPass < _renderPasses.Count &&
                        _renderPasses[currRenderPass].renderPassEvent < limits[i])
                    currRenderPass++;

                ranges[currRangeIndex++] = currRenderPass;
            }
            ranges[currRangeIndex] = _renderPasses.Count;


            for (int i = 0; i < limits.Length; i++) {
                renderPasses[limits[i]] = _renderPasses.GetRange(ranges[i], ranges[i + 1] - ranges[i]).ToArray();
            }

            return renderPasses;
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

            LunarRenderPass.Dependency passDependencies = LunarRenderPass.Dependency.None;
            for (int i = 0; i < _renderPasses.Count; i++) {
                passDependencies |= _renderPasses[i].dependencies;
            }
            ResolvePassDependencies(passDependencies);


            _renderPasses.Sort((a, b) => a.renderPassEvent.CompareTo(b.renderPassEvent));
        }

        /// <summary>
        /// Sets up all the Render Passes after they have been added.
        /// </summary>
        internal void SetupRenderPasses(ScriptableRenderContext context, ref RenderingData renderingData) {
            for (int i = 0; i < _rendererFeatures.Count; i++) {
                _rendererFeatures[i].SetupRenderPasses(this, renderingData);
            }
            
            // Opaque;
            // Skybox;
            // Transparents;
        }

        /// <summary>
        /// Executes all the Render Passes that have been queued up.
        /// </summary>
        internal void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

            using (new ProfilingScope(null, LunarRenderPipeline.cameraSetupProfilingSampler)) {
                for (int i = 0; i < _renderPasses.Count; ++i) {
                    _renderPasses[i].OnCameraSetup(renderingData.commandBuffer, ref renderingData);
                }
                context.ExecuteCommandBuffer(renderingData.commandBuffer);
                renderingData.commandBuffer.Clear();
            }

            using (new ProfilingScope(null, LunarRenderPipeline.configureProfilingSampler)) {
                for (int i = 0; i < _renderPasses.Count; ++i) {
                    // _renderPasses[i].Configure(renderingData.commandBuffer, renderingData.cameraData.cameraTargetDescriptor);
                }

                context.ExecuteCommandBuffer(renderingData.commandBuffer);
                renderingData.commandBuffer.Clear();
            }

            
            LunarRenderPass.Event[] limits = new LunarRenderPass.Event[4] {
                LunarRenderPass.Event.BeforeRenderingPrePasses,
                LunarRenderPass.Event.AfterRenderingOpaques,
                LunarRenderPass.Event.AfterRenderingTransparents,
                LunarRenderPass.Event.AfterRenderingPostProcessing
            };
            Dictionary<LunarRenderPass.Event, LunarRenderPass[]> segmented = SegmentRenderPasses(limits);


            LunarRenderPass[] beforeRendering = segmented[LunarRenderPass.Event.BeforeRenderingPrePasses];
            for (int i = 0; i < beforeRendering.Length; i++) {
                LunarRenderPass renderPass = beforeRendering[i];
                renderPass.Execute(context, ref renderingData);
            }

            LunarRenderPass[] opaquePasses = segmented[LunarRenderPass.Event.AfterRenderingOpaques];
            for (int i = 0; i < opaquePasses.Length; i++) {
                LunarRenderPass renderPass = opaquePasses[i];
                renderPass.Execute(context, ref renderingData);
            }

            LunarRenderPass[] transparentPasses = segmented[LunarRenderPass.Event.AfterRenderingTransparents];
            for (int i = 0; i < transparentPasses.Length; i++) {
                LunarRenderPass renderPass = transparentPasses[i];
                renderPass.Execute(context, ref renderingData);
            }

            DrawGizmos(context, renderingData.cameraData.camera, GizmoSubset.PreImageEffects, ref renderingData);

            LunarRenderPass[] afterRendering = segmented[LunarRenderPass.Event.AfterRenderingPostProcessing];
            for (int i = 0; i < afterRendering.Length; i++) {
                LunarRenderPass renderPass = afterRendering[i];
                renderPass.Execute(context, ref renderingData);
            }

            DrawWireOverlay(context, renderingData.cameraData.camera);

            DrawGizmos(context, renderingData.cameraData.camera, GizmoSubset.PostImageEffects, ref renderingData);
        }



    }
}