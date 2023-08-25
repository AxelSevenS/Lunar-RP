using UnityEngine;


namespace Seven.LunarRenderPipeline {

    /// <summary>
    /// Struct that flattens several rendering settings used to render a camera stack.
    /// URP builds the <c>RenderingData</c> settings from several places, including the pipeline asset, camera and light settings.
    /// The settings also might vary on different platforms and depending on if Adaptive Performance is used.
    /// </summary>
    public struct CameraData {
        /// <summary>
        /// The camera component.
        /// </summary>
        public Camera camera;

        private CameraData(Camera camera) {
            this.camera = camera;
        }

        public static CameraData GetCameraData(Camera camera) {
            CameraData data = new CameraData(camera);
            return data;
        }
    }

}