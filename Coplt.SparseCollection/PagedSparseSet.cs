using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Coplt.SparseCollection.Internal;

namespace Coplt.SparseCollection;

public class PagedSparseSet<T> : IDictionary<SparseId, T>
{
    #region Consts

    private const int DefaultPageSize = 64;
    private const int InitCap = 1;

    #endregion

    #region Fields

    private T[]?[] m_pages;
    public SparseSetInner m_inner;
    private readonly int m_page_size;

    #endregion

    #region Ctor

    public PagedSparseSet(int pageSize = DefaultPageSize)
    {
        if (pageSize < 4) throw new ArgumentException("Page size is too small", nameof(pageSize));
        m_page_size = pageSize;
        m_inner = new(InitCap * pageSize);
        m_pages = [];
        Resize(InitCap);
    }

    #endregion

    #region Getter

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_inner.Length;
    }

    #endregion

    #region Resize

    private void Resize(int target_size)
    {
        target_size = (int)Utils.RoundUpToPowerOf2((uint)target_size);
        var len = m_pages.Length;
        if (target_size <= len) return;
        var old_pages = m_pages;
        var new_pages = new T[]?[target_size];
        old_pages.CopyTo(new_pages, 0);
        m_pages = new_pages;
    }

    #endregion

    #region Inetnal

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SparseIndex SetInternal(SparseId id, T value, out bool is_old, out T old_value,
        bool need_old_value, bool replace)
    {
        Unsafe.SkipInit(out old_value);
        var raw_index = m_inner.SetAddOrGetAutoGrow(id, out is_old);
        int index = raw_index;
        var page_index = Math.DivRem(index, m_page_size, out var index_in_page);
        if (is_old)
        {
            ref var page = ref m_pages[page_index];
            ref var slot = ref page![index_in_page];
            if (need_old_value) old_value = slot;
            if (replace) slot = value;
        }
        else
        {
            if (page_index >= m_pages.Length) Resize(page_index + 1);
            ref var page = ref m_pages[page_index];
            page ??= Utils.AllocateUninitializedArray<T>(m_page_size);
            page[index_in_page] = value;
        }
        return raw_index;
    }

    #endregion

    #region SetOrReplace

    public SparseIndex SetOrReplace(SparseId id, T value) =>
        SetInternal(id, value, out _, out Unsafe.NullRef<T>(), false, true);

    public SparseIndex SetOrReplace(SparseId id, T value, out bool has_old) =>
        SetInternal(id, value, out has_old, out Unsafe.NullRef<T>(), false, true);

    public SparseIndex SetOrReplace(SparseId id, T value, out bool has_old, out T old_value) =>
        SetInternal(id, value, out has_old, out old_value, true, true);

    #endregion

    #region Add

    public SparseIndex Add(SparseId id, T value)
    {
        var i = SetInternal(id, value, out var old, out Unsafe.NullRef<T>(), false, false);
        if (old) throw new ArgumentException("Duplicate Key");
        return i;
    }

    public bool TryAdd(SparseId id, T value, out SparseIndex index)
    {
        index = SetInternal(id, value, out var old, out Unsafe.NullRef<T>(), false, false);
        return !old;
    }

    #endregion

    #region Get

    public bool UnsafeTryGetPage(SparseId id, out SparseIndex index,
        out int index_in_page, [NotNullWhen(true)] out T[]? page)
    {
        if (!m_inner.HasId(id, out index))
        {
            page = null;
            index_in_page = 0;
            return false;
        }
        int i = index;
        var page_index = Math.DivRem(i, m_page_size, out index_in_page);
        page = m_pages[page_index]!;
        return true;
    }

    public ref T TryGetValueRef(SparseId id, out SparseIndex index, out bool success)
    {
        if (!m_inner.HasId(id, out index))
        {
            success = false;
            return ref Unsafe.NullRef<T>();
        }
        int i = index;
        var page_index = Math.DivRem(i, m_page_size, out var index_in_page);
        ref var page = ref m_pages[page_index];
        ref var slot = ref page![index_in_page];
        success = true;
        return ref slot!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T TryGetValueRef(SparseId id, out bool success) => ref TryGetValueRef(id, out _, out success);

    public bool TryGetValue(SparseId id, out SparseIndex index, out T value)
    {
        Unsafe.SkipInit(out value);
        if (!m_inner.HasId(id, out index)) return false;
        int i = index;
        var page_index = Math.DivRem(i, m_page_size, out var index_in_page);
        ref var page = ref m_pages[page_index];
        ref var slot = ref page![index_in_page];
        value = slot;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(SparseId id, out T value) => TryGetValue(id, out _, out value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(SparseId id)
    {
        ref var r = ref TryGetValueRef(id, out var success);
        if (!success) throw new ArgumentOutOfRangeException(nameof(id));
        return ref r!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Get(SparseId id)
    {
        if (!TryGetValue(id, out var value)) throw new ArgumentOutOfRangeException(nameof(id));
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetAt(int index) => GetRefAt(index);

    public ref T GetRefAt(int index)
    {
        if (index < 0 || index > Count) throw new ArgumentOutOfRangeException(nameof(index));
        var page_index = Math.DivRem(index, m_page_size, out var index_in_page);
        ref var page = ref m_pages[page_index];
        ref var slot = ref page![index_in_page];
        return ref slot!;
    }

    #endregion

    #region Contains

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsId(SparseId id) => m_inner.HasId(id, out _);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsIndex(SparseIndex index) => m_inner.HasIndex(index, out _);

    #endregion

    #region Remove

    public bool Remove(SparseId id)
    {
        if (!m_inner.RemoveId(id, out var index, out var last_i)) return false;
        var dst_page_index = Math.DivRem(index, m_page_size, out var dst_index_in_page);
        var src_page_index = Math.DivRem(last_i, m_page_size, out var src_index_in_page);
        ref var dst_page = ref m_pages[dst_page_index];
        ref var dst_slot = ref dst_page![dst_index_in_page];
        ref var src_page = ref m_pages[src_page_index];
        ref var src_slot = ref src_page![src_index_in_page];
        dst_slot = src_slot;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) src_slot = default!;
        return true;
    }

    public void RemoveAt(int index)
    {
        if (m_inner.RemoveAt(index, out _, out var last_i))
        {
            var dst_page_index = Math.DivRem(index, m_page_size, out var dst_index_in_page);
            var src_page_index = Math.DivRem(last_i, m_page_size, out var src_index_in_page);
            ref var dst_page = ref m_pages[dst_page_index];
            ref var dst_slot = ref dst_page![dst_index_in_page];
            ref var src_page = ref m_pages[src_page_index];
            ref var src_slot = ref src_page![src_index_in_page];
            dst_slot = src_slot;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) src_slot = default!;
        }
        else throw new UnreachableException();
    }

    #endregion

    #region IndexById IdByIndex

    /// <summary>
    /// Get index by id
    /// </summary>
    public int IndexById(SparseId id)
    {
        if (m_inner.HasId(id, out var index))
        {
            return index;
        }
        return -1;
    }

    /// <summary>
    /// Get id by index
    /// </summary>
    public SparseId IdByIndex(int index)
    {
        if (m_inner.HasIndex(index, out var id))
        {
            return id;
        }
        return default;
    }

    #endregion

    #region Index

    public T this[SparseId id]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TryGetValue(id, out var value) ? value : throw new ArgumentOutOfRangeException(nameof(id));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => SetOrReplace(id, value);
    }

    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref GetRefAt(index);
    }

    #endregion

    #region Clear

    public void Clear()
    {
        m_inner = new(InitCap * m_page_size);
        m_pages = [];
        Resize(InitCap);
    }

    #endregion

    #region CopyTo

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ICollection<KeyValuePair<SparseId, T>>.CopyTo(KeyValuePair<SparseId, T>[] array, int arrayIndex) =>
        CopyTo(array.AsSpan(arrayIndex));

    private void CopyTo(Span<KeyValuePair<SparseId, T>> span)
    {
        var i = 0;
        foreach (var kv in this.Entries)
        {
            span[i] = kv;
            i++;
        }
    }

    #endregion

    #region Interface

    void IDictionary<SparseId, T>.Add(SparseId id, T value) => Add(id, value);

    bool ICollection<KeyValuePair<SparseId, T>>.Contains(KeyValuePair<SparseId, T> item)
    {
        if (!TryGetValue(item.Key, out var value)) return false;
        return EqualityComparer<T>.Default.Equals(item.Value, value);
    }

    bool IDictionary<SparseId, T>.ContainsKey(SparseId key) => ContainsId(key);

    void ICollection<KeyValuePair<SparseId, T>>.Add(KeyValuePair<SparseId, T> item) => Add(item.Key, item.Value);
    bool ICollection<KeyValuePair<SparseId, T>>.Remove(KeyValuePair<SparseId, T> item) => Remove(item.Key);

    bool ICollection<KeyValuePair<SparseId, T>>.IsReadOnly => false;

    #endregion

    #region Enumerator

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    public EntriesCollection Entries
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    public IdsCollection Ids
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    public ValuesCollection Values
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    public PagesCollection Pages
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    ICollection<SparseId> IDictionary<SparseId, T>.Keys => new IdsCollection(this);
    ICollection<T> IDictionary<SparseId, T>.Values => new ValuesCollection(this);

    IEnumerator<KeyValuePair<SparseId, T>> IEnumerable<KeyValuePair<SparseId, T>>.GetEnumerator()
        => new EntryEnumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new EntryEnumerator(this);

    public ref struct Enumerator(PagedSparseSet<T> self)
    {
        private int i = self.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (i > 0)
            {
                i--;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            i = self.Count;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref self[i];
        }
    }

    public readonly struct IdsCollection(PagedSparseSet<T> self) : ICollection<SparseId>
    {
        public IdEnumerator GetEnumerator() => new(self);
        IEnumerator<SparseId> IEnumerable<SparseId>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(SparseId item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(SparseId item) => throw new NotSupportedException();
        public void CopyTo(SparseId[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(SparseId item) => throw new NotSupportedException();
        public int Count => self.Count;
        public bool IsReadOnly => true;
    }

    public struct IdEnumerator(PagedSparseSet<T> self) : IEnumerator<SparseId>
    {
        private int i = self.Count;

        public bool MoveNext()
        {
            if (i > 0)
            {
                i--;
                return true;
            }
            return false;
        }
        public void Reset()
        {
            i = self.Count;
        }
        public SparseId Current => self.IdByIndex(i);
        object IEnumerator.Current => Current;
        public void Dispose() { }
    }

    public readonly struct ValuesCollection(PagedSparseSet<T> self) : ICollection<T>
    {
        public ValuesEnumerator GetEnumerator() => new(self);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(T item) => throw new NotSupportedException();
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
        public int Count => self.Count;
        public bool IsReadOnly => true;
    }

    public struct ValuesEnumerator(PagedSparseSet<T> self) : IEnumerator<T>
    {
        private int i = self.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (i > 0)
            {
                i--;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            i = self.Count;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => self[i];
        }

        object IEnumerator.Current => Current!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
    }

    public readonly struct EntriesCollection(PagedSparseSet<T> self) : ICollection<KeyValuePair<SparseId, T>>
    {
        public EntryEnumerator GetEnumerator() => new(self);

        IEnumerator<KeyValuePair<SparseId, T>> IEnumerable<KeyValuePair<SparseId, T>>.GetEnumerator() =>
            GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(KeyValuePair<SparseId, T> item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(KeyValuePair<SparseId, T> item) => throw new NotSupportedException();
        public void CopyTo(KeyValuePair<SparseId, T>[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(KeyValuePair<SparseId, T> item) => throw new NotSupportedException();
        public int Count => self.Count;
        public bool IsReadOnly => true;
    }

    public struct EntryEnumerator(PagedSparseSet<T> self) : IEnumerator<KeyValuePair<SparseId, T>>
    {
        private int i = self.Count;

        public bool MoveNext()
        {
            if (i > 0)
            {
                i--;
                return true;
            }
            return false;
        }
        public void Reset()
        {
            i = self.Count;
        }
        public KeyValuePair<SparseId, T> Current
        {
            get
            {
                var v = self[i];
                var id = self.IdByIndex(i);
                return new(id, v);
            }
        }
        object IEnumerator.Current => Current;
        public void Dispose() { }
    }

    public readonly struct PagesCollection(PagedSparseSet<T> self) : ICollection<SparsePage<T>>
    {
        public PageEnumerator GetEnumerator() => new(self);

        IEnumerator<SparsePage<T>> IEnumerable<SparsePage<T>>.GetEnumerator() =>
            GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(SparsePage<T> item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(SparsePage<T> item) => throw new NotSupportedException();
        public void CopyTo(SparsePage<T>[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(SparsePage<T> item) => throw new NotSupportedException();
        public int Count => self.Count / self.m_page_size;
        public bool IsReadOnly => true;
    }

    public struct PageEnumerator(PagedSparseSet<T> self) : IEnumerator<SparsePage<T>>
    {
        private int i = self.Count / self.m_page_size;

        public bool MoveNext()
        {
            if (i > 0)
            {
                i--;
                return true;
            }
            return false;
        }
        public void Reset()
        {
            i = self.Count / self.m_page_size;
        }
        public SparsePage<T> Current
        {
            get
            {
                var page = self.m_pages[i]!;
                var end = Math.Min((i + 1) * self.m_page_size, self.Count) - i * self.m_page_size;
                return new(page, end);
            }
        }
        object IEnumerator.Current => Current;
        public void Dispose() { }
    }

    #endregion
}
