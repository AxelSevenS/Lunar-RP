using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.LunarRenderPipeline {

    public sealed class TransparentRenderPass : LunarRenderPass {

        private ProfilingSampler _transparentSampler;

        public override Event renderPassEvent => Event.BeforeRenderingTransparents;
        public override Dependency dependencies => Dependency.None;

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            _transparentSampler = new ProfilingSampler(nameof(OpaqueRenderPass));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            using (new ProfilingScope(renderingData.commandBuffer, _transparentSampler)) {
                SortingSettings sortingSettings = new(renderingData.cameraData.camera);
                DrawingSettings drawingSettings = new(LunarRenderPipeline.forwardLightmodeId, sortingSettings) {
                    enableDynamicBatching = renderingData.supportsDynamicBatching,
                    enableInstancing = renderingData.supportsInstancing
                };

                FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            
                // Render opaque objects
                sortingSettings.criteria = SortingCriteria.CommonTransparent;
                filteringSettings.renderQueueRange = RenderQueueRange.transparent;
                context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings);
            }
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            // cmd.ReleaseTemporaryRT(LunarRenderPipeline.cameraColorTextureId);
            // cmd.ReleaseTemporaryRT(LunarRenderPipeline.cameraDepthTextureId);
        }

    }

}