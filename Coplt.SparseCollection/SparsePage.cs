using System;
using System.Runtime.CompilerServices;

namespace Coplt.SparseCollection;

public readonly record struct SparsePage<T>(T[] Datas, int Length)
{
    public T[] Datas { get; } = Datas;
    public int Length { get; } = Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(SparsePage<T> s) => s.AsSpan();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan() => Datas.AsSpan(0, Length);
}
