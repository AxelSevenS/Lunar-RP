using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.LunarRenderPipeline {

    public sealed class SkyboxRenderPass : LunarRenderPass {

        private ProfilingSampler _skyboxSampler;

        public override Event renderPassEvent => Event.BeforeRenderingSkybox;
        public override Dependency dependencies => Dependency.None;

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            _skyboxSampler = new ProfilingSampler(nameof(SkyboxRenderPass));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            using (new ProfilingScope(renderingData.commandBuffer, _skyboxSampler)) {
                if (renderingData.cameraData.camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) {
                    context.DrawSkybox(renderingData.cameraData.camera);
                }
            }
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            // cmd.ReleaseTemporaryRT(LunarRenderPipeline.cameraColorTextureId);
            // cmd.ReleaseTemporaryRT(LunarRenderPipeline.cameraDepthTextureId);
        }
    }

}