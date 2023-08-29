

using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace Seven.LunarRenderPipeline {

    public sealed class RendererFeatureList : TypeInstanceList<LunarRendererFeature, LunarRenderPipelineAssetEditor> {
        

        public RendererFeatureList(LunarRenderPipelineAssetEditor editor, SerializedProperty soInstances, SerializedProperty soInstanceGUIDs, bool hasBeenInitialized = true) : base(editor, soInstances, soInstanceGUIDs) {
            if (!hasBeenInitialized) {
                AddFeature(nameof(OpaqueRendererFeature));
                AddFeature(nameof(SkyboxRendererFeature));
                AddFeature(nameof(TransparentRendererFeature));
            }
        }

        public override GUIContent RenderFeaturesGUI => new(
            "Renderer Features",
            "A Renderer Feature is an asset that lets you add extra Render passes to a URP Renderer and configure their behavior."
        );

        public override GUIContent MissingFeature => new(
            "Missing RendererFeature",
            "Missing reference, due to compilation issues or missing files. you can attempt auto fix or choose to remove the feature."
        );

        public override GUIContent PassNameField => new(
            "Name", 
            "Render pass name. This name is the name displayed in Frame Debugger."
        );

        public override FilterWindow.IProvider InstanceProvider => new LunarRendererFeatureProvider(this);

        public override bool CanFix => false;
    }
}