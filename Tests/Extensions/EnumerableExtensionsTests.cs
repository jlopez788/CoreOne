using CoreOne.Extensions;
using CoreOne.Results;
using NUnit.Framework;

namespace Tests.Extensions;

public class EnumerableExtensionsTests
{
    [Test]
    public void AddOrUpdate_AddsNewItem_WhenNotExists()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = list.AddOrUpdate(4);
        Assert.That(result.Count, Is.EqualTo(4));
        Assert.That(result.Contains(4), Is.True);
    }

    [Test]
    public void AddOrUpdate_UpdatesExistingItem_WhenExists()
    {
        var list = new List<string> { "apple", "banana", "cherry" };
        var result = list.AddOrUpdate("BANANA", p => p.Equals("banana", StringComparison.OrdinalIgnoreCase));
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result[1], Is.EqualTo("BANANA"));
    }

    [Test]
    public void AddOrUpdate_CreatesNewList_WhenNull()
    {
        List<int>? list = null;
        var result = list.AddOrUpdate(5);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0], Is.EqualTo(5));
    }

    [Test]
    public void ExcludeNulls_RemovesNullValues()
    {
        var list = new List<string?> { "apple", null, "banana", null, "cherry" };
        var result = list.ExcludeNulls().ToList();
        Assert.That(result.Count, Is.EqualTo(3));
        Assert.That(result, Does.Not.Contain(null));
    }

    [Test]
    public void ExcludeNulls_ReturnsEmpty_WhenSourceIsNull()
    {
        List<string>? list = null;
        var result = list.ExcludeNulls();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ExcludeNullOrEmpty_RemovesNullAndEmptyStrings()
    {
        var list = new List<string?> { "apple", null, "", "  ", "banana" };
        var result = list.ExcludeNullOrEmpty().ToList();
        Assert.That(result.Count, Is.EqualTo(2));
        Assert.That(result[0], Is.EqualTo("apple"));
        Assert.That(result[1], Is.EqualTo("banana"));
    }

    [Test]
    public void Each_InvokesActionForEachItem()
    {
        var list = new List<int> { 1, 2, 3 };
        var sum = 0;
        list.Each(x => sum += x);
        Assert.That(sum, Is.EqualTo(6));
    }

    [Test]
    public void Each_WithIndex_ProvidesIndexToAction()
    {
        var list = new List<string> { "a", "b", "c" };
        var indices = new List<int>();
        list.Each((item, index) => indices.Add(index));
        Assert.That(indices, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public async Task EachAsync_ProcessesItemsSequentially()
    {
        var list = new List<int> { 1, 2, 3 };
        var results = new List<int>();
        await list.EachAsync(async x => {
            await Task.Delay(10);
            results.Add(x);
        });
        Assert.That(results, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void Partition_SplitsIntoChunks()
    {
        var list = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
        var partitions = list.Partition(3).ToList();
        Assert.That(partitions.Count, Is.EqualTo(3));
        Assert.That(partitions[0].Count(), Is.EqualTo(3));
        Assert.That(partitions[1].Count(), Is.EqualTo(3));
        Assert.That(partitions[2].Count(), Is.EqualTo(1));
    }

    [Test]
    public void SelectList_MapsToNewType()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = list.SelectList(x => x.ToString());
        Assert.That(result, Is.EqualTo(new[] { "1", "2", "3" }));
    }

    [Test]
    public void SelectArray_MapsToArray()
    {
        var list = new List<int> { 1, 2, 3 };
        var result = list.SelectArray(x => x * 2);
        Assert.That(result, Is.InstanceOf<int[]>());
        Assert.That(result, Is.EqualTo(new[] { 2, 4, 6 }));
    }

    [Test]
    public void ToData_CreatesDictionaryFromItems()
    {
        var list = new List<KeyValuePair<string, int>> {
            new("one", 1),
            new("two", 2),
            new("three", 3)
        };
        var data = list.ToData(kv => kv.Key, kv => kv.Value);
        Assert.That(data["one"], Is.EqualTo(1));
        Assert.That(data["two"], Is.EqualTo(2));
        Assert.That(data["three"], Is.EqualTo(3));
    }

    [Test]
    public async Task AggregateAsync_AccumulatesValues()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var result = await list.AggregateAsync(0, async (sum, x) => {
            await Task.Delay(1);
            return sum + x;
        });
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void AggregateResult_StopsOnFailure()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var processedCount = 0;
        var result = list.AggregateResult(Result.Ok, (acc, x) => {
            processedCount++;
            return x == 3 ? Result.Fail() : Result.Ok;
        });
        Assert.That(result.Success, Is.False);
        Assert.That(processedCount, Is.EqualTo(3));
    }

    [Test]
    public async Task AggregateResultAsync_StopsOnFailure()
    {
        var list = new List<int> { 1, 2, 3, 4 };
        var processedCount = 0;
        var result = await list.AggregateResultAsync(Result.Ok, async (acc, x) => {
            processedCount++;
            await Task.Delay(1);
            return x == 2 ? Result.Fail() : Result.Ok;
        });
        Assert.That(result.Success, Is.False);
        Assert.That(processedCount, Is.EqualTo(2));
    }
}
