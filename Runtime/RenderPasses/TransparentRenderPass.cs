using UnityEngine;
using UnityEngine.Rendering;

namespace LunarRenderPipeline {

    /// <summary>
    ///     <para>
    ///         Renders all transparent objects in the scene.
    ///     </para>
    /// </summary>
    public sealed class TransparentRenderPass : LunarRenderPass {

        private ProfilingSampler _transparentSampler;

        public override Event renderPassEvent => Event.BeforeRenderingTransparents;
        public override Dependency dependencies => Dependency.None;

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            _transparentSampler = new ProfilingSampler(nameof(OpaqueRenderPass));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            using (new ProfilingScope(renderingData.commandBuffer, _transparentSampler)) {
                
                SortingSettings sortingSettings = new(renderingData.cameraData.camera) {
                    criteria = SortingCriteria.CommonTransparent
                };

                DrawingSettings drawingSettings = new(LunarRenderPipeline.forwardLightmodeId, sortingSettings) {
                    enableDynamicBatching = renderingData.supportsDynamicBatching,
                    enableInstancing = renderingData.supportsInstancing
                };

                FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            
                // Render opaque objects
                context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings);
            }
        }

    }

}