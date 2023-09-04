using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using UnityEditor.Rendering;

namespace LunarRenderPipeline {

    [CustomEditor(typeof(LunarRenderPipelineAsset), true)]
    public class LunarRenderPipelineAssetEditor : Editor {

        public static readonly GUIContent renderFeaturesGUI = new(
            "Renderer Features",
            "A Renderer Feature is an asset that lets you add extra Render passes to a URP Renderer and configure their behavior."
        );
        public static readonly GUIContent MissingFeature = new(
            "Missing RendererFeature",
            "Missing reference, due to compilation issues or missing files. you can attempt auto fix or choose to remove the feature."
        );
        public static readonly GUIContent PassNameField = new(
            "Name", 
            "Render pass name. This name is the name displayed in Frame Debugger."
        );
        
        private LunarRenderPipelineAsset targetAsset;
        private SerializedProperty soUseSRPBatcher;
        private SerializedProperty soUseInstancing;
        private SerializedProperty soEnableLODCrossFade;
        private SerializedProperty soSupportsHDR;
        private SerializedProperty soRendererFeatures;
        private SerializedProperty soRendererFeaturesGUIDs;

        private RendererFeatureList list;

        private bool foldout = true;

        private void OnEnable() {
            soUseSRPBatcher = serializedObject.FindProperty(nameof(LunarRenderPipelineAsset.useSRPBatcher)/* "useSRPBatcher" */);
            soUseInstancing = serializedObject.FindProperty(nameof(LunarRenderPipelineAsset.useInstancing)/* "useInstancing" */);
            soEnableLODCrossFade = serializedObject.FindProperty(nameof(LunarRenderPipelineAsset.enableLODCrossFade)/* "enableLODCrossFade" */);
            soSupportsHDR = serializedObject.FindProperty(nameof(LunarRenderPipelineAsset.supportsHDR)/* "supportsHDR" */);

            soRendererFeatures = serializedObject.FindProperty(nameof(LunarRenderPipelineAsset.rendererFeatures)/* "rendererFeatures" */);
            soRendererFeaturesGUIDs = serializedObject.FindProperty(nameof(LunarRenderPipelineAsset.rendererFeaturesGUIDs)/* "rendererFeatures" */);

            SerializedProperty soHasBeenInitialized = serializedObject.FindProperty(nameof(LunarRenderPipelineAsset.hasBeenInitialized)/* "hasBeenInitialized" */);
            list = new RendererFeatureList(this, soRendererFeatures, soRendererFeaturesGUIDs);
            if ( !soHasBeenInitialized.boolValue ) {
                list.AddInstance(typeof(OpaqueRendererFeature));
                list.AddInstance(typeof(SkyboxRendererFeature));
                list.AddInstance(typeof(TransparentRendererFeature));
                soHasBeenInitialized.boolValue = true;
                serializedObject.ApplyModifiedProperties();
            }
            list?.UpdateEditorList();

            targetAsset = (LunarRenderPipelineAsset)target;
        }

        private void OnDisable() {
            list?.ClearEditorsList();
        }

        public override void OnInspectorGUI() {

            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, "Settings");
            if (foldout) {
                EditorGUILayout.PropertyField(soUseSRPBatcher);
                EditorGUILayout.PropertyField(soUseInstancing);
                EditorGUILayout.PropertyField(soEnableLODCrossFade);
                EditorGUILayout.PropertyField(soSupportsHDR);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            list?.DrawList();

        }
    }

}