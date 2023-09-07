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

        private readonly static string _blitTexName = "_BlitTex";
        private readonly static int _blitTexID = Shader.PropertyToID(_blitTexName);
        private RTHandle _blitHandle;
        // private RenderTargetIdentifier _blitTexId = new(_blitTexName);


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
            
            _blitHandle = RTHandles.Alloc(_blitTexID, name: _blitTexName);
            cmd.GetTemporaryRT(_blitTexID, cameraTextureDescriptor);

        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

            RTHandle source = renderingData.renderer.cameraColorTarget;

            using (new ProfilingScope(renderingData.commandBuffer, _blitSampler)) {
                
                renderingData.commandBuffer.Blit(source.nameID, _blitHandle, _blitMaterial, 0);
                renderingData.commandBuffer.Blit(_blitHandle.nameID, source.nameID);
            }

            context.ExecuteCommandBuffer(renderingData.commandBuffer);
            renderingData.commandBuffer.Clear();
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            _blitHandle.Release();
            cmd.ReleaseTemporaryRT(_blitTexID);

        }
    }

}