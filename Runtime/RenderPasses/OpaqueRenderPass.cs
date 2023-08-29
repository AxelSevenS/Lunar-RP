using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.LunarRenderPipeline {

    public sealed class OpaqueRenderPass : LunarRenderPass {

        private ProfilingSampler _opaqueSampler;

        public override Event renderPassEvent => Event.BeforeRenderingOpaques;
        public override Dependency dependencies => Dependency.None;

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            _opaqueSampler = new ProfilingSampler(nameof(OpaqueRenderPass));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            using (new ProfilingScope(renderingData.commandBuffer, _opaqueSampler)) {
                SortingSettings sortingSettings = new(renderingData.cameraData.camera);
                DrawingSettings drawingSettings = new(LunarRenderPipeline.forwardLightmodeId, sortingSettings) {
                    enableDynamicBatching = renderingData.supportsDynamicBatching,
                    enableInstancing = renderingData.supportsInstancing
                };

                FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            
                // Render opaque objects
                sortingSettings.criteria = SortingCriteria.CommonOpaque;
                filteringSettings.renderQueueRange = RenderQueueRange.opaque;
                context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings);
            }
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            // cmd.ReleaseTemporaryRT(LunarRenderPipeline.cameraColorTextureId);
            // cmd.ReleaseTemporaryRT(LunarRenderPipeline.cameraDepthTextureId);
        }
    }

}