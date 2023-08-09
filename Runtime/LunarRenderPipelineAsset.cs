using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.LunarRenderPipeline {
    [CreateAssetMenu(menuName = "Rendering/Lunar Render Pipeline")]
    public class LunarRenderPipelineAsset : RenderPipelineAsset  {
        protected override RenderPipeline CreatePipeline() {
            return new LunarRenderPipeline();
        }
    }
}
