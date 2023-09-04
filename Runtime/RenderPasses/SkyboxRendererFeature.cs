namespace LunarRenderPipeline {

    /// <summary>
    ///     <para>
    ///         Injects the <c>SkyboxRenderPass</c> into the render pipeline.
    ///     </para>
    /// </summary>
    [DisallowMultipleRendererFeature]
    public sealed class SkyboxRendererFeature : LunarRendererFeature {

        private SkyboxRenderPass _skyboxRenderPass;


        public override void Create() {
            _skyboxRenderPass = new SkyboxRenderPass();
        }

        public override void AddRenderPasses(LunarRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(_skyboxRenderPass);
        }
    }

}