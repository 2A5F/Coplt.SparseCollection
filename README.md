# Coplt.SparseCollection

[![.NET](https://github.com/2A5F/Coplt.SparseCollection/actions/workflows/dotnet.yml/badge.svg)](https://github.com/2A5F/Coplt.SparseCollection/actions/workflows/dotnet.yml)
[![Nuget](https://img.shields.io/nuget/v/Coplt.SparseCollection)](https://www.nuget.org/packages/Coplt.SparseCollection/)
![MIT](https://img.shields.io/github/license/2A5F/Coplt.SparseCollection)

Fast, cache friendly, continuous memory sparse collections with CRUD all O(1)

### Example

- `SparseList`

    ```csharp
    var list = new SparseList<int>();
    
    var id = list.Add(123);
    list.Add(456);
    list.Add(789);
    
    if (list.ContainsId(id)) { }
    
    if (list.TryGetValue(id, out var v) { }
    
    var index = list.IndexById(id);
    list[index] = 111;
    
    list.RemoveAt(1);
    list.RemoveById(id);
    
    Span<int> span = list.Values;
    ```

- `PagedSparseSet`

    ```csharp
    var list = new SparseList<int>();
    var set = new PagedSparseSet<string>();
    for (int i = 0; i < 1000; i++)
    {
        var id = list.Add(i);
        set.Add(id, $"{i}");
    }
    ```
