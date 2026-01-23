using CoreOne.Collections;

namespace Tests.Collections;

public class DataListTests
{
    [Test]
    public void Constructor_CreatesEmptyDataList()
    {
        var dataList = new DataList<string, int>();
        
        Assert.That(dataList, Is.Empty);
    }

    [Test]
    public void Add_KeyValue_AddsToList()
    {
        var dataList = new DataList<string, int> {
            { "key", 1 },
            { "key", 2 }
        };
        
        var list = dataList["key"];
        Assert.That(list, Has.Count.EqualTo(2));
        Assert.That(list, Does.Contain(1));
        Assert.That(list, Does.Contain(2));
    }

    [Test]
    public void Add_KeyValuePair_AddsToList()
    {
        var dataList = new DataList<string, int> {
            new KeyValuePair<string, int>("key", 1),
            new KeyValuePair<string, int>("key", 2)
        };
        
        var list = dataList["key"];
        Assert.That(list, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddRange_AddsMultipleItems()
    {
        var dataList = new DataList<string, int>();
        var items = new[] { 1, 2, 3, 4, 5 };
        
        dataList.AddRange(items, x => x % 2 == 0 ? "even" : "odd");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dataList["even"], Has.Count.EqualTo(2));
            Assert.That(dataList["odd"], Has.Count.EqualTo(3));
        }
    }

    [Test]
    public void AddRange_NullItems_DoesNotThrow()
    {
        var dataList = new DataList<string, int>();
        
        dataList.AddRange(null, x => "key");
        
        Assert.That(dataList, Is.Empty);
    }

    [Test]
    public void AddRange_ToExistingKey_AppendsToList()
    {
        var dataList = new DataList<string, int> {
            { "key", 1 }
        };
        var items = new[] { 2, 3 };
        
        dataList.AddRange(items, x => "key");
        
        Assert.That(dataList["key"], Has.Count.EqualTo(3));
    }

    [Test]
    public void RemoveAll_WithPredicate_RemovesMatchingItems()
    {
        var dataList = new DataList<string, int> {
            { "key1", 1 },
            { "key1", 2 },
            { "key1", 3 },
            { "key2", 4 }
        };
        
        dataList.RemoveAll((k, v) => k == "key1" && v > 1);
        
        Assert.That(dataList["key1"], Has.Count.EqualTo(1));
        Assert.That(dataList["key1"]![0], Is.EqualTo(1));
    }

    [Test]
    public void RemoveAll_RemovesEmptyKeys()
    {
        var dataList = new DataList<string, int> {
            { "key1", 1 },
            { "key1", 2 }
        };
        
        dataList.RemoveAll((k, v) => true);
        
        Assert.That(dataList.ContainsKey("key1"), Is.False);
    }

    [Test]
    public void Constructor_WithCapacity_CreatesDataList()
    {
        var dataList = new DataList<string, int>(100);
        
        Assert.That(dataList, Is.Empty);
    }

    [Test]
    public void Constructor_WithComparer_UsesComparer()
    {
        var dataList = new DataList<string, int>(StringComparer.OrdinalIgnoreCase) {
            { "Key", 1 }
        };
        
        Assert.That(dataList.ContainsKey("KEY"), Is.True);
    }

    [Test]
    public void Constructor_WithSizeAndComparer_UsesComparer()
    {
        var dataList = new DataList<string, int>(10, StringComparer.OrdinalIgnoreCase) {
            { "Key", 1 }
        };
        
        Assert.That(dataList.ContainsKey("KEY"), Is.True);
    }

    [Test]
    public void Get_NonExistentKey_ReturnsNull()
    {
        var dataList = new DataList<string, int>();
        
        var result = dataList["nonexistent"];
        
        Assert.That(result, Is.Null);
    }

    [Test]
    public void AddRange_WithDifferentKeys_CreatesMultipleLists()
    {
        var dataList = new DataList<string, int>();
        var items = new[] { 1, 2, 3, 4 };
        
        dataList.AddRange(items, x => x.ToString());
        
        Assert.That(dataList, Has.Count.EqualTo(4));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(dataList["1"]![0], Is.EqualTo(1));
            Assert.That(dataList["2"]![0], Is.EqualTo(2));
        }
    }

    [Test]
    public void Clear_RemovesAllItems()
    {
        var dataList = new DataList<string, int> {
            { "key1", 1 },
            { "key2", 2 }
        };
        
        dataList.Clear();
        
        Assert.That(dataList, Is.Empty);
    }
}
