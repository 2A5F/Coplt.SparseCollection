using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Coplt.SparseCollection.Internal;

namespace Coplt.SparseCollection;

/// <summary>
/// A List with O(1) CRUD and continuous memory, no index stability
/// </summary>
public class SparseList<T> : IList<T>, IReadOnlyList<T>
{
    public bool IsReadOnly => false;

    private const int InitCap = 4;

    private T[] _values = null!;
    private SparseSetInner inner;

    public SparseList()
    {
        Init();
    }

    private void Init()
    {
        _values = new T[InitCap];
        inner = new(InitCap);
    }

    private void Grow()
    {
        var new_cap = Cap * 2;
        var new_values = new T[new_cap];
        _values.AsSpan().CopyTo(new_values);
        inner.Scaling(new_cap);
        _values = new_values;
    }

    public int Count => inner.Length;

    public int Cap => _values.Length;
    public Span<T> Values => _values.AsSpan(0, inner.Length);

    public T this[int index]
    {
        get => Values[index];
        set => Values[index] = value;
    }

    /// <inheritdoc cref="ICollection{T}.Add"/>
    /// <returns>id</returns>
    public SparseId Add(T item)
    {
        if (Count >= Cap) Grow();
        var i = inner.ListAdd(out var id);
        _values[i] = item;
        return id;
    }

    /// <summary>
    /// Remove by id
    /// </summary>
    public bool RemoveById(SparseId id)
    {
        if (!inner.RemoveId(id, out var index, out var last_i)) return false;
        _values[index] = _values[last_i];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) _values[last_i] = default!;
        return true;
    }

    public void RemoveAt(int index)
    {
        if (inner.RemoveAt(index, out _, out var last_i))
        {
            _values[index] = _values[last_i];
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) _values[last_i] = default!;
        }
        else throw new Exception("Unreachable");
    }

    void ICollection<T>.Add(T item) => Add(item);
    bool ICollection<T>.Contains(T item) => IndexOf(item) >= 0;
    bool ICollection<T>.Remove(T item)
    {
        var i = IndexOf(item);
        if (i < 0) return false;
        RemoveAt(i);
        return true;
    }

    /// <summary>
    /// Contains id
    /// </summary>
    public bool ContainsId(SparseId id) => inner.HasId(id, out _);

    public bool TryGetValue(SparseId id, out T value)
    {
        if (!inner.HasId(id, out var index))
        {
            value = default!;
            return false;
        }
        value = Values[index];
        return true;
    }

    /// <summary>
    /// Get index by id
    /// </summary>
    public int IndexById(SparseId id)
    {
        if (!inner.HasId(id, out var index))
        {
            return index;
        }
        return -1;
    }

    public int IndexOf(T item)
    {
        var len = Count;
        for (int i = 0; i < len; i++)
        {
            if (EqualityComparer<T>.Default.Equals(item, Values[i])) return i;
        }
        return -1;
    }

    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();

    public void Clear()
    {
        Init();
    }

    public void CopyTo(T[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));

    public void CopyTo(Span<T> target) => Values.CopyTo(target);

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
                if (!self.inner.HasIndex(i, out var id)) return false;
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
                if (!self.inner.HasIndex(i, out var id)) return false;
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
}
