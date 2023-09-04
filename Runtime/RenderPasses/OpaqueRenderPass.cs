using UnityEngine;
using UnityEngine.Rendering;

namespace LunarRenderPipeline {

    /// <summary>
    ///     <para>
    ///         Renders all opaque objects in the scene.
    ///     </para>
    /// </summary>
    public sealed class OpaqueRenderPass : LunarRenderPass {

        private ProfilingSampler _opaqueSampler;

        // private readonly int _testHandleId = Shader.PropertyToID("_TestHandle");
        // private RTHandle _testHandle;

        public override Event renderPassEvent => Event.BeforeRenderingOpaques;
        public override Dependency dependencies => Dependency.None;


        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
            // _testHandle = RTHandles.Alloc(renderingData.cameraData.cameraTargetDescriptor, name: "_TestHandle");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            _opaqueSampler = new ProfilingSampler(nameof(OpaqueRenderPass));
            // ConfigureTarget(_testHandle, _testHandle);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            using (new ProfilingScope(renderingData.commandBuffer, _opaqueSampler)) {

                SortingSettings sortingSettings = new(renderingData.cameraData.camera) {
                    criteria = SortingCriteria.CommonOpaque
                };

                DrawingSettings drawingSettings = new(LunarRenderPipeline.forwardLightmodeId, sortingSettings) {
                    enableDynamicBatching = renderingData.supportsDynamicBatching,
                    enableInstancing = renderingData.supportsInstancing
                };

                FilteringSettings filteringSettings = FilteringSettings.defaultValue;
                filteringSettings.renderQueueRange = RenderQueueRange.opaque;
            
                // Render opaque objects
                context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings);
                
                // renderingData.commandBuffer.SetGlobalTexture(_testHandleId, _testHandle.nameID);

                context.ExecuteCommandBuffer(renderingData.commandBuffer);
                renderingData.commandBuffer.Clear();

                // renderingData.commandBuffer.Blit(_testHandle, LunarRenderPipeline._cameraTargetHandle);
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            // RTHandles.Release(_testHandle);
        }
    }

}