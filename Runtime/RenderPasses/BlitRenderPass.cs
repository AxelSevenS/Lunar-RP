using UnityEngine;
using UnityEngine.Rendering;

namespace LunarRenderPipeline {

    /// <summary>
    ///     <para>
    ///         Renders all blit objects in the scene.
    ///     </para>
    /// </summary>
    public sealed class BlitRenderPass : LunarRenderPass {

        private ProfilingSampler _blitSampler;

        public override Event renderPassEvent => _passEvent;
        public override Dependency dependencies => _dependency;

        // private readonly static int _blitTex = Shader.PropertyToID("_BlitTex");
        // private RenderTargetIdentifier _blitTexId = new(_blitTex);
        private RTHandle _blitHandle;


        private readonly Event _passEvent;
        private readonly Dependency _dependency;
        private readonly Material _blitMaterial;

        public BlitRenderPass(Event passEvent, Dependency dependency, Material blitMaterial) {
            _passEvent = passEvent;
            _dependency = dependency;
            _blitMaterial = blitMaterial;
        }


        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            _blitSampler = new ProfilingSampler(nameof(BlitRenderPass));
            _blitHandle = RTHandles.Alloc(cameraTextureDescriptor, name: "BlitHandle");
            // cmd.GetTemporaryRT(_blitTex, cameraTextureDescriptor);

        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

            RTHandle source = renderingData.renderer.cameraColorTarget;

            using (new ProfilingScope(renderingData.commandBuffer, _blitSampler)) {
                
                Blitter.BlitCameraTexture(renderingData.commandBuffer, source, _blitHandle, _blitMaterial, 0);
            }

            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            RTHandles.Release(_blitHandle);
            // cmd.ReleaseTemporaryRT(_blitTex);

        }
    }

}