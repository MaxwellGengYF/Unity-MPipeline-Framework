using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using System;
using UnityEngine.Rendering;
using System.Reflection;
using UnityEngine.Experimental.Rendering;
namespace MPipeline
{
    public unsafe class RenderPipeline : UnityEngine.Experimental.Rendering.RenderPipeline
    {
        #region STATIC_AREA

        public static RenderPipeline current;
        public static PipelineCommandData data;
        #endregion
        public PipelineResources resources;
        public static T GetEvent<T>(PipelineResources.CameraRenderingPath path) where T : PipelineEvent
        {
            var allEvents = current.resources.renderingPaths;
            PipelineEvent[] events = allEvents[path];
            for (int i = 0; i < events.Length; ++i)
            {
                PipelineEvent evt = events[i];
                if (evt.GetType() == typeof(T)) return (T)evt;
            }
            return null;
        }

        public static PipelineEvent GetEvent(PipelineResources.CameraRenderingPath path, Type targetType)
        {
            var allEvents = current.resources.renderingPaths;
            PipelineEvent[] events = allEvents[path];
            for (int i = 0; i < events.Length; ++i)
            {
                PipelineEvent evt = events[i];
                if (evt.GetType() == targetType) return evt;
            }
            return null;
        }

        public RenderPipeline(PipelineResources resources)
        {
            resources.SetRenderingPath();
            var allEvents = resources.renderingPaths;
            this.resources = resources;
            current = this;
            data.buffer = new CommandBuffer();
            data.frustumPlanes = new Vector4[6];
            var keys = allEvents.Keys;
            foreach (var i in keys)
            {
                PipelineEvent[] events = allEvents[i];
                foreach(var j in events)
                {
                    j.Prepare(i);
                }
                foreach (var j in events)
                {
                    j.InitEvent(resources);
                }
            }
        }

        public override void Dispose()
        {
            if (current != this) return;
            current = null;
            data.buffer.Dispose();
            var allEvents = resources.renderingPaths;
            var values = allEvents.Values;
            foreach (var i in values)
            {
                PipelineEvent[] array = i;
                foreach (var j in array)
                {
                    j.DisposeEvent();
                }
            }
        }
        public override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            bool* propertyCheckedFlags = stackalloc bool[]
            {
                false,
                false,
                false
            };
            foreach (var cam in cameras)
            {
                PipelineCamera pipelineCam = cam.GetComponent<PipelineCamera>();
                if (!pipelineCam)
                {
                    pipelineCam = Camera.main.GetComponent<PipelineCamera>();
                    if (!pipelineCam) continue;
                }
                Render(pipelineCam, BuiltinRenderTextureType.CameraTarget, ref renderContext, cam, propertyCheckedFlags);
                data.ExecuteCommandBuffer();
                renderContext.Submit();
            }
        }

        private void Render(PipelineCamera pipelineCam, RenderTargetIdentifier dest, ref ScriptableRenderContext context, Camera cam, bool* pipelineChecked)
        {
            PipelineResources.CameraRenderingPath path = pipelineCam.renderingPath;
            pipelineCam.cam = cam;
            pipelineCam.EnableThis();
            if (!CullResults.GetCullingParameters(cam, out data.cullParams)) return;
            context.SetupCameraProperties(cam);
            //Set Global Data
            data.context = context;
            data.cullResults = CullResults.Cull(ref data.cullParams, context);
            data.resources = resources;
            PipelineFunctions.GetViewProjectMatrix(cam, out data.vp, out data.inverseVP);
            for (int i = 0; i < data.frustumPlanes.Length; ++i)
            {
                Plane p = data.cullParams.GetCullingPlane(i);
                //GPU Driven RP's frustum plane is inverse from SRP's frustum plane
                data.frustumPlanes[i] = new Vector4(-p.normal.x, -p.normal.y, -p.normal.z, -p.distance);
            }
            var allEvents = resources.renderingPaths;
            var collect = allEvents[pipelineCam.renderingPath];
#if UNITY_EDITOR
            //Need only check for Unity Editor's bug!
            if (!pipelineChecked[(int)pipelineCam.renderingPath])
            {
                pipelineChecked[(int)pipelineCam.renderingPath] = true;
                foreach (var e in collect)
                {
                    if (!e.CheckProperty())
                    {
                        e.InitEvent(resources);
                    }
                }
            }
#endif
            foreach (var e in collect)
            {
                if (e.Enabled)
                {
                    e.PreRenderFrame(pipelineCam, ref data);
                }
            }
            JobHandle.ScheduleBatchedJobs();
            foreach (var e in collect)
            {
                if (e.Enabled)
                {
                    e.FrameUpdate(pipelineCam, ref data);
                }
            }
        }
    }
}