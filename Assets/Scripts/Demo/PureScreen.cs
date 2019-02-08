using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace MPipeline
{
    [CreateAssetMenu(menuName = "Demo/Pure Screen Color")]
    public class PureScreen : PipelineEvent
    {
        public override bool CheckProperty()
        {
            return true;
        }

        protected override void Init(PipelineResources resources)
        {
            
        }

        protected override void Dispose()
        {
        }

        public override void FrameUpdate(PipelineCamera cam, ref PipelineCommandData data)
        {
            data.buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            data.buffer.ClearRenderTarget(true, true, new Color(0, 0.8f, 0.3f));
        }
    }
}
