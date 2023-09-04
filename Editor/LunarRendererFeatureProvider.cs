using System;
using System.Collections.Generic;

using UnityEngine;

using UnityEditor;
using UnityEditor.Rendering;


namespace LunarRenderPipeline {

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
            foreach (Type type in types) {
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
                _list.AddInstance(featureElement.type);
                return true;
            }

            return false;
        }

        string GetMenuNameFromType(Type type) {
            string path = ObjectNames.NicifyVariableName(type.Name.Replace("RendererFeature", ""));

            if (type.Namespace != null) {
                if (type.Namespace.Contains("Experimental"))
                    path += " (Experimental)";
            }

            return path;
        }

    }
}
