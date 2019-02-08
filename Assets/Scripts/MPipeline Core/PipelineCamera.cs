using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
namespace MPipeline
{
    [RequireComponent(typeof(Camera))]
    public class PipelineCamera : MonoBehaviour
    {
        [System.NonSerialized]
        public Camera cam;
        public PipelineResources.CameraRenderingPath renderingPath;
        public Dictionary<Type, IPerCameraData> postDatas = new Dictionary<Type, IPerCameraData>(47);
        public void EnableThis()
        {
            //TODO
            //Enabled Camera
        }

        private void OnDestroy()
        {
            DisableThis();
        }

        public void DisableThis()
        {
            foreach (var i in postDatas.Values)
            {
                i.DisposeProperty();
            }
            postDatas.Clear();
        }
    }
}
