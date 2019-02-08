using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using System;
using UnityEngine.Experimental.Rendering;
namespace MPipeline
{
    [CreateAssetMenu(menuName ="MPipeline/Create Resources")]
    public unsafe class PipelineResources : RenderPipelineAsset
    {
        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new MPipeline.RenderPipeline(this);
        }
        public enum CameraRenderingPath
        {
            DemoPipeline = 0
        }
        [Serializable]
        public struct Shaders
        {
           //TODO
           //Add Shaders here
        }
        public Shaders shaders = new Shaders();
        public PipelineEvent[] gpurpEvents;
        private static Dictionary<CameraRenderingPath, Func<PipelineResources, PipelineEvent[]>> presetDict = null;
        public static Dictionary<CameraRenderingPath, Func<PipelineResources, PipelineEvent[]>> GetEventsDict()
        {
            if (presetDict != null) return presetDict;
            presetDict = new Dictionary<CameraRenderingPath, Func<PipelineResources, PipelineEvent[]>>();
            presetDict.Add(CameraRenderingPath.DemoPipeline, (res) => res.gpurpEvents);
            //TODO
            //Add Pipeline Settings
            return presetDict;
        }
    }
}