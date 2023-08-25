using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.LunarRenderPipeline {
    [CreateAssetMenu(menuName = "Rendering/Lunar Render Pipeline")]
    public class LunarRenderPipelineAsset : RenderPipelineAsset  {

        [SerializeField] internal bool useSRPBatcher = true;
        [SerializeField] internal bool useDynamicBatching = true;
        [SerializeField] internal bool enableLODCrossFade = true;
        [SerializeField] internal bool supportsHDR = true;
        protected override RenderPipeline CreatePipeline() {
            return new LunarRenderPipeline(this);
        }
    }
}
