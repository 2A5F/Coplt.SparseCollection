using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.SparseCollection.Internal;

// hint: ListAdd and SetAdd cannot be mixed use
public struct SparseSetInner
{
    #region Consts

    private const int DefaultCap = 64;

    #endregion

    #region Fields

    private int[] m_arr;
    private int m_cap;
    private int m_cur;

    #endregion

    #region Getter

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_cur;
    }

    public Span<SparseId> values
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => packed.Slice(0, m_cur);
    }

    public Span<SparseId> packed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MemoryMarshal.Cast<int, SparseId>(m_arr.AsSpan(0, m_cap * 2));
    }
    public Span<SparseIndex> sparse
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MemoryMarshal.Cast<int, SparseIndex>(m_arr.AsSpan(m_cap * 2));
    }

    #endregion

    #region Ctor

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseSetInner() : this(DefaultCap) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SparseSetInner(int cap)
    {
        if (cap <= 0) cap = DefaultCap;
        m_cap = cap;
        m_arr = new int[cap * 3];
    }

    #endregion

    #region Grow

    /// <summary>
    /// resize cap * 2
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Grow() => Scaling(m_cap * 2);

    /// <summary>
    /// resize
    /// </summary>
    /// <param name="new_cap">must > cap</param>
    public void Scaling(int new_cap)
    {
        if (new_cap <= m_cap) throw new ArgumentOutOfRangeException(nameof(new_cap));
        var new_arr = new int[new_cap * 3];
        values.CopyTo(MemoryMarshal.Cast<int, SparseId>(new_arr));
        sparse.CopyTo(MemoryMarshal.Cast<int, SparseIndex>(new_arr.AsSpan(new_cap * 2)));
        m_arr = new_arr;
        m_cap = new_cap;
    }

    #endregion

    #region Id Index Utils

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsIdOutOfRange(SparseId id) => id.IsEmpty || id.Id < 0 || id.Id >= m_cap;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsIndexOutOfRange(SparseIndex i) => i.Index <= 0 || i.Index > Length;

    #endregion

    #region ListAdd

    /// <param name="id">the id</param>
    /// <returns>the index</returns>
    public SparseIndex ListAdd(out SparseId id)
    {
        var c = m_cur++;
        var v = packed[c];
        if (v.IsEmpty) v = new(c);
        packed[c] = v;
        sparse[v.Id] = c;
        id = v;
        return c;
    }

    #endregion

    #region SetAdd

    /// <returns>the index</returns>
    public SparseIndex SetAdd(SparseId id)
    {
        if (IsIdOutOfRange(id)) throw new IndexOutOfRangeException(nameof(id));
        var c = m_cur++;
        packed[c] = id;
        sparse[id.Id] = c;
        return c;
    }

    #endregion

    #region SetAddOrGetAutoGrow

    /// <returns>the index</returns>
    public SparseIndex SetAddOrGetAutoGrow(SparseId id, out bool old)
    {
        if (IsIdOutOfRange(id)) Scaling((int)Utils.RoundUpToPowerOf2((uint)(id.Id + 1)));
        var i = sparse[id.Id];
        if (i.IsEmpty)
        {
            old = false;
            var c = m_cur++;
            packed[c] = id;
            sparse[id.Id] = c;
            return c;
        }
        else
        {
            old = true;
            return i;
        }
    }

    #endregion

    #region Has

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

    #endregion

    #region Remove

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
        last_i = --m_cur;
        if (last_i != i)
        {
            var last = packed[last_i];
            sparse[last.Id] = i;
            packed[i] = last;
            packed[last_i] = id.Next();
        }
        sparse[id.Id] = default;
    }

    #endregion
}
