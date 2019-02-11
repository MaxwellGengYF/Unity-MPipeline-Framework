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
        public PipelineEvent[] demoPipelineEvents;
        private Dictionary<CameraRenderingPath, PipelineEvent[]> presetDict = new Dictionary<CameraRenderingPath, PipelineEvent[]>();
        public Dictionary<CameraRenderingPath, PipelineEvent[]> renderingPaths
        {
            get { return presetDict; }
        }
        public void SetRenderingPath()
        {
            presetDict.Clear();
            presetDict.Add(CameraRenderingPath.DemoPipeline, demoPipelineEvents);
            //Add New Events Here
        }
    }
}