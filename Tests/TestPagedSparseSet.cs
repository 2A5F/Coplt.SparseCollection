using Coplt.SparseCollection;

namespace Tests;

public class TestPagedSparseSet
{
    [Test]
    public void Test1()
    {
        var set = new PagedSparseSet<int>();
        var id3 = new SparseId(3);
        set.Add(id3, 123);
        var id123 = new SparseId(123);
        set.Add(id123, 456);
        Assert.That(set.Count, Is.EqualTo(2));
        set.Remove(id3);
        var id789 = new SparseId(789);
        set.Add(id789, 666);
        var r = new HashSet<int>();
        Assert.That(set.Count, Is.EqualTo(2));
        foreach (var val in set)
        {
            r.Add(val);
            Console.WriteLine(val);
        }
        CollectionAssert.AreEqual(r, new[] { 666, 456 });
    }

    [Test]
    public void Test2()
    {
        var set = new PagedSparseSet<int>();
        var id3 = new SparseId(3);
        set.Add(id3, 123);
        var id123 = new SparseId(123);
        set.Add(id123, 456);
        Assert.That(set.Count, Is.EqualTo(2));
        set.Remove(id3);
        var id789 = new SparseId(789);
        set.Add(id789, 666);
        var str = string.Join(", ", set);
        Console.WriteLine(str);
        var r = set.Values.ToHashSet();
        CollectionAssert.AreEqual(r, new[] { 666, 456 });
    }

    [Test]
    public void Test3()
    {
        var set = new PagedSparseSet<int>();
        for (int i = 0; i < 10; i++)
        {
            set.Add(new SparseId(i), i);
        }
        var str = string.Join(", ", set);
        Console.WriteLine(str);
        var r = set.Values.ToHashSet();
        Assert.That(r, Is.EqualTo(new[] { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }));
    }

    [Test]
    public void Test4()
    {
        var set = new PagedSparseSet<int>();
        for (int i = 0; i < 1000; i++)
        {
            set.Add(new SparseId(i), i);
        }
        var str = string.Join(", ", set);
        Console.WriteLine(str);
        var r = set.Values.ToHashSet();
        Assert.That(r.Count, Is.EqualTo(1000));
    }

    [Test]
    public void Test5()
    {
        var list = new SparseList<int>();
        var set = new PagedSparseSet<string>();
        for (int i = 0; i < 1000; i++)
        {
            var id = list.Add(i);
            set.Add(id, $"{i}");
        }
        var str = string.Join(", ", set);
        Console.WriteLine(str);
        var r = set.Values.ToHashSet();
        Assert.That(r.Count, Is.EqualTo(1000));
    }
}
