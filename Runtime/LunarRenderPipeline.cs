using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace Seven.LunarRenderPipeline {
    public class LunarRenderPipeline : RenderPipeline {


        private static ShaderTagId forwardLightmodeId = new(name: "UniversalForward");

        protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
            foreach (var camera in cameras) {
                RenderCamera(context, camera);
            }
        }


        private void RenderCamera(ScriptableRenderContext context, Camera camera) {
            context.SetupCameraProperties(camera);

            CameraClearFlags clearFlags = camera.clearFlags;


            CommandBuffer cmd = new() {
                name = "Render Loop"
            };
            cmd.ClearRenderTarget(
                (clearFlags & CameraClearFlags.Depth) != 0,
                (clearFlags & CameraClearFlags.Color) != 0,
                camera.backgroundColor
            );
            cmd.BeginSample("Render Camera");
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();



            if ( !camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters) ) {
                return;
            }	

    #if UNITY_EDITOR
            if (camera.cameraType == CameraType.SceneView) {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
    #endif
    
            CullingResults cullingResults = context.Cull(ref cullingParameters);

            SortingSettings sortingSettings = new(camera);
            DrawingSettings drawingSettings = new(forwardLightmodeId, sortingSettings);

            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        
            // Render opaque objects
            sortingSettings.criteria = SortingCriteria.CommonOpaque;
            filteringSettings.renderQueueRange = RenderQueueRange.opaque;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            // Render skybox if necessary
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) {
                context.DrawSkybox(camera);
            }

            // Render transparent objects
            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);



            cmd.EndSample("Render Camera");
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();

            context.Submit();
        }
    }
}
