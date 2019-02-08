using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
namespace MPipeline
{
    [CreateAssetMenu(menuName = "Demo/Pure Screen Color")]
    public class PureScreen : PipelineEvent
    {
        protected override void Init(PipelineResources resources)
        {
        }

        public override bool CheckProperty()
        {
            return true;
        }

        protected override void Dispose()
        {
        }

        public override void FrameUpdate(PipelineCamera cam, ref PipelineCommandData data)
        {
            data.buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            data.buffer.ClearRenderTarget(true, true, Color.black);
            data.ExecuteCommandBuffer(); //Execute the commandbuffer, buffer will be automatically cleared after executed
            FilterRenderersSettings filterSettings = new FilterRenderersSettings(true);
            filterSettings.excludeMotionVectorObjects = false;
            filterSettings.layerMask = cam.cam.cullingMask;
            filterSettings.renderQueueRange = RenderQueueRange.opaque;
            DrawRendererSettings drawSettings = new DrawRendererSettings(cam.cam, new ShaderPassName("Unlit"));
            drawSettings.flags = DrawRendererFlags.EnableDynamicBatching;
            drawSettings.rendererConfiguration = RendererConfiguration.None;
            drawSettings.sorting.flags = SortFlags.CommonOpaque;
            data.context.DrawRenderers(data.cullResults.visibleRenderers, ref drawSettings, filterSettings);
        }
    }
}
