using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Coplt.SparseCollection;

internal static class Utils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint RoundUpToPowerOf2(uint value)
    {
#if NETSTANDARD
        --value;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1U;
#else
        return BitOperations.RoundUpToPowerOf2(value);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] AllocateUninitializedArray<T>(int len)
    {
#if NETSTANDARD
        return new T[len];
#else
        return GC.AllocateUninitializedArray<T>(len);
#endif
    }
}
