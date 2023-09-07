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


        private static readonly string _targetHandleName = "_SetRTBuffer";
        private static readonly int _targetHandleID = Shader.PropertyToID(_targetHandleName);
        private RTHandle _targetHandle;
        // private static RenderTargetIdentifier _targetIdentifier = new(_targetHandleID);

        private readonly Event _passEvent;
        private readonly Dependency _dependency;

        public SetRTRenderPass(Event passEvent, Dependency dependency) {
            _passEvent = passEvent;
            _dependency = dependency;
        }


        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            _setRTSampler = new ProfilingSampler(nameof(SetRTRenderPass));

            _targetHandle = RTHandles.Alloc(_targetHandleID, name: _targetHandleName);
            cmd.GetTemporaryRT(_targetHandleID, cameraTextureDescriptor);
                
            ConfigureTarget(_targetHandle, _targetHandle);
            ConfigureClear(ClearFlag.All, Color.clear);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

            CommandBuffer buffer = CommandBufferPool.Get("SetRTBuffer");

            using (new ProfilingScope(buffer, _setRTSampler)) {

                SortingSettings sortingSettings = new(renderingData.cameraData.camera) {
                    criteria = SortingCriteria.CommonOpaque
                };
                DrawingSettings drawingSettings = new(LunarRenderPipeline.forwardLightmodeId, sortingSettings);
                FilteringSettings filteringSettings = new(RenderQueueRange.opaque);

                context.DrawRenderers(renderingData.cullingResults, ref drawingSettings, ref filteringSettings);


                buffer.Blit(_targetHandle.nameID, renderingData.renderer.cameraColorTarget);
                // Blitter.BlitCameraTexture(buffer, _targetHandle, renderingData.renderer.cameraColorTarget/* , null, 0 */);

            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd) {
            _targetHandle.Release();
            cmd.ReleaseTemporaryRT(_targetHandleID);
        }
    }

}