using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.SparseCollection.Internal;

// hint: ListAdd and SetAdd cannot be mixed use
public struct SparseSetInner
{
    private const int DefaultCap = 64;

    private int[] arr;
    private int cap;
    private int cur;

    public int Length => cur;

    public Span<SparseId> values => packed.Slice(0, cur);

    public Span<SparseId> packed => MemoryMarshal.Cast<int, SparseId>(arr.AsSpan(0, cap * 2));
    public Span<SparseIndex> sparse => MemoryMarshal.Cast<int, SparseIndex>(arr.AsSpan(cap * 2));

    public SparseSetInner() : this(DefaultCap) { }

    public SparseSetInner(int cap)
    {
        if (cap <= 0) cap = DefaultCap;
        this.cap = cap;
        arr = new int[cap * 3];
    }

    /// <summary>
    /// resize cap * 2
    /// </summary>
    public void Grow() => Scaling(cap * 2);

    /// <summary>
    /// resize
    /// </summary>
    /// <param name="new_cap">must > cap</param>
    public void Scaling(int new_cap)
    {
        if (new_cap <= cap) throw new ArgumentOutOfRangeException(nameof(new_cap));
        var new_arr = new int[new_cap * 3];
        values.CopyTo(MemoryMarshal.Cast<int, SparseId>(new_arr));
        sparse.CopyTo(MemoryMarshal.Cast<int, SparseIndex>(new_arr.AsSpan(new_cap * 2)));
        arr = new_arr;
        cap = new_cap;
    }

    /// <param name="id">the id</param>
    /// <returns>the index</returns>
    public SparseIndex ListAdd(out SparseId id)
    {
        var c = cur++;
        var v = packed[c];
        if (v.IsEmpty) v = new(c);
        packed[c] = v;
        sparse[v.Id] = c;
        id = v;
        return c;
    }

    private bool IsIdOutOfRange(SparseId id) => id.IsEmpty || id.Id < 0 || id.Id >= cap;
    private bool IsIndexOutOfRange(SparseIndex i) => i.Index <= 0 || i.Index > Length;

    /// <returns>the index</returns>
    public SparseIndex SetAdd(SparseId id)
    {
        if (IsIdOutOfRange(id)) throw new IndexOutOfRangeException(nameof(id));
        var c = cur++;
        packed[c] = id;
        sparse[id.Id] = c;
        return c;
    }

    /// <param name="id">the id</param>
    /// <param name="i">the index</param>
    public bool HasId(SparseId id, out SparseIndex i)
    {
        if (IsIdOutOfRange(id)) 
        {
            i = default;
            return false;
        }
        i = sparse[id.Id];
        if (i.IsEmpty) return false;
        if (packed[i] != id) return false;
        return true;
    }

    /// <param name="i">the index</param>
    /// <param name="id">the id</param>
    public bool HasIndex(SparseIndex i, out SparseId id)
    {
        if (IsIndexOutOfRange(i))
        {
            id = default;
            return false;
        }
        id = packed[i];
        if (id.IsEmpty) return false;
        return true;
    }

    /// <summary>remove and swap value at i and last_i</summary>
    /// <param name="id">the id</param>
    /// <param name="i">the index</param>
    /// <param name="last_i">the last index</param>
    public bool RemoveId(SparseId id, out SparseIndex i, out SparseIndex last_i)
    {
        if (!HasId(id, out i))
        {
            last_i = default;
            return false;
        }
        DoRemove(i, id, out last_i);
        return true;
    }

    /// <summary>remove and swap value at i and last_i</summary>
    /// <param name="i">the index</param>
    /// <param name="id">the id</param>
    /// <param name="last_i">the last index</param>
    public bool RemoveAt(SparseIndex i, out SparseId id, out SparseIndex last_i)
    {
        if (!HasIndex(i, out id))
        {
            last_i = default;
            return false;
        }
        DoRemove(i, id, out last_i);
        return true;
    }

    public void DoRemove(SparseIndex i, SparseId id, out SparseIndex last_i)
    {
        last_i = --cur;
        if (last_i != i)
        {
            var last = packed[last_i];
            sparse[last.Id] = i;
            packed[i] = last;
            packed[last_i] = id.Next();
        }
        sparse[id.Id] = default;
    }
}
