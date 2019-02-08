using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using System;
using System.Text;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using MPipeline;

public unsafe static class PipelineFunctions
{

    public static void GetOrthoCullingPlanes(ref OrthoCam orthoCam, float4* planes)
    {
        planes[0] = VectorUtility.GetPlane(orthoCam.forward, orthoCam.position + orthoCam.forward * orthoCam.farClipPlane);
        planes[1] = VectorUtility.GetPlane(-orthoCam.forward, orthoCam.position + orthoCam.forward * orthoCam.nearClipPlane);
        planes[2] = VectorUtility.GetPlane(-orthoCam.up, orthoCam.position - orthoCam.up * orthoCam.size);
        planes[3] = VectorUtility.GetPlane(orthoCam.up, orthoCam.position + orthoCam.up * orthoCam.size);
        planes[4] = VectorUtility.GetPlane(orthoCam.right, orthoCam.position + orthoCam.right * orthoCam.size);
        planes[5] = VectorUtility.GetPlane(-orthoCam.right, orthoCam.position - orthoCam.right * orthoCam.size);
    }

    public static void GetFrustumCorner(ref PerspCam perspCam, float distance, float3* corners)
    {
        perspCam.fov = Mathf.Deg2Rad * perspCam.fov * 0.5f;
        float upLength = distance * tan(perspCam.fov);
        float rightLength = upLength * perspCam.aspect;
        float3 farPoint = perspCam.position + distance * perspCam.forward;
        float3 upVec = upLength * perspCam.up;
        float3 rightVec = rightLength * perspCam.right;
        corners[0] = farPoint - upVec - rightVec;
        corners[1] = farPoint - upVec + rightVec;
        corners[2] = farPoint + upVec - rightVec;
        corners[3] = farPoint + upVec + rightVec;
    }

    public static void GetFrustumPlanes(ref PerspCam perspCam, float4* planes)
    {
        float3* corners = stackalloc float3[4];
        GetFrustumCorner(ref perspCam, perspCam.farClipPlane, corners);
        planes[0] = VectorUtility.GetPlane(corners[1], corners[0], perspCam.position);
        planes[1] = VectorUtility.GetPlane(corners[2], corners[3], perspCam.position);
        planes[2] = VectorUtility.GetPlane(corners[0], corners[2], perspCam.position);
        planes[3] = VectorUtility.GetPlane(corners[3], corners[1], perspCam.position);
        planes[4] = VectorUtility.GetPlane(perspCam.forward, perspCam.position + perspCam.forward * perspCam.farClipPlane);
        planes[5] = VectorUtility.GetPlane(-perspCam.forward, perspCam.position + perspCam.forward * perspCam.nearClipPlane);
    }
    public static void GetFrustumPlanes(ref OrthoCam ortho, float4* planes)
    {
        planes[0] = VectorUtility.GetPlane(ortho.up, ortho.position + ortho.up * ortho.size);
        planes[1] = VectorUtility.GetPlane(-ortho.up, ortho.position - ortho.up * ortho.size);
        planes[2] = VectorUtility.GetPlane(ortho.right, ortho.position + ortho.right * ortho.size);
        planes[3] = VectorUtility.GetPlane(-ortho.right, ortho.position - ortho.right * ortho.size);
        planes[4] = VectorUtility.GetPlane(ortho.forward, ortho.position + ortho.forward * ortho.farClipPlane);
        planes[5] = VectorUtility.GetPlane(-ortho.forward, ortho.position + ortho.forward * ortho.nearClipPlane);
    }

    public static void GetfrustumCorners(float* planes, int planesCount, Camera cam, float3* frustumCorners)
    {
        for (int i = 0; i < planesCount; ++i)
        {
            int index = i * 4;
            float p = planes[i];
            frustumCorners[index] = cam.ViewportToWorldPoint(new Vector3(0, 0, p));
            frustumCorners[1 + index] = cam.ViewportToWorldPoint(new Vector3(0, 1, p));
            frustumCorners[2 + index] = cam.ViewportToWorldPoint(new Vector3(1, 1, p));
            frustumCorners[3 + index] = cam.ViewportToWorldPoint(new Vector3(1, 0, p));
        }

    }

