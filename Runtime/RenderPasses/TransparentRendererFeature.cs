namespace LunarRenderPipeline {

    /// <summary>
    ///     <para>
    ///         Injects the <c>TransparentRenderPass</c> into the render pipeline.
    ///     </para>
    /// </summary>
    [DisallowMultipleRendererFeature]
    public sealed class TransparentRendererFeature : LunarRendererFeature {

        private TransparentRenderPass _transparentRenderPass;


        public override void Create() {
            _transparentRenderPass = new TransparentRenderPass();
        }

        public override void AddRenderPasses(LunarRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(_transparentRenderPass);
        }
    }

}