

using System;
using System.Reflection;

using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;

using SevenGame.Utility;

namespace LunarRenderPipeline {

    public sealed class RendererFeatureList : TypeInstanceList<LunarRendererFeature, LunarRenderPipelineAssetEditor> {
        

        public RendererFeatureList(LunarRenderPipelineAssetEditor editor, SerializedProperty soInstances, SerializedProperty soInstanceGUIDs) : base(editor, soInstances, soInstanceGUIDs) {}

        public override GUIContent RenderFeaturesGUI => new(
            "Renderer Features",
            "A Renderer Feature is an asset that lets you add extra Render passes to a URP Renderer and configure their behavior."
        );
        public override GUIContent MissingFeatureGUI => new(
            "Missing RendererFeature",
            "Missing reference, due to compilation issues or missing files. you can attempt auto fix or choose to remove the feature."
        );
        public override GUIContent PassNameFieldGUI => new(
            "Name", 
            "Render pass name. This name is the name displayed in Frame Debugger."
        );
        public override GUIContent NoInstanceGUI => new(
            "No Renderer Features added", 
            "A Renderer Feature is an asset that lets you add extra Render passes to Lunar and configure their behavior."
        );
        public override GUIContent AddInstanceGUI => new(
            "Add Renderer Feature",
            "A Renderer Feature is an asset that lets you add extra Render passes to Lunar and configure their behavior."
        );

        public override FilterWindow.IProvider InstanceProvider => new LunarRendererFeatureProvider(this);

        public override bool CanFix => false;

        protected override bool DuplicateCheck(Type type) {
            bool isUniqueFeature = type.GetCustomAttribute(typeof(DisallowMultipleRendererFeature)) != null;
            // Return if there are no limitations on this feature OR if there can be no other features of this type.
            if ( !isUniqueFeature || soInstances.arraySize == 0 ) {
                return false;
            }
            
            // Check if there is already a feature of this type.
            for (int i = 0; i < soInstances.arraySize; i++) {
                SerializedProperty property = soInstances.GetArrayElementAtIndex(i);
                if (property.objectReferenceValue.GetType() == type) {
                    return true;
                }
            }

            // No duplicate found.
            return false;
        }
    }
}