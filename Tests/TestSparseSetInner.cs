using Coplt.SparseCollection.Internal;

namespace Tests;

public class TestSparseSetInner
{
    [Test]
    public void TestSparseSetInnerListAdd()
    {
        var a = new SparseSetInner();
        var ia_0 = a.ListAdd(out var ia);
        var ib_0 = a.ListAdd(out var ib);
        Assert.That(a.HasId(ia, out var ia_1), Is.True);
        Assert.That(a.HasId(ib, out var ib_1), Is.True);
        Assert.That(a.RemoveId(ia, out var ia_2, out var ia_3), Is.True);
        Assert.That(a.RemoveId(ib, out var ib_2, out var ib_3), Is.True);
        var ic_0 = a.ListAdd(out var ic);
        Assert.That(a.HasId(ic, out var ic_1), Is.True);
        Console.WriteLine($"{ia}, {ia_0}, {ia_1}, {ia_2}, {ia_3}");
        Console.WriteLine($"{ib}, {ib_0}, {ib_1}, {ib_2}, {ib_3}");
        Console.WriteLine($"{ic}, {ic_0}, {ic_1}");
    }

    [Test]
    public void TestSparseSetInnerSetAdd()
    {
        var a = new SparseSetInner();
        var i3_0 = a.SetAdd(3);
        var i5_0 = a.SetAdd(5);
        Assert.That(a.HasId(3, out var i3_1), Is.True);
        Assert.That(a.HasId(5, out var i5_1), Is.True);
        Assert.That(a.RemoveId(3, out var i3_2, out var i3_3), Is.True);
        Assert.That(a.RemoveId(5, out var i5_2, out var i5_3), Is.True);
        Console.WriteLine($"{i3_0}, {i3_1}, {i3_2}, {i3_3}");
        Console.WriteLine($"{i5_0}, {i5_1}, {i5_2}, {i5_3}");
    }

    [Test]
    public void TestSparseSetInnerGrow()
    {
        var a = new SparseSetInner(8);
        a.SetAdd(3);
        a.SetAdd(5);
        Assert.That(a.HasId(3, out _), Is.True);
        Assert.That(a.HasId(5, out _), Is.True);
        a.Grow();
        Assert.That(a.HasId(3, out _), Is.True);
        Assert.That(a.HasId(5, out _), Is.True);
        a.SetAdd(7);
        Assert.That(a.HasId(7, out _), Is.True);
    }
}
