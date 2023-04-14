/****************************************************
    * License: MIT
    * Name: NativeArray2D.cs
    * Author: LuoxuanLove
    * Function: 
    *     A native container of 2D array (Jagged)
    *     Can be used similarly as NativeArray
*****************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Internal;

namespace Unity.Collections
{
    [NativeContainer]
    [NativeContainerSupportsMinMaxWriteRestriction]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [DebuggerDisplay("SizeX = {SizeX}")]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeArray2D<T> : IDisposable, IEnumerable<NativeArray2D<T>.ReadOnlyNativeArray>, IEnumerable, IEquatable<NativeArray2D<T>> where T : struct
    {
        [ExcludeFromDocs]
        public struct Enumerator : IEnumerator<ReadOnlyNativeArray>, IEnumerator, IDisposable
        {
            private NativeArray2D<T> m_Array;

            private int m_Index;

            public ReadOnlyNativeArray Current => m_Array[m_Index];

            object IEnumerator.Current => Current;

            public Enumerator(ref NativeArray2D<T> array)
            {
                m_Array = array;
                m_Index = -1;
            }

            public void Dispose()
            {

            }

            public bool MoveNext()
            {
                m_Index++;
                return m_Index < m_Array.SizeX;
            }

            public void Reset()
            {
                m_Index = -1;
            }
        }

        [NativeContainer]
        [NativeContainerIsReadOnly]
        [DebuggerDisplay("Length = {Length}")]
        public unsafe struct ReadOnlyNativeArray : IEnumerable<T>, IEnumerable
        {
            [ExcludeFromDocs]
            public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
            {
                private ReadOnlyNativeArray m_Array;

                private int m_Index;

                public T Current => m_Array[m_Index];

                object IEnumerator.Current => Current;

                public Enumerator(in ReadOnlyNativeArray array)
                {
                    m_Array = array;
                    m_Index = -1;
                }

                public void Dispose()
                {

                }

                public bool MoveNext()
                {
                    m_Index++;
                    return m_Index < m_Array.Length;
                }

                public void Reset()
                {
                    m_Index = -1;
                }
            }

            // ��������һ�������ָ�롣
            [NativeDisableUnsafePtrRestriction] internal unsafe void* m_Buffer;

            internal int m_Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // ���ư�ȫ�����
            internal AtomicSafetyHandle m_Safety;
#endif

            public int Length => m_Length;

            public T this[int index]
            {
                get
                {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                    CheckElementReadAccess(index);
#endif
                    return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
                }
            }

            public void CopyTo(T[] array)
            {
                Copy(this, array);
            }

            public static void Copy(ReadOnlyNativeArray src, T[] dst)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
                CheckCopyLengths(src.Length, dst.Length);
#endif
                Copy(src, 0, dst, 0, src.Length);
            }

            public unsafe static void Copy(ReadOnlyNativeArray src, int srcIndex, T[] dst, int dstIndex, int length)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
                if (dst == null)
                    throw new ArgumentNullException("dst");
                CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
#endif
                GCHandle gCHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
                IntPtr intPtr = gCHandle.AddrOfPinnedObject();
                UnsafeUtility.MemCpy((byte*)(void*)intPtr + dstIndex * UnsafeUtility.SizeOf<T>(), (byte*)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>(), length * UnsafeUtility.SizeOf<T>());
                gCHandle.Free();
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private unsafe void CheckElementReadAccess(int index)
            {
                if (index < 0 || index >= m_Length)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range (must be between 0 and {m_Length - 1}).");
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(in this);
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [NativeDisableUnsafePtrRestriction] internal unsafe void** m_Buffer;

        internal int m_SizeX;
        [NativeDisableUnsafePtrRestriction] internal unsafe int* m_SizeY;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // NativeContainerSupportsMinMaxWriteRestriction ����������Բ����Ĵ��ݷ�Χ���а�ȫ��顣
        // ��һ������Job��������������Jobʱ���÷�Χ�����ݸ�������
        internal int m_MinIndex;
        internal int m_MaxIndex;

        internal AtomicSafetyHandle m_Safety;
        [NativeSetClassTypeToNullOnSchedule] internal DisposeSentinel m_DisposeSentinel;
#endif

        internal Allocator m_AllocatorLabel;

        public unsafe bool IsCreated => m_Buffer != null;

        public int SizeX => m_SizeX;
        internal unsafe int* SizeY => m_SizeY;

        public unsafe ReadOnlyNativeArray this[int X]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                CheckRangeAccess(X);
#endif
                return new ReadOnlyNativeArray()
                {
                    m_Buffer = m_Buffer[X],
                    m_Length = m_SizeY[X],
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    m_Safety = m_Safety
#endif
                };
            }
        }

        public unsafe T this[int X, int Y]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
                CheckRangeAccess(X);
#endif
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer[X], Y);
            }
            [WriteAccessRequired]
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
                CheckRangeAccess(X);
#endif
                UnsafeUtility.WriteArrayElement(m_Buffer[X], Y, value);
            }
        }

        public unsafe NativeArray2D(int sizeX, int sizeY, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            var SizeY = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * sizeX, UnsafeUtility.AlignOf<int>(), allocator); ;
            for (int i = 0; i < sizeX; i++)
            {
                SizeY[i] = sizeY;
            }
            Allocate(sizeX, SizeY, allocator, out this);

            // �����Ҫ�����ڴ������Ϊ0��
            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                for (int i = 0; i < SizeX; i++)
                    UnsafeUtility.MemClear(m_Buffer[i], (long)SizeY[i] * UnsafeUtility.SizeOf<T>());
        }

        public unsafe NativeArray2D(int[] size, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (size == null)
                throw new ArgumentNullException("array");
#endif
            var SizeY = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * size.Length, UnsafeUtility.AlignOf<int>(), allocator); ;
            for (int i = 0; i < size.Length; i++)
            {
                SizeY[i] = size[i];
            }
            Allocate(size.Length, SizeY, allocator, out this);

            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
                for (int i = 0; i < SizeX; i++)
                    UnsafeUtility.MemClear(m_Buffer[i], (long)SizeY[i] * UnsafeUtility.SizeOf<T>());
        }

        public NativeArray2D(T[][] array2D, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (array2D == null)
                throw new ArgumentNullException("array");
#endif
            var SizeY = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * array2D.Length, UnsafeUtility.AlignOf<int>(), allocator);
            for (int i = 0; i < array2D.Length; i++)
            {
                SizeY[i] = array2D[i].Length;
            }

            Allocate(array2D.Length, SizeY, allocator, out this);
            Copy(array2D, this);
        }

        public NativeArray2D(NativeArray2D<T> array2D, Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(array2D.m_Safety);
#endif
            var SizeY = (int*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<int>() * array2D.SizeX, UnsafeUtility.AlignOf<int>(), allocator);
            for (int i = 0; i < array2D.SizeX; i++)
            {
                SizeY[i] = array2D[i].Length;
            }

            Allocate(array2D.SizeX, SizeY, allocator, out this);
            Copy(array2D, this);
        }

        private unsafe static void Allocate(int sizeX, int* sizeY, Allocator allocator, out NativeArray2D<T> array2D)
        {
            // ������Ƿ�����Ч�ķ��䡣
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CheckAllocateArguments(sizeX, sizeY, allocator);
#endif

            // ����NativeArray2D�������ڴ�
            array2D = default;
            array2D.m_SizeX = sizeX;
            array2D.m_SizeY = sizeY;
            array2D.m_AllocatorLabel = allocator;
            array2D.m_Buffer = (void**)UnsafeUtility.Malloc(sizeof(void*) * sizeX, sizeof(void*), allocator);
            for (int i = 0; i < sizeX; i++)
            {
                array2D.m_Buffer[i] = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>() * sizeY[i], UnsafeUtility.AlignOf<T>(), allocator);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // Ĭ������£�Job������������Χ�����С�
            array2D.m_MinIndex = 0;
            array2D.m_MaxIndex = sizeX - 1;

            // �����ڴ�й©��
            DisposeSentinel.Create(out array2D.m_Safety, out array2D.m_DisposeSentinel, 1, allocator);
#endif
        }

        public int GetSizeX() => m_SizeX;

        public int GetSizeY(int X) => m_SizeY[X];

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocateArguments(int sizeX, int* sizeY, Allocator allocator)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));

            if (sizeX < 0)
                throw new ArgumentOutOfRangeException(nameof(sizeX), "Length must be >= 0");

            if (sizeX * sizeof(void*) > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(sizeX), $"Length * sizeof(void*) cannot exceed {int.MaxValue} bytes");

            for (var i = 0; i < sizeX; i++)
            {
                if (sizeY[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(sizeY), "Length must be >= 0");

                if (sizeY[i] * UnsafeUtility.SizeOf<T>() > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(sizeY), $"Length * sizeof(T) cannot exceed {int.MaxValue} bytes");
            }

            if (!UnsafeUtility.IsBlittable<T>())
                throw new ArgumentException(string.Format("{0} used in NativeCustomArray<{0}> must be blittable", typeof(T)));

            if (!UnsafeUtility.IsValidNativeContainerElementType<T>())
                throw new InvalidOperationException($"{typeof(T)} used in NativeCustomArray<{typeof(T)}> must be unmanaged (contain no managed types) and cannot itself be a native container type.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckRangeAccess(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // ����Ƿ��ڴ˲���������Job������������Χ�ڡ�
            if (index < m_MinIndex || index > m_MaxIndex)
            {
                if (index < SizeX && (m_MinIndex != 0 || m_MaxIndex != SizeX - 1))
                    throw new IndexOutOfRangeException(string.Format(
                        "Index {0} is out of restricted IJobParallelFor range [{1}...{2}] in ReadWriteBuffer.\n" +
                        "ReadWriteBuffers are restricted to only read & write the element at the job index. " +
                        "You can use double buffering strategies to avoid race conditions due to " +
                        "reading & writing in parallel to the same elements from a job.",
                        index, m_MinIndex, m_MaxIndex));

                // �ⲻ�ǲ���Job����������Ȼ������Χ��
                throw new IndexOutOfRangeException(string.Format("Index {0} is out of range of '{1}' Length.", index, SizeX));
            }
#endif
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // DisposeSentinel ��Ҫ�����߳��������
            DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif

            // ����һ��Job���������ǵ������������ǵ�ָ��ĸ������ݸ�����
            NativeCustomArray2DDisposeJob disposeJob = new()
            {
                Data = new NativeCustomArray2DDispose()
                {
                    m_Buffer = m_Buffer,
                    m_SizeX = m_SizeX,
                    m_SizeY = m_SizeY,
                    m_AllocatorLabel = m_AllocatorLabel
                }
            };
            JobHandle result = disposeJob.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(m_Safety);
#endif

            m_Buffer = null;
            m_SizeX = 0;
            m_SizeY = null;

            return result;
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!UnsafeUtility.IsValidAllocator(m_AllocatorLabel))
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");

            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

            // �ͷŷ�����ڴ沢���ñ�����
            for (int i = 0; i < m_SizeX; i++)
            {
                UnsafeUtility.Free(m_Buffer[i], m_AllocatorLabel);
            }
            UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
            UnsafeUtility.Free(m_SizeY, m_AllocatorLabel);
            m_Buffer = null;
            m_SizeX = 0;
            m_SizeY = null;
        }

        [WriteAccessRequired]
        public void CopyFrom(T[][] array2D)
        {
            Copy(array2D, this);
        }

        [WriteAccessRequired]
        public void CopyFrom(NativeArray2D<T> array2D)
        {
            Copy(array2D, this);
        }

        public void CopyTo(T[][] array2D)
        {
            Copy(this, array2D);
        }

        public void CopyTo(NativeArray2D<T> array2D)
        {
            Copy(this, array2D);
        }

        public T[][] ToArray2D()
        {
            T[][] array2D = new T[m_SizeX][];
            for (int i = 0; i < m_SizeX; i++)
            {
                array2D[i] = new T[m_SizeY[i]];
            }
            Copy(this, array2D);
            return array2D;
        }

        internal int[] ArrayZero => new int[SizeX];

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyLengths(int srcLength, int dstLength)
        {
            if (srcLength != dstLength)
                throw new ArgumentException("source and destination length must be the same");
        }

        public static void Copy(NativeArray2D<T> src, NativeArray2D<T> dst)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
            for (var i = 0; i < dst.SizeX; i++)
            {
                CheckCopyLengths(src.SizeY[i], dst.SizeY[i]);
            }
#endif
            Copy(src, dst.ArrayZero, dst, dst.ArrayZero, dst.SizeY);
        }

        public static void Copy(T[][] src, NativeArray2D<T> dst)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
            for (var i = 0; i < dst.SizeX; i++)
            {
                CheckCopyLengths(src[i].Length, dst.SizeY[i]);
            }
#endif
            Copy(src, dst.ArrayZero, dst, dst.ArrayZero, dst.SizeY);
        }

        public static void Copy(NativeArray2D<T> src, T[][] dst)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            for (var i = 0; i < src.SizeX; i++)
            {
                CheckCopyLengths(src.SizeY[i], dst[i].Length);
            }
#endif
            Copy(src, src.ArrayZero, dst, src.ArrayZero, src.SizeY);
        }

        public static void Copy(NativeArray2D<T> src, NativeArray2D<T> dst, int* length)
        {
            Copy(src, dst.ArrayZero, dst, dst.ArrayZero, length);
        }

        public static void Copy(T[][] src, NativeArray2D<T> dst, int* length)
        {
            Copy(src, dst.ArrayZero, dst, dst.ArrayZero, length);
        }

        public static void Copy(NativeArray2D<T> src, T[][] dst, int* length)
        {
            Copy(src, src.ArrayZero, dst, src.ArrayZero, length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyArguments(int srcLength, int srcIndex, int dstLength, int dstIndex, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "length must be equal or greater than zero.");

            if (srcIndex < 0 || srcIndex > srcLength || (srcIndex == srcLength && srcLength > 0))
                throw new ArgumentOutOfRangeException("srcIndex", "srcIndex is outside the range of valid indexes for the source NativeArray.");

            if (dstIndex < 0 || dstIndex > dstLength || (dstIndex == dstLength && dstLength > 0))
                throw new ArgumentOutOfRangeException("dstIndex", "dstIndex is outside the range of valid indexes for the destination NativeArray.");

            if (srcIndex + length > srcLength)
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source NativeArray.", "length");

            if (srcIndex + length < 0)
                throw new ArgumentException("srcIndex + length causes an integer overflow");

            if (dstIndex + length > dstLength)
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination NativeArray.", "length");

            if (dstIndex + length < 0)
                throw new ArgumentException("dstIndex + length causes an integer overflow");
        }

        public unsafe static void Copy(NativeArray2D<T> src, int[] srcIndex, NativeArray2D<T> dst, int[] dstIndex, int* length)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
#endif

            for (var i = 0; i < dst.SizeX; i++)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                CheckCopyArguments(src[i].Length, srcIndex[i], dst.SizeY[i], dstIndex[i], length[i]);
#endif
                GCHandle gCHandle = GCHandle.Alloc(src[i], GCHandleType.Pinned);
                IntPtr intPtr = gCHandle.AddrOfPinnedObject();
                UnsafeUtility.MemCpy((byte*)dst.m_Buffer[i] + dstIndex[i] * UnsafeUtility.SizeOf<T>(), (byte*)(void*)intPtr + srcIndex[i] * UnsafeUtility.SizeOf<T>(), length[i] * UnsafeUtility.SizeOf<T>());
                gCHandle.Free();
            }
        }

        public unsafe static void Copy(T[][] src, int[] srcIndex, NativeArray2D<T> dst, int[] dstIndex, int* length)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(dst.m_Safety);
            if (src == null)
                throw new ArgumentNullException("src");
#endif

            for (var i = 0; i < dst.SizeX; i++)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (src[i] == null)
                    throw new ArgumentNullException($"src[{i}]");
                CheckCopyArguments(src[i].Length, srcIndex[i], dst.SizeY[i], dstIndex[i], length[i]);
#endif
                GCHandle gCHandle = GCHandle.Alloc(src[i], GCHandleType.Pinned);
                IntPtr intPtr = gCHandle.AddrOfPinnedObject();
                UnsafeUtility.MemCpy((byte*)dst.m_Buffer[i] + dstIndex[i] * UnsafeUtility.SizeOf<T>(), (byte*)(void*)intPtr + srcIndex[i] * UnsafeUtility.SizeOf<T>(), length[i] * UnsafeUtility.SizeOf<T>());
                gCHandle.Free();
            }
        }

        public unsafe static void Copy(NativeArray2D<T> src, int[] srcIndex, T[][] dst, int[] dstIndex, int* length)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(src.m_Safety);
            if (dst == null)
                throw new ArgumentNullException("dst");
#endif

            for (var i = 0; i < src.SizeX; i++)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (dst[i] == null)
                    throw new ArgumentNullException($"dst[{i}]");
                CheckCopyArguments(src.SizeY[i], srcIndex[i], dst[i].Length, dstIndex[i], length[i]);
#endif
                GCHandle gCHandle = GCHandle.Alloc(dst[i], GCHandleType.Pinned);
                IntPtr intPtr = gCHandle.AddrOfPinnedObject();
                UnsafeUtility.MemCpy((byte*)(void*)intPtr + dstIndex[i] * UnsafeUtility.SizeOf<T>(), (byte*)src.m_Buffer[i] + srcIndex[i] * UnsafeUtility.SizeOf<T>(), length[i] * UnsafeUtility.SizeOf<T>());
                gCHandle.Free();
            }
        }

        public IEnumerator<ReadOnlyNativeArray> GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator<ReadOnlyNativeArray> IEnumerable<ReadOnlyNativeArray>.GetEnumerator()
        {
            return new Enumerator(ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(NativeArray2D<T> other)
        {
            if (m_Buffer != other.m_Buffer || m_SizeX != other.m_SizeX)
                return false;
            for (int i = 0; i < m_SizeX; i++)
            {
                if (m_Buffer[i] != other.m_Buffer[i] || m_SizeY[i] != other.m_SizeY[i]) 
                    return false;
            }
            return true;
        }
    }

    [NativeContainer]
    internal unsafe struct NativeCustomArray2DDispose
    {
        // ����ָ��İ�ȫ�ԣ��Ա�Job����������ṹ�����š�
        [NativeDisableUnsafePtrRestriction] internal void** m_Buffer;
        [NativeDisableUnsafePtrRestriction] internal void* m_SizeY;
        internal int m_SizeX;
        internal Allocator m_AllocatorLabel;

        public void Dispose()
        {
            // �ͷŷ�����ڴ�
            for (int i = 0; i < m_SizeX; i++)
            {
                UnsafeUtility.Free(m_Buffer[i], m_AllocatorLabel);
            }
            UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
        }
    }

    internal struct NativeCustomArray2DDisposeJob : IJob
    {
        internal NativeCustomArray2DDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }
}