/****************************************************
 * Copyright (c) 2023 XuanTools MIT License
 * 
 * UnsafeHelper.cs
 * Provides utility methods for unsafe
*****************************************************/

using System.Runtime.CompilerServices;

namespace XuanTools.CollectionsExtension.LowLevel.Unsafe
{
    public class UnsafeHelper
    {
        private unsafe struct AlignOfPointerHelper
        {
            public byte dummy;

            public void* data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int SizeOfPointer()
        {
            return sizeof(void*);
        }

        public unsafe static int AlignOfPointer()
        {
            return sizeof(AlignOfPointerHelper) - sizeof(void*);
        }
    }
}
