using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.LunarRenderPipeline {
    [CreateAssetMenu(menuName = "Rendering/Lunar Render Pipeline")]
    public class LunarRenderPipelineAsset : RenderPipelineAsset  {

        [SerializeField] internal bool useSRPBatcher = true;
        [SerializeField] internal bool useInstancing = true;
        [SerializeField] internal bool enableLODCrossFade = true;
        [SerializeField] internal bool supportsHDR = true;

        [SerializeField] internal List<LunarRendererFeature> rendererFeatures = null;
        protected override RenderPipeline CreatePipeline() {
            return new LunarRenderPipeline(this);
        }

        private void OnEnable() {
            if ( rendererFeatures == null) {
                rendererFeatures = new List<LunarRendererFeature>() {
                    CreateInstance<OpaqueRendererFeature>(),
                    CreateInstance<SkyboxRendererFeature>(),
                    CreateInstance<TransparentRendererFeature>(),
                };
            }
        }
    }
}
