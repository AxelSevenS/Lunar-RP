using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LunarRenderPipeline {
    [CreateAssetMenu(menuName = "Rendering/Lunar Render Pipeline")]
    public class LunarRenderPipelineAsset : RenderPipelineAsset  {

        // --------------------------- //
        // ----- SETTINGS FIELDS ----- //
        // --------------------------- //
        [SerializeField] internal bool useSRPBatcher = true;
        [SerializeField] internal bool useInstancing = true;
        [SerializeField] internal bool enableLODCrossFade = true;
        [SerializeField] internal bool supportsHDR = true;
        [SerializeField] internal bool conservativeEnclosingSphere = false;
        [SerializeField] internal int numIterationsEnclosingSphere = 64;


        // ----------------------------- //
        // ----- RENDERER FEATURES ----- //
        // ----------------------------- //
        [SerializeField, HideInInspector] internal bool hasBeenInitialized = false;
        [SerializeField, HideInInspector] internal List<LunarRendererFeature> rendererFeatures = new();
        [SerializeField, HideInInspector] internal List<long> rendererFeaturesGUIDs = new();


        protected override RenderPipeline CreatePipeline() {
            return new LunarRenderPipeline(this);
        }
    }
}
