using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.SparseCollection.Internal;

[StructLayout(LayoutKind.Explicit, Size = sizeof(uint))]
public readonly record struct SparseIndex : IComparable<SparseIndex>, IComparable
{
    [FieldOffset(0)]
    public readonly int Index;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SparseIndex(int Index)
    {
        this.Index = Index;
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Index == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SparseIndex(int Index) => new(Index + 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(SparseIndex Index) => Index.Index - 1;

    public override string ToString() => IsEmpty ? "Empty" : (Index - 1).ToString();

    #region CompareTo

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(SparseIndex other) => Index.CompareTo(other.Index);
    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is SparseIndex other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(SparseIndex)}");
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(SparseIndex left, SparseIndex right) => left.CompareTo(right) < 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(SparseIndex left, SparseIndex right) => left.CompareTo(right) > 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(SparseIndex left, SparseIndex right) => left.CompareTo(right) <= 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(SparseIndex left, SparseIndex right) => left.CompareTo(right) >= 0;

    #endregion
}