    public static int DownDimension(int3 coord, int2 xysize)
    {
        return coord.z * xysize.y * xysize.x + coord.y * xysize.x + coord.x;
    }

    public static int3 UpDimension(int coord, int2 xysize)
    {
        int xy = (xysize.x * xysize.y);
        return int3(coord % xysize.x, (coord % xy) / xysize.x, coord / xy);
    }

    public static bool FrustumCulling(ref Matrix4x4 ObjectToWorld, Vector3 extent, Vector4* frustumPlanes)
    {
        Vector3 right = new Vector3(ObjectToWorld.m00, ObjectToWorld.m10, ObjectToWorld.m20);
        Vector3 up = new Vector3(ObjectToWorld.m01, ObjectToWorld.m11, ObjectToWorld.m21);
        Vector3 forward = new Vector3(ObjectToWorld.m02, ObjectToWorld.m12, ObjectToWorld.m22);
        Vector3 position = new Vector3(ObjectToWorld.m03, ObjectToWorld.m13, ObjectToWorld.m23);
        for (int i = 0; i < 6; ++i)
        {
            ref Vector4 plane = ref frustumPlanes[i];
            Vector3 normal = new Vector3(plane.x, plane.y, plane.z);
            float distance = plane.w;
            float r = Vector3.Dot(position, normal);
            Vector3 absNormal = new Vector3(Mathf.Abs(Vector3.Dot(normal, right)), Mathf.Abs(Vector3.Dot(normal, up)), Mathf.Abs(Vector3.Dot(normal, forward)));
            float f = Vector3.Dot(absNormal, extent);
            if ((r - f) >= -distance)
                return false;
        }
        return true;
    }

    public static bool FrustumCulling(Vector3 position, float range, Vector4* frustumPlanes)
    {
        for (int i = 0; i < 5; ++i)
        {
            ref Vector4 plane = ref frustumPlanes[i];
            Vector3 normal = new Vector3(plane.x, plane.y, plane.z);
            float rayDist = Vector3.Dot(normal, position);
            rayDist += plane.w;
            if (rayDist > range)
            {
                return false;
            }
        }
        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetViewProjectMatrix(Camera currentCam, out Matrix4x4 vp, out Matrix4x4 invVP)
    {
        vp = mul(GraphicsUtility.GetGPUProjectionMatrix(currentCam.projectionMatrix, false), currentCam.worldToCameraMatrix);
        invVP = vp.inverse;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExecuteCommandBuffer(ref this PipelineCommandData data)
    {
        data.context.ExecuteCommandBuffer(data.buffer);
        data.buffer.Clear();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExecuteCommandBufferAsync(ref this PipelineCommandData data, CommandBuffer asyncBuffer, ComputeQueueType queueType)
    {
        data.context.ExecuteCommandBufferAsync(asyncBuffer, queueType);
        asyncBuffer.Clear();
    }

    public static void InsertTo<T>(this List<T> targetArray, T value, Func<T, T, int> compareResult)
    {
        Vector2Int range = new Vector2Int(0, targetArray.Count);
        while (true)
        {
            if (targetArray.Count == 0)
            {
                targetArray.Add(value);
                return;
            }
            else if (abs(range.x - range.y) == 1)
            {
                int compareX = compareResult(targetArray[range.x], value);
                if (compareX < 0)
                {
                    targetArray.Insert(range.x, value);
                    return;
                }
                else if (compareX > 0)
                {
                    if (range.y < targetArray.Count && compareResult(targetArray[range.y], value) == 0)
                    {
                        return;
                    }
                    else
                    {
                        targetArray.Insert(range.y, value);
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                int currentIndex = (int)((range.x + range.y) / 2f);
                int compare = compareResult(targetArray[currentIndex], value);
                if (compare == 0)
                {
                    return;
                }
                else
                {
                    if (compare < 0)
                    {
                        range.y = currentIndex;
                    }
                    else if (compare > 0)
                    {
                        range.x = currentIndex;
                    }
                }
            }
        }
    }
 
}