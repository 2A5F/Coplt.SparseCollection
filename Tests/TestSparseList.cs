using Coplt.SparseCollection;

namespace Tests;

public class TestSparseList
{
    [Test]
    public void Test1()
    {
        var list = new SparseList<int>();
        list.Add(123);
        var id = list.Add(456);
        list.Add(789);
        list.RemoveById(id);
        list.Add(456);
        var str = string.Join(", ", list);
        Console.WriteLine(str);
        Assert.That(str, Is.EqualTo("123, 789, 456"));
    }

    [Test]
    public void Test2()
    {
        var list = new SparseList<int>();
        list.Add(123);
        list.Add(456);
        list.Add(789);
        list.RemoveAt(1);
        list.Add(456);
        var str = string.Join(", ", list);
        Console.WriteLine(str);
        Assert.That(str, Is.EqualTo("123, 789, 456"));
    }

    [Test]
    public void Test3()
    {
        var list = new SparseList<int>();
        for (int i = 0; i < 10; i++)
        {
            list.Add(i);
        }
        var str = string.Join(", ", list);
        Console.WriteLine(str);
        Assert.That(str, Is.EqualTo("0, 1, 2, 3, 4, 5, 6, 7, 8, 9"));
    }

    [Test]
    public void Test4()
    {
        var list = new SparseList<int>();
        for (int i = 0; i < 10; i++)
        {
            list.Add(i);
        }
        list.RemoveAt(4);
        list.RemoveAt(4);
        list.RemoveAt(4);
        var id = list.Add(123);
        var str = string.Join(", ", list);
        Console.WriteLine(str);
        Console.WriteLine(id);
        Assert.That(str, Is.EqualTo("0, 1, 2, 3, 7, 5, 6, 123"));
        Assert.That(id.Id, Is.EqualTo(8));
    }
    
    [Test]
    public void Test5()
    {
        var list = new SparseList<int>();
        for (int i = 0; i < 10; i++)
        {
            list.Add(i);
        }
        list.RemoveAt(4);
        list.RemoveAt(4);
        list.RemoveAt(4);
        var str = string.Join(", ", list.Ids);
        Console.WriteLine(str);
        Assert.That(str, Is.EqualTo("0:1, 1:1, 2:1, 3:1, 7:1, 5:1, 6:1"));
    }
    
    [Test]
    public void Test6()
    {
        var list = new SparseList<int>();
        for (int i = 0; i < 10; i++)
        {
            list.Add(i);
        }
        list.RemoveAt(4);
        list.RemoveAt(4);
        list.RemoveAt(4);
        var str = string.Join(", ", list.Entries);
        Console.WriteLine(str);
        Assert.That(str, Is.EqualTo("[0:1, 0], [1:1, 1], [2:1, 2], [3:1, 3], [7:1, 7], [5:1, 5], [6:1, 6]"));
    }
}
