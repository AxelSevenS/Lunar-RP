using UnityEngine;

namespace LunarRenderPipeline {

    /// <summary>
    ///     <para>
    ///         Injects the <c>BlitRenderPass</c> into the render pipeline.
    ///     </para>
    /// </summary>
    public sealed class BlitRendererFeature : LunarRendererFeature {

        private BlitRenderPass _blitRenderPass;

        [SerializeField] private LunarRenderPass.Event _event = LunarRenderPass.Event.AfterRendering;
        [SerializeField] private LunarRenderPass.Dependency _dependency = LunarRenderPass.Dependency.None;
        [SerializeField] private Material _blitMaterial;


        public override void Create() {
            if (_blitMaterial == null) {
                // Debug.LogError("BlitRendererFeature: Blit material is null.");
                return;
            }
            _blitRenderPass = new BlitRenderPass(_event, _dependency, _blitMaterial);
        }

        public override void AddRenderPasses(LunarRenderer renderer, ref RenderingData renderingData) {
            if (_blitRenderPass == null) {
                return;
            }
            renderer.EnqueuePass(_blitRenderPass);
        }
    }

}