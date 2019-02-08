using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
namespace MPipeline
{
    public struct PerspCam
    {
        public float3 right;
        public float3 up;
        public float3 forward;
        public float3 position;
        public float fov;
        public float nearClipPlane;
        public float farClipPlane;
        public float aspect;
        public float4x4 localToWorldMatrix;
        public float4x4 worldToCameraMatrix;
        public float4x4 projectionMatrix;
        public void UpdateTRSMatrix()
        {
            localToWorldMatrix.c0 = float4(right, 0);
            localToWorldMatrix.c1 = float4(up, 0);
            localToWorldMatrix.c2 = float4(forward, 0);
            localToWorldMatrix.c3 = float4(position, 1);
            worldToCameraMatrix = MatrixUtility.GetWorldToLocal(localToWorldMatrix);
            float4 row2 = -float4(worldToCameraMatrix.c0.z, worldToCameraMatrix.c1.z, worldToCameraMatrix.c2.z, worldToCameraMatrix.c3.z);
            worldToCameraMatrix.c0.z = row2.x;
            worldToCameraMatrix.c1.z = row2.y;
            worldToCameraMatrix.c2.z = row2.z;
            worldToCameraMatrix.c3.z = row2.w;
        }
        public void UpdateViewMatrix(float4x4 localToWorld)
        {
            worldToCameraMatrix = MatrixUtility.GetWorldToLocal(localToWorld);
            float4 row2 = -float4(worldToCameraMatrix.c0.z, worldToCameraMatrix.c1.z, worldToCameraMatrix.c2.z, worldToCameraMatrix.c3.z);
            worldToCameraMatrix.c0.z = row2.x;
            worldToCameraMatrix.c1.z = row2.y;
            worldToCameraMatrix.c2.z = row2.z;
            worldToCameraMatrix.c3.z = row2.w;
        }
        public void UpdateProjectionMatrix()
        {
            projectionMatrix = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
        }
    }
    public struct OrthoCam
    {
        public float4x4 worldToCameraMatrix;
        public float4x4 localToWorldMatrix;
        public float3 right;
        public float3 up;
        public float3 forward;
        public float3 position;
        public float size;
        public float nearClipPlane;
        public float farClipPlane;
        public float4x4 projectionMatrix;
        public void UpdateTRSMatrix()
        {
            localToWorldMatrix.c0 = new float4(right, 0);
            localToWorldMatrix.c1 = new float4(up, 0);
            localToWorldMatrix.c2 = new float4(forward, 0);
            localToWorldMatrix.c3 = new float4(position, 1);
            worldToCameraMatrix = MatrixUtility.GetWorldToLocal(localToWorldMatrix);
            worldToCameraMatrix.c0.z = -worldToCameraMatrix.c0.z;
            worldToCameraMatrix.c1.z = -worldToCameraMatrix.c1.z;
            worldToCameraMatrix.c2.z = -worldToCameraMatrix.c2.z;
            worldToCameraMatrix.c3.z = -worldToCameraMatrix.c3.z;
        }
        public void UpdateProjectionMatrix()
        {
            projectionMatrix = Matrix4x4.Ortho(-size, size, -size, size, nearClipPlane, farClipPlane);
        }
    }
    public struct PipelineCommandData
    {
        public Matrix4x4 vp;                            //Current camera's view projection Matrix
        public Matrix4x4 inverseVP;                     //Current camera's inverse view projection matrix
        public Vector4[] frustumPlanes;                 //Current camera's frustum planes
        public CommandBuffer buffer;                    //Main command buffer
        public ScriptableRenderContext context;         //Main context
        public CullResults cullResults;                 //Camera's culling results
        public ScriptableCullingParameters cullParams;  //Camera's culling parameters
        public PipelineResources resources;             //Resources
    }
}