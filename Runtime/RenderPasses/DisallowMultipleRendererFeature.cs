using System;


namespace LunarRenderPipeline {

    /// <summary>
    ///     <para>
    ///         Prevents <c>ScriptableRendererFeatures</c> of same type to be added more than once to a Scriptable Renderer.
    ///     </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DisallowMultipleRendererFeature : Attribute {

        /// <summary>
        /// Constructor for the attribute to prevent <c>ScriptableRendererFeatures</c> of same type to be added more than once to a Scriptable Renderer.
        /// </summary>
        public DisallowMultipleRendererFeature() {}
    }
}
