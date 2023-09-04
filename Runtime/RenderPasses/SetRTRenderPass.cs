using UnityEngine;
using UnityEngine.Rendering;

namespace LunarRenderPipeline {

    /// <summary>
    ///     <para>
    ///         Renders all blit objects in the scene.
    ///     </para>
    /// </summary>
    public sealed class SetRTRenderPass : LunarRenderPass {

        private ProfilingSampler _setRTSampler;

        public override Event renderPassEvent => _passEvent;
        public override Dependency dependencies => _dependency;

        private RTHandle _targetHandle;

        private readonly Event _passEvent;
        private readonly Dependency _dependency;

        public SetRTRenderPass(Event passEvent, Dependency dependency) {
            _passEvent = passEvent;
            _dependency = dependency;
        }


        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            _setRTSampler = new ProfilingSampler(nameof(SetRTRenderPass));
            _targetHandle = RTHandles.Alloc(cameraTextureDescriptor, name: "SetRTHandle");
                
            ConfigureTarget(_targetHandle, _targetHandle);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

            RTHandle source = renderingData.renderer.cameraColorTarget;

            using (new ProfilingScope(renderingData.commandBuffer, _setRTSampler)) {

                
            }

            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            RTHandles.Release(_targetHandle);

        }
    }

}