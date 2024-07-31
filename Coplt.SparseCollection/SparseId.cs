using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.SparseCollection;

[StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
public readonly record struct SparseId : IComparable<SparseId>, IComparable
{
    [FieldOffset(0)]
    public readonly int Id;

    [FieldOffset(sizeof(uint))]
    public readonly int Version;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseId(int Id) : this(Id, 1) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SparseId(int Id, int Version)
    {
        this.Id = Id;
        this.Version = Version;
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Version == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseId Next() => new(Id, Version + 1);

    public override string ToString() => IsEmpty ? "Empty" : $"{Id}:{Version}";

    #region CompareTo

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(SparseId other)
    {
        var idComparison = Id.CompareTo(other.Id);
        if (idComparison != 0) return idComparison;
        return Version.CompareTo(other.Version);
    }
    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is SparseId other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(SparseId)}");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(SparseId left, SparseId right) => left.CompareTo(right) < 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(SparseId left, SparseId right) => left.CompareTo(right) > 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(SparseId left, SparseId right) => left.CompareTo(right) <= 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(SparseId left, SparseId right) => left.CompareTo(right) >= 0;

    #endregion
}
