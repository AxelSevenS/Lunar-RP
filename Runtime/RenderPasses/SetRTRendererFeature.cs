using UnityEngine;

namespace LunarRenderPipeline {

    /// <summary>
    ///     <para>
    ///         Injects the <c>SetRTRenderPass</c> into the render pipeline.
    ///     </para>
    /// </summary>
    public sealed class SetRTRendererFeature : LunarRendererFeature {

        private SetRTRenderPass _setRTRenderPass;
        [SerializeField] private LunarRenderPass.Event _event = LunarRenderPass.Event.AfterRendering;
        [SerializeField] private LunarRenderPass.Dependency _dependency = LunarRenderPass.Dependency.None;


        public override void Create() {
            _setRTRenderPass = new SetRTRenderPass(_event, _dependency);
        }

        public override void AddRenderPasses(LunarRenderer renderer, ref RenderingData renderingData) {
            if (_setRTRenderPass == null) {
                return;
            }
            renderer.EnqueuePass(_setRTRenderPass);
        }
    }

}