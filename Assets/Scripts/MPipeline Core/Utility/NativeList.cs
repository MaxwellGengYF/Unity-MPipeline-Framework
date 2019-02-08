using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using System.Threading;
using System.Runtime.CompilerServices;

public unsafe struct NativeListData
{
    public int count;
    public int capacity;
    public Allocator allocator;
    public void* ptr;
}
public unsafe struct NativeList<T> : IEnumerable<T> where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction]
    private NativeListData* data;
    public bool isCreated { get; private set; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeList(int capacity, Allocator alloc)
    {
        isCreated = true;
        capacity = Mathf.Max(capacity, 1);
        data = (NativeListData*)UnsafeUtility.Malloc(sizeof(NativeListData), 16, alloc);
        data->count = 0;
        data->capacity = capacity;
        data->allocator = alloc;
        data->ptr = UnsafeUtility.Malloc(sizeof(T) * capacity, 16, alloc);
    }
    public NativeList(int count, Allocator alloc, T defaultValue)
    {
        isCreated = true;
        data = (NativeListData*)UnsafeUtility.Malloc(sizeof(NativeListData), 16, alloc);
        data->count = count;
        data->capacity = count;
        data->allocator = alloc;
        data->ptr = UnsafeUtility.Malloc(sizeof(T) * count, 16, alloc);
        T* add = (T*)data->ptr;
        for (int i = 0; i < count; ++i)
        {
            add[i] = defaultValue;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeList(int count, int capacity, Allocator alloc)
    {
        isCreated = true;
        data = (NativeListData*)UnsafeUtility.Malloc(sizeof(NativeListData), 16, alloc);
        data->count = count;
        data->capacity = capacity;
        data->allocator = alloc;
        data->ptr = UnsafeUtility.Malloc(sizeof(T) * capacity, 16, alloc);
    }
    public Allocator allocator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return data->allocator;
        }
    }
    private void Resize()
    {
        if (data->count <= data->capacity) return;
        int lastcapacity = data->capacity;
        data->capacity *= 2;
        void* newPtr = UnsafeUtility.Malloc(sizeof(T) * data->capacity, 16, data->allocator);
        UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(T) * lastcapacity);
        UnsafeUtility.Free(data->ptr, data->allocator);
        data->ptr = newPtr;
    }

    private void ResizeToCount()
    {
        if (data->count <= data->capacity) return;
        int lastcapacity = data->capacity;
        data->capacity = data->count;
        void* newPtr = UnsafeUtility.Malloc(sizeof(T) * data->capacity, 16, data->allocator);
        UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(T) * lastcapacity);
        UnsafeUtility.Free(data->ptr, data->allocator);
        data->ptr = newPtr;
    }

    public void AddCapacityTo(int capacity)
    {
        if (capacity <= data->capacity) return;
        int lastcapacity = data->capacity;
        data->capacity = capacity;
        void* newPtr = UnsafeUtility.Malloc(sizeof(T) * data->capacity, 16, data->allocator);
        UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(T) * lastcapacity);
        UnsafeUtility.Free(data->ptr, data->allocator);
        data->ptr = newPtr;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveLast(int length)
    {
        data->count -= length;
        data->count = Mathf.Max(0, data->count);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveLast()
    {
        data->count -= 1;
        data->count = Mathf.Max(0, data->count);
    }


    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return data->count;
        }
    }
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return data->capacity;
        }
    }
    public T* unsafePtr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return (T*)data->ptr;
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        isCreated = false;
        Allocator alloc = data->allocator;
        UnsafeUtility.Free(data->ptr, alloc);
        UnsafeUtility.Free(data, alloc);
    }
    public ref T this[int id]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            T* ptr = (T*)data->ptr;
            return ref *(ptr + id);
        }
    }

    public ref T this[uint id]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            T* ptr = (T*)data->ptr;
            return ref *(ptr + id);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(int length)
    {
        data->count += length;
        ResizeToCount();
    }
    public void AddRange(T[] array)
    {
        int last = data->count;
        data->count += array.Length;
        ResizeToCount();
        fixed (void* source = &array[0])
        {
            void* dest = unsafePtr + last;
            UnsafeUtility.MemCpy(dest, source, array.Length * sizeof(T));
        }
    }
    public void AddRange(NativeList<T> array)
    {
        int last = data->count;
        data->count += array.Length;
        ResizeToCount();
        void* dest = unsafePtr + last;
        UnsafeUtility.MemCpy(dest, array.unsafePtr, array.Length * sizeof(T));
    }
    public int ConcurrentAdd(T value)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last <= data->capacity)
        {
            last--;
            T* ptr = (T*)data->ptr;
            *(ptr + last) = value;
            return last;
        }
        return -1;
    }
    public int ConcurrentAdd(ref T value)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last <= data->capacity)
        {
            last--;
            T* ptr = (T*)data->ptr;
            *(ptr + last) = value;
            return last;
        }

        return -1;
    }
    public int ConcurrentAdd(T value, object lockerObj)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last > data->capacity)
        {
            lock (lockerObj)
            {
                if (last > data->capacity)
                {
                    int newCapacity = data->capacity * 2;
                    void* newPtr = UnsafeUtility.Malloc(sizeof(T) * newCapacity, 16, data->allocator);
                    UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(T) * data->capacity);
                    UnsafeUtility.Free(data->ptr, data->allocator);
                    data->ptr = newPtr;
                    data->capacity = newCapacity;
                }
            }
        }
        last--;
        T* ptr = (T*)data->ptr;
        *(ptr + last) = value;
        return last;
    }
    public int ConcurrentAdd(ref T value, object lockerObj)
    {
        int last = Interlocked.Increment(ref data->count);
        //Concurrent Resize
        if (last > data->capacity)
        {
            lock (lockerObj)
            {
                if (last > data->capacity)
                {
                    int newCapacity = data->capacity * 2;
                    void* newPtr = UnsafeUtility.Malloc(sizeof(T) * newCapacity, 16, data->allocator);
                    UnsafeUtility.MemCpy(newPtr, data->ptr, sizeof(T) * data->capacity);
                    UnsafeUtility.Free(data->ptr, data->allocator);
                    data->ptr = newPtr;
                    data->capacity = newCapacity;
                }
            }
        }
        last--;
        T* ptr = (T*)data->ptr;
        *(ptr + last) = value;
        return last;
    }
    public void Add(T value)
    {
        int last = data->count;
        data->count++;
        Resize();
        T* ptr = (T*)data->ptr;
        *(ptr + last) = value;
    }
    public void Add(ref T value)
    {
        int last = data->count;
        data->count++;
        Resize();
        T* ptr = (T*)data->ptr;
        *(ptr + last) = value;
    }
    public void Remove(int i)
    {
        ref int count = ref data->count;
        if (count == 0) return;
        count--;
        this[i] = this[count];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        data->count = 0;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerator<T> GetEnumerator()
    {
        return new ListIenumerator<T>(data);
    }
}

public unsafe class ListIenumerator<T> : IEnumerator<T> where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction]
    private NativeListData* data;
    private int iteIndex;
    public ListIenumerator(NativeListData* dataPtr)
    {
        data = dataPtr;
        iteIndex = -1;
    }
    object IEnumerator.Current
    {
        get
        {
            return ((T*)data->ptr)[iteIndex];
        }
    }

    public T Current
    {
        get
        {
            return ((T*)data->ptr)[iteIndex];
        }
    }

    public bool MoveNext()
    {
        return (++iteIndex < (data->count));
    }

    public void Reset()
    {
        iteIndex = -1;
    }

    public void Dispose()
    {
    }
}