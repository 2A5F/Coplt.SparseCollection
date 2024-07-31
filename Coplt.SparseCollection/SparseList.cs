using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Coplt.SparseCollection.Internal;

namespace Coplt.SparseCollection;

/// <summary>
/// An unordered List with O(1) CRUD and continuous memory, no index stability
/// </summary>
public class SparseList<T> : IList<T>, IReadOnlyList<T>
{
    #region Consts

    private const int InitCap = 4;

    #endregion

    #region Fields

    private T[] m_values = null!;
    private SparseSetInner m_inner;

    #endregion

    #region Ctor

    public SparseList()
    {
        Init();
    }

    #endregion

    #region Init

    private void Init()
    {
        m_values = Utils.AllocateUninitializedArray<T>(InitCap);
        m_inner = new(InitCap);
    }

    #endregion

    #region Grow

    private void Grow()
    {
        var new_cap = Cap * 2;
        var new_values = new T[new_cap];
        m_values.AsSpan().CopyTo(new_values);
        m_inner.Scaling(new_cap);
        m_values = new_values;
    }

    #endregion

    #region Getter

    public int Count => m_inner.Length;

    public int Cap => m_values.Length;
    public Span<T> Values => m_values.AsSpan(0, m_inner.Length);

    public T this[int index]
    {
        get => Values[index];
        set => Values[index] = value;
    }

    #endregion

    #region Add

    /// <inheritdoc cref="ICollection{T}.Add"/>
    /// <returns>id</returns>
    public SparseId Add(T item)
    {
        if (Count >= Cap) Grow();
        var i = m_inner.ListAdd(out var id);
        m_values[i] = item;
        return id;
    }

    #endregion

    #region Remove

    /// <summary>
    /// Remove by id
    /// </summary>
    public bool RemoveById(SparseId id)
    {
        if (!m_inner.RemoveId(id, out var index, out var last_i)) return false;
        m_values[index] = m_values[last_i];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) m_values[last_i] = default!;
        return true;
    }

    public void RemoveAt(int index)
    {
        if (m_inner.RemoveAt(index, out _, out var last_i))
        {
            m_values[index] = m_values[last_i];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) m_values[last_i] = default!;
        }
        else throw new UnreachableException();
    }

    #endregion

    #region ContainsId

    /// <summary>
    /// Contains id
    /// </summary>
    public bool ContainsId(SparseId id) => m_inner.HasId(id, out _);

    #endregion

    #region TryGetValue

    public bool TryGetValue(SparseId id, out T value)
    {
        if (!m_inner.HasId(id, out var index))
        {
            value = default!;
            return false;
        }
        value = Values[index];
        return true;
    }

    #endregion

    #region IndexById IdByIndex

    /// <summary>
    /// Get index by id
    /// </summary>
    public int IndexById(SparseId id)
    {
        if (m_inner.HasId(id, out var index)) return index;
        return -1;
    }

    /// <summary>
    /// Get id by index
    /// </summary>
    public SparseId IdByIndex(int index)
    {
        if (m_inner.HasIndex(index, out var id)) return id;
        return default;
    }

    #endregion

    #region IndexOf

    public int IndexOf(T item)
    {
        var len = Count;
        for (int i = 0; i < len; i++)
        {
            if (EqualityComparer<T>.Default.Equals(item, Values[i])) return i;
        }
        return -1;
    }

    #endregion

    #region Clear

    public void Clear()
    {
        Init();
    }

    #endregion

    #region CopyTo

    public void CopyTo(T[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));

    public void CopyTo(Span<T> target) => Values.CopyTo(target);

    #endregion

    #region ICollection

    bool ICollection<T>.IsReadOnly => false;
    void ICollection<T>.Add(T item) => Add(item);
    bool ICollection<T>.Contains(T item) => IndexOf(item) >= 0;
    bool ICollection<T>.Remove(T item)
    {
        var i = IndexOf(item);
        if (i < 0) return false;
        RemoveAt(i);
        return true;
    }

    #endregion

    #region IList

    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

    #endregion

    #region Enumerator

    public IdsEnumerator Ids => new(this);
    public EntryEnumerator Entries => new(this);
    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(SparseList<T> self) : IEnumerator<T>
    {
        private int i = 0;

        public bool MoveNext()
        {
            if (i < self.Count)
            {
                i++;
                return true;
            }
            return false;
        }
        public void Reset()
        {
            i = 0;
        }
        public T Current => self[i - 1];

        object IEnumerator.Current => Current!;

        public void Dispose() { }
    }

    public struct IdsEnumerator(SparseList<T> self) : IEnumerator<SparseId>, IEnumerable<SparseId>
    {
        private int i = 0;

        public bool MoveNext()
        {
            if (i < self.Count)
            {
                if (!self.m_inner.HasIndex(i, out var id)) return false;
                Current = id;
                i++;
                return true;
            }
            return false;
        }
        public void Reset()
        {
            i = 0;
        }
        public SparseId Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose() { }

        IEnumerator<SparseId> IEnumerable<SparseId>.GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
    }

    public struct EntryEnumerator(SparseList<T> self)
        : IEnumerator<KeyValuePair<SparseId, T>>, IEnumerable<KeyValuePair<SparseId, T>>
    {
        private int i = 0;

        public bool MoveNext()
        {
            if (i < self.Count)
            {
                if (!self.m_inner.HasIndex(i, out var id)) return false;
                Current = new(id, self.Values[i]);
                i++;
                return true;
            }
            return false;
        }
        public void Reset()
        {
            i = 0;
        }
        public KeyValuePair<SparseId, T> Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose() { }

        IEnumerator<KeyValuePair<SparseId, T>> IEnumerable<KeyValuePair<SparseId, T>>.GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
    }

    #endregion
}
