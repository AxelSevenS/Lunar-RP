using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.LunarRenderPipeline {
    [CreateAssetMenu(menuName = "Rendering/Lunar Render Pipeline")]
    public class LunarRenderPipelineAsset : RenderPipelineAsset  {

        [SerializeField, HideInInspector] internal bool hasBeenInitialized = false;

        [SerializeField] internal bool useSRPBatcher = true;
        [SerializeField] internal bool useInstancing = true;
        [SerializeField] internal bool enableLODCrossFade = true;
        [SerializeField] internal bool supportsHDR = true;

        [SerializeField, HideInInspector] internal List<LunarRendererFeature> rendererFeatures = new();
        [SerializeField, HideInInspector] internal List<long> rendererFeaturesGUIDs = new();
        protected override RenderPipeline CreatePipeline() {
            return new LunarRenderPipeline(this);
        }

        private void ResetList() {
            // if ( rendererFeatures == null /* || rendererFeatures.Count == 0  */) {
            //     rendererFeatures = new List<LunarRendererFeature>() {
            //         CreateInstance<OpaqueRendererFeature>(),
            //         CreateInstance<SkyboxRendererFeature>(),
            //         CreateInstance<TransparentRendererFeature>(),
            //     };
            // }
        }

        private void OnEnable() {
            ResetList();
        }

        protected override void OnValidate() {
            base.OnValidate();
            ResetList();
        }
    }
}
