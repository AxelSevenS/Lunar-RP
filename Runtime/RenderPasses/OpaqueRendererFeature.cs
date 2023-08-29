namespace Seven.LunarRenderPipeline {
    public sealed class OpaqueRendererFeature : LunarRendererFeature {

        private OpaqueRenderPass _opaqueRenderPass;


        public override void Create() {
            _opaqueRenderPass = new OpaqueRenderPass();
        }

        public override void AddRenderPasses(LunarRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(_opaqueRenderPass);
        }
    }

}