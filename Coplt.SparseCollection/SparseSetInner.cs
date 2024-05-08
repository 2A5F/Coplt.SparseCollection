using System;

namespace Coplt.SparseCollection.Internal;

// hint: ListAdd and SetAdd cannot be mixed use
public struct SparseSetInner
{
    private const int DefaultCap = 64;

    private int[] arr;
    private int cap;
    private int cur;

    public int Length => cur;

    public Span<int> values => packed.Slice(0, cur);

    public Span<int> packed => arr.AsSpan(0, cap);
    public Span<int> sparse => arr.AsSpan(cap);

    public SparseSetInner() : this(DefaultCap) { }

    public SparseSetInner(int cap)
    {
        if (cap <= 0) cap = DefaultCap;
        this.cap = cap;
        arr = new int[cap * 2];
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
        var new_arr = new int[new_cap * 2];
        values.CopyTo(new_arr.AsSpan(0, new_cap));
        sparse.CopyTo(new_arr.AsSpan(new_cap));
        arr = new_arr;
        cap = new_cap;
    }

    /// <param name="id">the id</param>
    /// <returns>the index</returns>
    public int ListAdd(out int id)
    {
        var c = cur++;
        var v = packed[c];
        if (v == 0) v = c;
        else v -= 1;
        packed[c] = v + 1;
        sparse[v] = c + 1;
        id = v;
        return c;
    }

    /// <returns>the index</returns>
    public int SetAdd(int id)
    {
        if (id < 0 || id >= cap) throw new IndexOutOfRangeException(nameof(id));
        var c = cur++;
        packed[c] = id + 1;
        sparse[id] = c + 1;
        return c;
    }

    /// <param name="id">the id</param>
    /// <param name="i">the index</param>
    public bool HasId(int id, out int i)
    {
        if (id < 0 || id >= cap) throw new IndexOutOfRangeException(nameof(id));
        i = sparse[id];
        if (i == 0) return false;
        i--;
        return true;
    }

    /// <param name="i">the index</param>
    /// <param name="id">the id</param>
    public bool HasIndex(int i, out int id)
    {
        if (i < 0 || i >= Length) throw new IndexOutOfRangeException(nameof(i));
        id = packed[i];
        if (id == 0) return false;
        id--;
        return true;
    }

    /// <summary>remove and swap value at i and last_i</summary>
    /// <param name="id">the id</param>
    /// <param name="i">the index</param>
    /// <param name="last_i">the last index</param>
    public bool RemoveId(int id, out int i, out int last_i)
    {
        if (!HasId(id, out i))
        {
            last_i = 0;
            return false;
        }
        DoRemove(i, id, out last_i);
        return true;
    }

    /// <summary>remove and swap value at i and last_i</summary>
    /// <param name="i">the index</param>
    /// <param name="id">the id</param>
    /// <param name="last_i">the last index</param>
    public bool RemoveAt(int i, out int id, out int last_i)
    {
        if (!HasIndex(i, out id))
        {
            last_i = 0;
            return false;
        }
        DoRemove(i, id, out last_i);
        return true;
    }

    public void DoRemove(int i, int id, out int last_i)
    {
        last_i = --cur;
        if (last_i != i)
        {
            var last = packed[last_i] - 1;
            sparse[last] = i + 1;
            packed[i] = last + 1;
            packed[last_i] = id + 1;
        }
        sparse[id] = 0;
    }
}
