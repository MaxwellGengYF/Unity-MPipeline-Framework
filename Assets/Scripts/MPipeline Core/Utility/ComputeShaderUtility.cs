using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using System.Runtime.CompilerServices;
using Unity.Mathematics;


public static class ComputeShaderUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispatch(ComputeShader shader, CommandBuffer buffer, int kernal, int count, float threadGroupCount)
    {
        int threadPerGroup = Mathf.CeilToInt(count / threadGroupCount);
        buffer.SetComputeIntParam(shader, ShaderIDs._Count, count);
        buffer.DispatchCompute(shader, kernal, threadPerGroup, 1, 1);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Dispatch(ComputeShader shader, int kernal, int count, float threadGroupCount)
    {
        int threadPerGroup = Mathf.CeilToInt(count / threadGroupCount);
        shader.SetInt(ShaderIDs._Count, count);
        shader.Dispatch(kernal, threadPerGroup, 1, 1);
    }
}
public unsafe static class MUnsafeUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Resize<T>(ref this NativeArray<T> arr, int targetLength, Allocator alloc) where T : unmanaged
    {
        if (targetLength <= arr.Length) return;
        NativeArray<T> newArr = new NativeArray<T>(targetLength, alloc);
        MemCpy(newArr.GetUnsafePtr(), arr.GetUnsafePtr(), sizeof(T) * arr.Length);
        arr.Dispose();
        arr = newArr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* Ptr<T>(ref this NativeArray<T> arr) where T : unmanaged
    {
        return (T*)arr.GetUnsafePtr();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T Get<T>(ref this NativeArray<T> arr, int index) where T : unmanaged
    {
        return ref *((T*)arr.GetUnsafePtr() + index);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyFrom<T>(this T[] array, T* source, int length) where T : unmanaged
    {
        fixed(T* dest = array)
        {
            MemCpy(dest, source, length * sizeof(T));
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* Ptr<T>(this T[] array) where T: unmanaged
    {
        return (T*)AddressOf(ref array[0]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* Ptr<T>(ref this T array) where T : unmanaged
    {
        return (T*)AddressOf(ref array);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo<T>(this T[] array, T* dest, int length) where T : unmanaged
    {
        fixed (T* source = array)
        {
            MemCpy(dest, source, length * sizeof(T));
        }
    }

    private struct PtrKeeper<T>
    {
        public T value;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetManagedPtr<T>(T obj) where T : class
    {
        PtrKeeper<T> keeper = new PtrKeeper<T> { value = obj };
        void* ptr = null;
        MemCpy(&ptr, AddressOf(ref keeper), SizeOf<PtrKeeper<T>>());
        return ptr;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetObject<T>(void* ptr) where T : class
    {
        PtrKeeper<T> keeper = new PtrKeeper<T>();
        MemCpy(AddressOf(ref keeper), &ptr, SizeOf<PtrKeeper<T>>());
        return keeper.value;
    }
}