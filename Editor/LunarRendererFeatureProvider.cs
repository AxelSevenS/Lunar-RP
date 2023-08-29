using System;
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;
using UnityEditor.Rendering;
// using UnityEngine.Rendering.Universal;

namespace Seven.LunarRenderPipeline {
    class LunarRendererFeatureProvider : FilterWindow.IProvider {
        class FeatureElement : FilterWindow.Element {
            public Type type;
        }

        readonly RendererFeatureList _list;
        public Vector2 position { get; set; }

        public LunarRendererFeatureProvider(RendererFeatureList list) {
            _list = list;
        }

        public void CreateComponentTree(List<FilterWindow.Element> tree) {
            tree.Add(new FilterWindow.GroupElement(0, "Renderer Features"));
            var types = TypeCache.GetTypesDerivedFrom<LunarRendererFeature>();
            var data = _list.editor.target as LunarRenderPipelineAsset;
            foreach (var type in types) {
                // if (data.DuplicateFeatureCheck(type)) {
                //     continue;
                // }

                string path = GetMenuNameFromType(type);
                tree.Add(new FeatureElement {
                    content = new GUIContent(path),
                    level = 1,
                    type = type
                });
            }
        }

        public bool GoToChild(FilterWindow.Element element, bool addIfComponent) {
            if (element is FeatureElement featureElement) {
                _list.AddFeature(featureElement.type.Name);
                return true;
            }

            return false;
        }

        string GetMenuNameFromType(Type type) {
            string path;
            // if (!m_Editor.GetCustomTitle(type, out path)) {
                path = ObjectNames.NicifyVariableName(type.Name);
            // }

            if (type.Namespace != null) {
                if (type.Namespace.Contains("Experimental"))
                    path += " (Experimental)";
            }

            return path;
        }

    }
}
