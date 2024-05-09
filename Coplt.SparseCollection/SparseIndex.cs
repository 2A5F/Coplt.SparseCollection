using System;
using System.Runtime.InteropServices;

namespace Coplt.SparseCollection.Internal;

[StructLayout(LayoutKind.Explicit, Size = sizeof(uint))]
public readonly record struct SparseIndex : IComparable<SparseIndex>, IComparable
{
    [FieldOffset(0)]
    public readonly int Index;

    private SparseIndex(int Index)
    {
        this.Index = Index;
    }

    public bool IsEmpty => Index == 0;

    public static implicit operator SparseIndex(int Index) => new(Index + 1);

    public static implicit operator int(SparseIndex Index) => Index.Index - 1;

    public override string ToString() => IsEmpty ? "Empty" : (Index - 1).ToString();

    #region CompareTo

    public int CompareTo(SparseIndex other)
    {
        return Index.CompareTo(other.Index);
    }
    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is SparseIndex other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(SparseIndex)}");
    }
    public static bool operator <(SparseIndex left, SparseIndex right)
    {
        return left.CompareTo(right) < 0;
    }
    public static bool operator >(SparseIndex left, SparseIndex right)
    {
        return left.CompareTo(right) > 0;
    }
    public static bool operator <=(SparseIndex left, SparseIndex right)
    {
        return left.CompareTo(right) <= 0;
    }
    public static bool operator >=(SparseIndex left, SparseIndex right)
    {
        return left.CompareTo(right) >= 0;
    }

    #endregion
}
