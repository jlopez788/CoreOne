using CoreOne.Collections.Concurrent;

namespace Tests.Collections;

public class ConcurrentSetTests
{
    [Test]
    public void Constructor_CreatesEmptySet()
    {
        var set = new ConcurrentSet<int>();
        
        Assert.That(set, Is.Empty);
    }

    [Test]
    public void Add_AddsItem()
    {
        var set = new ConcurrentSet<int>();
        
        var result = set.Add(1);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(set, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void Add_DuplicateItem_ReturnsFalse()
    {
        var set = new ConcurrentSet<int> {
            1
        };
        
        var result = set.Add(1);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(set, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        var set = new ConcurrentSet<int> {
            1
        };

        Assert.That(set, Does.Contain(1));
    }

    [Test]
    public void Contains_NonExistingItem_ReturnsFalse()
    {
        var set = new ConcurrentSet<int>();

        Assert.That(set, Does.Not.Contain(1));
    }

    [Test]
    public void Remove_ExistingItem_ReturnsTrue()
    {
        var set = new ConcurrentSet<int> {
            1
        };
        
        var result = set.Remove(1);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(set, Is.Empty);
        }
    }

    [Test]
    public void Remove_NonExistingItem_ReturnsFalse()
    {
        var set = new ConcurrentSet<int>();
        
        var result = set.Remove(1);
        
        Assert.That(result, Is.False);
    }

    [Test]
    public void Clear_RemovesAllItems()
    {
        var set = new ConcurrentSet<int> {
            1,
            2,
            3
        };
        
        set.Clear();
        
        Assert.That(set, Is.Empty);
    }

    [Test]
    public void Each_ExecutesForAllItems()
    {
        var set = new ConcurrentSet<int> {
            1,
            2,
            3
        };
        var sum = 0;
        
        set.Each(x => sum += x);
        
        Assert.That(sum, Is.EqualTo(6));
    }

    [Test]
    public async Task EachAsync_ExecutesForAllItems()
    {
        var set = new ConcurrentSet<int> {
            1,
            2,
            3
        };
        var sum = 0;
        
        await set.EachAsync(async x => {
            await Task.Delay(1);
            sum += x;
        });
        
        Assert.That(sum, Is.EqualTo(6));
    }

    [Test]
    public void ToList_ReturnsAllItems()
    {
        var set = new ConcurrentSet<int> {
            1,
            2,
            3
        };
        
        var list = set.ToList();
        
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.That(list, Does.Contain(1));
        Assert.That(list, Does.Contain(2));
        Assert.That(list, Does.Contain(3));
    }

    [Test]
    public void CopyTo_CopiesItems()
    {
        var set = new ConcurrentSet<int> {
            1,
            2
        };
        var array = new int[3];
        
        set.CopyTo(array, 0);
        
        Assert.That(array.Take(2), Does.Contain(1));
        Assert.That(array.Take(2), Does.Contain(2));
    }

    [Test]
    public void Constructor_WithComparer_UsesComparer()
    {
        var set = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Test"
        };

        Assert.That(set.Contains("TEST"), Is.True);
    }

    [Test]
    public void Constructor_WithItemsAndComparer_AddsItems()
    {
        var items = new[] { "a", "b", "c" };
        
        var set = new ConcurrentSet<string>(items, StringComparer.OrdinalIgnoreCase);
        
        Assert.That(set, Has.Count.EqualTo(3));
        Assert.That(set.Contains("A"), Is.True);
    }

    [Test]
    public void IsReadOnly_ReturnsFalse()
    {
        var set = new ConcurrentSet<int>();
        
        Assert.That(set.IsReadOnly, Is.False);
    }

    [Test]
    public void ICollectionAdd_AddsItem()
    {
        var set = new ConcurrentSet<int> {
            1
        };
        
        Assert.That(set, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetEnumerator_IteratesAllItems()
    {
        var set = new ConcurrentSet<int> {
            1,
            2,
            3
        };
        var sum = 0;
        
        foreach (var item in set)
        {
            sum += item;
        }
        
        Assert.That(sum, Is.EqualTo(6));
    }

    [Test]
    public void ThreadSafety_ConcurrentAdds_WorksCorrectly()
    {
        var set = new ConcurrentSet<int>();
        var tasks = new List<Task>();
        
        for (int i = 0; i < 10; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() => set.Add(value)));
        }
        
        Task.WaitAll([.. tasks]);
        
        Assert.That(set, Has.Count.EqualTo(10));
    }
}
