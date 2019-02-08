using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using System;
namespace MPipeline
{
    unsafe struct DictData
    {
        public int capacity;
        public int length;
        public void* start;
        public Allocator alloc;
    }
    public unsafe struct NativeDictionary<K, V> where K : unmanaged where V : unmanaged
    {
        static readonly int stride = sizeof(K) + sizeof(V) + 8;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static V* GetV(K* ptr)
        {
            return (V*)(ptr + 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static K** GetNextPtr(K* ptr)
        {
            ulong num = (ulong)ptr;
            num += (ulong)(sizeof(K) + sizeof(V));
            return (K**)num;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private K** GetK(int index)
        {
            return (K**)data->start + index;
        }
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return data->length; }
        }
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return data->capacity; }
        }
        private DictData* data;
        public bool isCreated { get; private set; }
        public Func<K, K, bool> equalsFunc;
        private void Resize(int targetSize)
        {
            K** newData = (K**)UnsafeUtility.Malloc(targetSize * 8, 16, data->alloc);
            UnsafeUtility.MemClear(newData, targetSize * 8);
            K** oldPtr = (K**)data->start;
            for (int i = 0; i < data->capacity; ++i)
            {
                K* currentPtr = oldPtr[i];
                while (currentPtr != null)
                {
                    AddTo(*currentPtr, *GetV(currentPtr), targetSize, newData);
                    currentPtr = *GetNextPtr(currentPtr);
                }
                currentPtr = oldPtr[i];
                while (currentPtr != null)
                {
                    K* next = *GetNextPtr(currentPtr);
                    UnsafeUtility.Free(currentPtr, data->alloc);

                    currentPtr = next;
                }
            }
            UnsafeUtility.Free(data->start, data->alloc);
            data->start = newData;
            data->capacity = targetSize;
        }

        public NativeDictionary(int capacity, Allocator alloc, Func<K, K, bool> equals)
        {
            capacity = Mathf.Max(capacity, 1);
            equalsFunc = equals;
            isCreated = true;
            data = (DictData*)UnsafeUtility.Malloc(sizeof(DictData), 16, alloc);
            data->capacity = capacity;
            data->length = 0;
            data->alloc = alloc;
            data->start = UnsafeUtility.Malloc(8 * capacity, 16, alloc);
            UnsafeUtility.MemClear(data->start, 8 * capacity);
        }

        private void AddTo(K key, V value, int capacity, K** origin)
        {
            int index = Mathf.Abs(key.GetHashCode()) % capacity;
            K** currentPos = origin + index;
            while ((*currentPos) != null)
            {
                currentPos = GetNextPtr(*currentPos);
            }
            (*currentPos) = (K*)UnsafeUtility.Malloc(stride, 16, data->alloc);
            (**currentPos) = key;
            (*GetV(*currentPos)) = value;
            (*GetNextPtr(*currentPos)) = null;
        }

        public void Remove(K key)
        {
            int index = Mathf.Abs(key.GetHashCode()) % data->capacity;
            K** currentPtr = GetK(index);
            while ((*currentPtr) != null)
            {
                K** next = GetNextPtr(*currentPtr);
                if (equalsFunc(**currentPtr, key))
                {
                    K* prev = *currentPtr;
                    *currentPtr = *next;
                    UnsafeUtility.Free(prev, data->alloc);
                    data->length--;
                    return;
                }
                else
                {
                    currentPtr = next;
                }
            }
            Debug.Log("Not found " + key);
        }

        public bool Contains(K key)
        {
            int index = Mathf.Abs(key.GetHashCode()) % data->capacity;
            K** currentPos = GetK(index);
            while ((*currentPos) != null)
            {
                if (equalsFunc(**currentPos, key))
                {
                    return true;
                }
                currentPos = GetNextPtr(*currentPos);
            }
            return false;
        }

        public V this[K key]
        {
            get
            {
                int index = Mathf.Abs(key.GetHashCode()) % data->capacity;
                K** currentPos = GetK(index);
                while ((*currentPos) != null)
                {
                    if (equalsFunc(**currentPos, key))
                    {
                        return *GetV(*currentPos);
                    }
                    currentPos = GetNextPtr(*currentPos);
                }
                return default;
            }
            set
            {
                int hashCode = key.GetHashCode();
                hashCode = Mathf.Abs(hashCode);
                int index = hashCode % data->capacity;
                K** currentPos = GetK(index);
                while ((*currentPos) != null)
                {
                    if (equalsFunc(**currentPos, key))
                    {
                        *GetV(*currentPos) = value;
                        return;
                    }
                    currentPos = GetNextPtr(*currentPos);
                }
                Add(ref key, ref value, hashCode);
            }
        }

        public void Add(K key, V value)
        {
            Add(ref key, ref value, Mathf.Abs(key.GetHashCode()));
        }

        private void Add(ref K key, ref V value, int hashCode)
        {
            if (data->capacity <= data->length)
            {
                Resize(Mathf.Max(data->length + 1, (int)(data->length * 1.5f)));
            }
            int index = hashCode % data->capacity;
            K** currentPos = GetK(index);
            while ((*currentPos) != null)
            {
                currentPos = GetNextPtr(*currentPos);
            }
            (*currentPos) = (K*)UnsafeUtility.Malloc(stride, 16, data->alloc);
            (**currentPos) = key;
            (*GetV(*currentPos)) = value;
            (*GetNextPtr(*currentPos)) = null;
            data->length++;
        }

        public void Dispose()
        {
            Allocator alloc = data->alloc;
            for (int i = 0; i < data->capacity; ++i)
            {
                K* currentPtr = *GetK(i);
                while (currentPtr != null)
                {
                    K* next = *GetNextPtr(currentPtr);
                    UnsafeUtility.Free(currentPtr, alloc);
                    currentPtr = next;
                }
            }
            UnsafeUtility.Free(data->start, alloc);
            UnsafeUtility.Free(data, alloc);
            isCreated = false;
        }

        public bool Get(K key, out V value)
        {
            int index = Mathf.Abs(key.GetHashCode()) % data->capacity;
            K** currentPos = GetK(index);
            while ((*currentPos) != null)
            {
                if (equalsFunc(**currentPos, key))
                {
                    value = *GetV(*currentPos);
                    return true;
                }
                currentPos = GetNextPtr(*currentPos);
            }
            value = default;
            return false;
        }
    }
}
