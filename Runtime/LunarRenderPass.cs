using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seven.LunarRenderPipeline {



    public abstract class LunarRenderPass {

        public abstract Dependency dependencies { get; }
        public abstract Event renderPassEvent { get; }

        public abstract void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor);

        public abstract void Execute(ScriptableRenderContext context, ref RenderingData renderingData);

        public virtual void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }

        public virtual void FrameCleanup(CommandBuffer cmd) { }
        
    
        /// <summary>
        /// Input requirements for <c>LunarRenderPass</c>.
        /// </summary>
        /// <seealso cref="ConfigureInput"/>
        [Flags]
        public enum Dependency {
            /// <summary>
            /// Used when a <c>LunarRenderPass</c> does not require any texture.
            /// </summary>
            None = 0,

            /// <summary>
            /// Used when a <c>LunarRenderPass</c> requires a depth texture.
            /// </summary>
            Depth = 1 << 0,

            /// <summary>
            /// Used when a <c>LunarRenderPass</c> requires a normal texture.
            /// </summary>
            Normal = 1 << 1,

            /// <summary>
            /// Used when a <c>LunarRenderPass</c> requires a color texture.
            /// </summary>
            Color = 1 << 2,

            // /// <summary>
            // /// Used when a <c>LunarRenderPass</c> requires a motion vectors texture.
            // /// </summary>
            // Motion = 1 << 3,
        }

        // Note: Spaced built-in events so we can add events in between them
        // We need to leave room as we sort render passes based on event.
        // Users can also inject render pass events in a specific point by doing RenderPassEvent + offset
        /// <summary>
        /// Controls when the render pass executes.
        /// </summary>
        public enum Event {
            /// <summary>
            /// Executes a <c>LunarRenderPass</c> before rendering any other passes in the pipeline.
            /// Camera matrices and stereo rendering are not setup this point.
            /// You can use this to draw to custom input textures used later in the pipeline, f.ex LUT textures.
            /// </summary>
            BeforeRendering = 0,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> before rendering shadowmaps.
            /// Camera matrices and stereo rendering are not setup this point.
            /// </summary>
            BeforeRenderingShadows = 50,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> after rendering shadowmaps.
            /// Camera matrices and stereo rendering are not setup this point.
            /// </summary>
            AfterRenderingShadows = 100,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> before rendering prepasses, f.ex, depth prepass.
            /// Camera matrices and stereo rendering are already setup at this point.
            /// </summary>
            BeforeRenderingPrePasses = 150,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> after rendering prepasses, f.ex, depth prepass.
            /// Camera matrices and stereo rendering are already setup at this point.
            /// </summary>
            AfterRenderingPrePasses = 200,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> before rendering gbuffer pass.
            /// </summary>
            BeforeRenderingGbuffer = 210,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> after rendering gbuffer pass.
            /// </summary>
            AfterRenderingGbuffer = 220,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> before rendering deferred shading pass.
            /// </summary>
            BeforeRenderingDeferredLights = 230,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> after rendering deferred shading pass.
            /// </summary>
            AfterRenderingDeferredLights = 240,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> before rendering opaque objects.
            /// </summary>
            BeforeRenderingOpaques = 250,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> after rendering opaque objects.
            /// </summary>
            AfterRenderingOpaques = 300,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> before rendering the sky.
            /// </summary>
            BeforeRenderingSkybox = 350,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> after rendering the sky.
            /// </summary>
            AfterRenderingSkybox = 400,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> before rendering transparent objects.
            /// </summary>
            BeforeRenderingTransparents = 450,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> after rendering transparent objects.
            /// </summary>
            AfterRenderingTransparents = 500,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> before rendering post-processing effects.
            /// </summary>
            BeforeRenderingPostProcessing = 550,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> after rendering post-processing effects but before final blit, post-processing AA effects and color grading.
            /// </summary>
            AfterRenderingPostProcessing = 600,

            /// <summary>
            /// Executes a <c>LunarRenderPass</c> after rendering all effects.
            /// </summary>
            AfterRendering = 1000,
        }
    }


}